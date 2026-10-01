using SunshineGameFinder;
using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
#if WINDOWS_BUILD
using System.Security.Principal;
#endif

#if WINDOWS_BUILD
// Only Windows needs elevation (apps.json lives in Program Files). On Linux/macOS it's user-owned under ~/.config/sunshine.
if (OperatingSystem.IsWindows() && !IsRunAsAdmin())
{
    var exeName = Environment.ProcessPath;
    if (exeName != null)
    {
        try
        {
            Process.Start(new ProcessStartInfo(exeName)
            {
                UseShellExecute = true,
                Verb = "runas",   // This triggers the UAC elevation prompt
                Arguments = string.Join(" ", args.Select(QuoteArgument))
            });
            return; // Exit this instance
        }
        catch (Exception ex)
        {
            // User declined the UAC prompt
            Logger.Log($"This application requires administrative privileges. Elevation failed: {ex.Message}", LogLevel.Error);
            return;
        }
    }
}
#endif

// constants
const string wildcardDrive = @"*:\";

// default values
var gameDirs = new HashSet<string>(PlatformPaths.PathComparer);
if (OperatingSystem.IsWindows())
{
    gameDirs.UnionWith([
        @"*:\Program Files (x86)\Steam\steamapps\common",
        @"*:\XboxGames",
        @"*:\Program Files\EA Games",
        @"*:\Program Files\Epic Games",
        @"*:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games",
        @"*:\Program Files\Rockstar Games",
    ]);
}
var exclusionWords = new List<string>() { "Steam" };
// Launcher/runtime folders that live next to games but aren't games
var excludedFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Launcher", "Social Club", "Rockstar Games Launcher", "Rockstar Games Social Club" };
var exeExclusionWords = new List<string>() { "Steam", "Cleanup", "DX", "Uninstall", "Touchup", "redist", "Crash", "Editor", "crs-handler", "EasyAntiCheat", "BattlEye", "BEService", "Prereq" };

// command setup
RootCommand rootCommand = new RootCommand("Searches your computer for various common game install paths for the Sunshine application. After running it, all games that did not already exist will be added to the apps.json, meaning your Moonlight client should see them next time it is started.");
var addlDirectoriesOption = new Option<string[]>("--addlDirectories", "-d");
addlDirectoriesOption.AllowMultipleArgumentsPerToken = true;
addlDirectoriesOption.Description = "Additional platform directories to search. ONLY looks for game directories in the top level of this folder.";
rootCommand.Options.Add(addlDirectoriesOption);

var addlExeExclusionWordsOption = new Option<string[]>("--addlExeExclusionWords", "-exeExclude");
addlExeExclusionWordsOption.Description = "Additional words to exclude from exe names when searching for game executables.";
addlExeExclusionWordsOption.AllowMultipleArgumentsPerToken = true;
rootCommand.Options.Add(addlExeExclusionWordsOption);

var sunshineConfigLocationOption = new Option<string>("--sunshineConfigLocation", "-c");
sunshineConfigLocationOption.Description = "Specify the Sunshine apps.json location";
sunshineConfigLocationOption.AllowMultipleArgumentsPerToken = false;
sunshineConfigLocationOption.DefaultValueFactory = arg => PlatformPaths.GetDefaultSunshineAppsJson();
rootCommand.Options.Add(sunshineConfigLocationOption);

var forceOption = new Option<bool>("--force", "-f");
forceOption.Description = "Force re-adding of existing games to Sunshine apps config";
forceOption.AllowMultipleArgumentsPerToken = false;
forceOption.DefaultValueFactory = arg => (false);
rootCommand.Options.Add(forceOption);

var removeUninstalledOption = new Option<bool>("--remove-uninstalled", "-ru");
removeUninstalledOption.AllowMultipleArgumentsPerToken = false;
removeUninstalledOption.Description = "Removes games from Sunshine apps config that no longer have an executable on disk";
removeUninstalledOption.DefaultValueFactory = arg => (false);
rootCommand.Options.Add(removeUninstalledOption);

var ensureDesktopAppOption = new Option<bool>("--ensure-desktop-app", "-desktop");
ensureDesktopAppOption.AllowMultipleArgumentsPerToken = false;
ensureDesktopAppOption.Description = "Ensures that the 'Desktop' app is there";
ensureDesktopAppOption.DefaultValueFactory = arg => (false);
rootCommand.Options.Add(ensureDesktopAppOption);

var ensureSteamBigPictureOption = new Option<bool>("--ensure-steam-big-picture", "-bigpicture");
ensureSteamBigPictureOption.AllowMultipleArgumentsPerToken = false;
ensureSteamBigPictureOption.Description = "Ensures that the 'Steam Big Picture' app is there";
ensureSteamBigPictureOption.DefaultValueFactory = arg => (false);
rootCommand.Options.Add(ensureSteamBigPictureOption);

var nowaitAfterRunning = new Option<bool>("--no-wait");
nowaitAfterRunning.AllowMultipleArgumentsPerToken = false;
nowaitAfterRunning.DefaultValueFactory = arg => (false);
rootCommand.Options.Add(nowaitAfterRunning);

var notifyOption = new Option<bool>("--notify", "-n");
notifyOption.AllowMultipleArgumentsPerToken = false;
notifyOption.Description = "Send a system notification with the results. Useful with --no-wait when running in the background (e.g. scheduled task / cron)";
notifyOption.DefaultValueFactory = arg => (false);
rootCommand.Options.Add(notifyOption);


Logger.Log($@"
Thanks for using the Sunshine Game Finder! App Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version} - Runtime: {System.Environment.Version}

Searches your computer for various common game install paths for the Sunshine application. After running it, all games that did not already exist will be added to the apps.json, meaning your Moonlight client should see them next time it is started.

Have an issue or an idea? Come contribute at https://github.com/JMTK/SunshineGameFinder
");

ParseResult parseResult = rootCommand.Parse(args);

// options handler
var addlDirectories = parseResult.GetValue(addlDirectoriesOption) ?? [];
var addlExeExclusionWords = parseResult.GetValue(addlExeExclusionWordsOption) ?? [];
var sunshineConfigLocation = parseResult.GetValue(sunshineConfigLocationOption);
var forceUpdate = parseResult.GetValue(forceOption);
var removeUninstalled = parseResult.GetValue(removeUninstalledOption);
var ensureDesktop = parseResult.GetValue(ensureDesktopAppOption);
var ensureSteamBigPicture = parseResult.GetValue(ensureSteamBigPictureOption);
var nowait = parseResult.GetValue(nowaitAfterRunning);
var notify = parseResult.GetValue(notifyOption);

void Notify(string title, string message)
{
    if (notify)
        Notifier.Send(title, message);
}

foreach (var dir in addlDirectories)
{
    if (Directory.Exists(dir))
    {
        gameDirs.Add(PlatformPaths.NormalizeDir(dir));
    }
    else
    {
        Logger.Log($"Additional directory does not exist, skipping: {dir}", LogLevel.Warning);
    }
}
exeExclusionWords.AddRange(addlExeExclusionWords);
var sunshineAppsJson = Path.GetFullPath(string.IsNullOrWhiteSpace(sunshineConfigLocation) ? PlatformPaths.GetDefaultSunshineAppsJson() : sunshineConfigLocation);
var sunshineRootFolder = Path.GetDirectoryName(sunshineAppsJson)!;

if (!File.Exists(sunshineAppsJson))
{
    Logger.Log($"Could not find Sunshine Apps config at specified path: {sunshineAppsJson}", LogLevel.Error);
    Notify("Sunshine Game Finder failed", $"Could not find Sunshine apps config at {sunshineAppsJson}");
    return;
}
var sunshineAppInstance = JsonSerializer.Deserialize<SunshineConfig>(await File.ReadAllTextAsync(sunshineAppsJson), SourceGenerationContext.Default.SunshineConfig);

sunshineAppInstance ??= new SunshineConfig() { Env = new Env() };
sunshineAppInstance.apps ??= new List<SunshineApp>();
sunshineAppInstance.Env ??= new Env();

var steamScan = SteamLibrary.Scan();
var installedSteamAppIds = steamScan.Games.Select(g => g.AppId).ToHashSet();

var gamesAdded = 0;
var gamesRemoved = 0;
if (removeUninstalled)
{
    for (int i = sunshineAppInstance.apps.Count() - 1; i >= 0; i--) //keep tolist so we can remove elements while iterating on the "copy"
    {
        var existingApp = sunshineAppInstance.apps[i];
        if (existingApp != null)
        {
            bool exeStillExists;
            var steamAppId = SteamLibrary.GetAppId(existingApp);
            if (steamAppId != null)
            {
                // Only trust the Steam check if we actually found a Steam library on this machine
                exeStillExists = steamScan.LibraryPaths.Count == 0 || installedSteamAppIds.Contains(steamAppId);
            }
            else
            {
                exeStillExists = existingApp.Cmd == null && existingApp.Detached == null ||
                                 existingApp.Cmd != null && File.Exists(existingApp.Cmd.Trim('"')) ||
                                 existingApp.Cmd?.Contains("://") == true ||
                                 existingApp.Detached != null && existingApp.Detached.Any(detachedCommand =>
                                 {
                                     return detachedCommand == null ||
                                      !detachedCommand.Contains("exe") ||
                                      detachedCommand != null && detachedCommand.EndsWith("exe") && File.Exists(detachedCommand.Trim('"'));
                                 });
            }
            if (!exeStillExists)
            {
                Logger.Log($"{existingApp.Name} no longer has an exe, removing from apps config...",
                    LogLevel.Error);
                sunshineAppInstance.apps.RemoveAt(i);
                gamesRemoved++;
            }
        }
    }
}

var foldersScanned = new HashSet<string>(PlatformPaths.PathComparer);
var gameDirsScanned = new HashSet<string>(PlatformPaths.PathComparer);
var coversFolderPath = Path.Combine(sunshineRootFolder, "covers");

async Task AddApp(SunshineApp newApp, string launchCommand)
{
    var apps = sunshineAppInstance.apps!;
    var existingApp = apps.FirstOrDefault(g => g.Name == newApp.Name || g.Cmd == launchCommand || g.Detached?.Contains(launchCommand) == true);
    if (existingApp != null && !forceUpdate)
    {
        Logger.Log($"Found existing Sunshine app for {newApp.Name} already!: " + (existingApp.Cmd ?? existingApp.Detached?.FirstOrDefault() ?? existingApp.Name).Trim());
        return;
    }
    if (existingApp != null)
    {
        apps.Remove(existingApp);
    }

    string? fullPathOfCoverImage = await ImageScraper.SaveIGDBImageToCoversFolder(newApp.Name, coversFolderPath);
    if (!string.IsNullOrEmpty(fullPathOfCoverImage))
    {
        newApp.ImagePath = fullPathOfCoverImage;
    }
    else
    {
        Logger.Log("Failed to find cover image for " + newApp.Name, LogLevel.Warning);
    }
    gamesAdded++;
    Logger.Log($"Adding new game to Sunshine apps: {newApp.Name} - {launchCommand}", LogLevel.Success);
    apps.Add(newApp);
}

async Task ScanGameDir(DirectoryInfo gameDir, string? displayName = null, bool isRockstar = false)
{
    if (!gameDirsScanned.Add(PlatformPaths.NormalizeDir(gameDir.FullName)))
        return;

    try
    {
        Logger.Log($"\tLooking in {gameDir.Name}...", false);
        var isMacApp = OperatingSystem.IsMacOS() && gameDir.Extension.Equals(".app", StringComparison.OrdinalIgnoreCase);
        var gameName = CleanGameName(displayName ?? (isMacApp ? Path.GetFileNameWithoutExtension(gameDir.Name) : gameDir.Name));
        if (excludedFolderNames.Contains(gameDir.Name) || excludedFolderNames.Contains(gameName) || exclusionWords.Any(ew => gameName.Contains(ew)))
        {
            Logger.Log($"Skipping due to excluded word match", LogLevel.Trace);
            return;
        }

        if (isMacApp)
        {
            var openCommand = $"open -a \"{gameDir.FullName}\"";
            await AddApp(new SunshineApp() { Name = gameName, Detached = [openCommand], WorkingDir = "" }, openCommand);
            return;
        }

        isRockstar |= gameDir.FullName.Contains("Rockstar Games", StringComparison.OrdinalIgnoreCase);
        var exe = GameExecutableFinder.Find(gameDir, gameName, exeExclusionWords, isRockstar);
        if (string.IsNullOrEmpty(exe))
        {
            Logger.Log($"EXE not be found", LogLevel.Warning);
            return;
        }

        SunshineApp newApp;
        if (Path.GetFileName(exe).Equals("gamelaunchhelper.exe", StringComparison.OrdinalIgnoreCase))
        {
            //xbox game pass game
            newApp = new SunshineApp() { Name = gameName, Detached = [exe], WorkingDir = "" };
        }
        else if (OperatingSystem.IsWindows())
        {
            newApp = new SunshineApp() { Name = gameName, Cmd = exe, WorkingDir = "" };
        }
        else
        {
            // Sunshine splits commands on spaces on Linux/macOS
            newApp = new SunshineApp() { Name = gameName, Cmd = exe.Contains(' ') ? $"\"{exe}\"" : exe, WorkingDir = gameDir.FullName };
        }
        await AddApp(newApp, newApp.Cmd ?? exe);
    }
    catch (Exception ex)
    {
        Logger.Log(ex.Message, LogLevel.Error);
    }
}

async Task ScanFolder(string folder)
{
    if (!foldersScanned.Add(folder))
        return;

    Logger.Log($"Scanning for games in {folder}...");
    var di = new DirectoryInfo(folder);
    if (!di.Exists)
    {
        Logger.Log($"Directory for platform {di.Name} does not exist, skipping...", LogLevel.Warning);
        return;
    }
    try
    {
        foreach (var gameDir in di.GetDirectories())
        {
            await ScanGameDir(gameDir);
        }
    }
    catch (Exception ex)
    {
        Logger.Log(ex.Message, LogLevel.Error);
    }
    Console.WriteLine(""); //blank line to separate platforms
}

// Steam: launch via steam://rungameid so Steam DRM / EasyAntiCheat start correctly
if (steamScan.Games.Count > 0)
{
    Logger.Log($"Scanning {steamScan.Games.Count} installed Steam apps across {steamScan.LibraryPaths.Count} libraries...");
    foreach (var game in steamScan.Games)
    {
        gameDirsScanned.Add(game.InstallDir);
        var gameName = CleanGameName(game.Name);
        Logger.Log($"\tSteam app {game.AppId} {gameName}...", false);
        if (SteamLibrary.IsTool(game) || exclusionWords.Any(ew => gameName.Contains(ew)))
        {
            Logger.Log($"Skipping Steam tool/excluded app", LogLevel.Trace);
            continue;
        }
        try
        {
            var steamApp = SteamLibrary.CreateApp(gameName, game.AppId);
            await AddApp(steamApp, steamApp.Detached![0]);
        }
        catch (Exception ex)
        {
            Logger.Log(ex.Message, LogLevel.Error);
        }
    }
    Console.WriteLine("");
}

if (OperatingSystem.IsWindows())
{
    var rockstarGames = RockstarLibrary.GetInstalledGames();
    if (rockstarGames.Count > 0)
    {
        Logger.Log("Scanning Rockstar Games Launcher titles...");
        foreach (var (name, installFolder) in rockstarGames)
        {
            await ScanGameDir(new DirectoryInfo(installFolder), name, isRockstar: true);
        }
        Console.WriteLine("");
    }
}

var logicalDrives = OperatingSystem.IsWindows() ? DriveInfo.GetDrives().Where(d => d.IsReady).ToArray() : [];
foreach (var platformDir in gameDirs)
{
    if (platformDir.StartsWith(wildcardDrive))
    {
        foreach (var drive in logicalDrives)
            await ScanFolder(drive.Name + platformDir[wildcardDrive.Length..]);
    }
    else
    {
        await ScanFolder(platformDir);
    }
}

if (ensureDesktop && !sunshineAppInstance.apps.Any(app => app.Name == "Desktop"))
{
    sunshineAppInstance.apps.Add(new DesktopApp());
}
if (ensureSteamBigPicture && !sunshineAppInstance.apps.Any(app => app.Name == "Steam Big Picture" || app.Cmd == "steam://open/bigpicture"))
{
    sunshineAppInstance.apps.Add(new SteamBigPictureApp());
}

Logger.Log("Finding Games Completed");
if (gamesAdded > 0 || gamesRemoved > 0)
{
    if (FileWriter.UpdateConfig(sunshineAppsJson, sunshineAppInstance))
    {
        Logger.Log($"Apps config is updated! {gamesAdded} apps were added. {gamesRemoved} apps were removed. Check Sunshine to ensure all games were added.", LogLevel.Success);
        Notify("Sunshine Game Finder", $"{gamesAdded} games added, {gamesRemoved} removed.");
    }
    else
    {
        Notify("Sunshine Game Finder failed", $"Could not update {sunshineAppsJson}. See the log for details.");
    }
}
else
{
    Logger.Log("No new games were found to be added to Sunshine");
    Notify("Sunshine Game Finder", "No new games were found.");
}

// Never block on input when there's no interactive console (scheduled task, cron, systemd timer)
var interactive = !nowait && !Console.IsInputRedirected;
if (interactive)
{
    // Prompt the user to optionally restart the Sunshine service
    Console.Write("\nWould you like to restart the Sunshine service now? (Y/N): ");
    var restartResponse = Console.ReadLine();
    if (!string.IsNullOrEmpty(restartResponse) && restartResponse.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
    {
        Logger.Log("Attempting to restart Sunshine service...", LogLevel.Trace);
        RestartSunshineService();
    }

    Logger.Log("\nPress any key to exit...");
    Console.ReadKey();
}

string CleanGameName(string name)
{
    string[] toReplace = new string[] { "Win10", "Windows 10", "Win11", "Windows 11", "™", "®", "©" };
    foreach (string toRemove in toReplace)
    {
        name = name.Replace(toRemove, "");
    }

    return name.Trim();
}

static string QuoteArgument(string arg)
{
    if (arg.Length > 0 && !arg.Any(c => char.IsWhiteSpace(c) || c == '"'))
        return arg;
    var escaped = arg.Replace("\"", "\\\"");
    // A trailing backslash would otherwise escape the closing quote
    if (escaped.EndsWith('\\'))
        escaped += "\\";
    return $"\"{escaped}\"";
}

#if WINDOWS_BUILD
static bool IsRunAsAdmin()
{
    try
    {
        if (OperatingSystem.IsWindows())
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        return false;
    }
    catch
    {
        return false;
    }
}
#endif

static void RestartSunshineService()
{
    try
    {
        if (OperatingSystem.IsWindows())
        {
            ProcessRunner.Run("net", ["stop", "SunshineService"]);
            if (ProcessRunner.Run("net", ["start", "SunshineService"]))
                Logger.Log("SunshineService restarted successfully.", LogLevel.Success);
            else
                Logger.Log("Failed to restart SunshineService.", LogLevel.Warning);
        }
        else if (OperatingSystem.IsLinux())
        {
            // Sunshine runs as a systemd *user* service; the unit name differs between newer and older packages
            if (ProcessRunner.Run("systemctl", ["--user", "restart", "app-dev.lizardbyte.app.Sunshine"]) ||
                ProcessRunner.Run("systemctl", ["--user", "restart", "sunshine"]))
                Logger.Log("Sunshine service restarted successfully.", LogLevel.Success);
            else
                Logger.Log("Failed to restart Sunshine. If you ran this with sudo, run it as your normal user or restart Sunshine manually.", LogLevel.Warning);
        }
        else
        {
            Logger.Log("Automatic restart isn't supported on this OS, please restart Sunshine manually.", LogLevel.Warning);
        }
    }
    catch (Exception ex)
    {
        Logger.Log("Error restarting service: " + ex.Message, LogLevel.Error);
    }
}


