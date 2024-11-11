// See https://aka.ms/new-console-template for more information
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using System.Text.Json;
using SunshineGameFinder;
using System.CommandLine;
using System.Text.RegularExpressions;
using System.Linq;

// constants
const string wildcatDrive = @"*:\";
const string steamLibraryFolders = @"Program Files (x86)\Steam\steamapps\libraryfolders.vdf";

// default values
var gameDirs = new HashSet<string>() { @"*:\Program Files (x86)\Steam\steamapps\common", @"*:\XboxGames", @"*:\Program Files\EA Games", @"*:\Program Files\Epic Games\", @"*:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games" };
var exclusionWords = new List<string>() { "Steam" };
var exeExclusionWords = new List<string>() { "Steam", "Cleanup", "DX", "Uninstall", "Touchup", "redist", "Crash", "Editor", "crs-handler" };

// command setup
RootCommand rootCommand = new RootCommand("Searches your computer for various common game install paths for the Sunshine application. After running it, all games that did not already exist will be added to the apps.json, meaning your Moonlight client should see them next time it is started.");
var addlDirectoriesOption = new Option<string[]>("--addlDirectories", "Additional platform directories to search. ONLY looks for game directories in the top level of this folder.");
addlDirectoriesOption.AllowMultipleArgumentsPerToken = true;
addlDirectoriesOption.AddAlias("-d");
rootCommand.AddOption(addlDirectoriesOption);

var addlExeExclusionWords = new Option<string[]>("--addlExeExclusionWords", "More words to exclude if the EXE matches any part of this");
addlExeExclusionWords.AllowMultipleArgumentsPerToken = true;
addlExeExclusionWords.AddAlias("-exeExclude");
rootCommand.AddOption(addlExeExclusionWords);

var sunshineConfigLocationOption = new Option<string>("--sunshineConfigLocation", "Specify the Sunshine apps.json location");
sunshineConfigLocationOption.AllowMultipleArgumentsPerToken = false;
sunshineConfigLocationOption.AddAlias("-c");
sunshineConfigLocationOption.SetDefaultValue(@"C:\Program Files\Sunshine\config\apps.json");
rootCommand.AddOption(sunshineConfigLocationOption);

var forceOption = new Option<bool>("--force", "Force update apps.json even if games already existed");
forceOption.AllowMultipleArgumentsPerToken = false;
forceOption.AddAlias("-f");
forceOption.SetDefaultValue(false);
rootCommand.AddOption(forceOption);

var removeUninstalledOption = new Option<bool>("--remove-uninstalled", "Removes apps whose exes can not be found");
removeUninstalledOption.AllowMultipleArgumentsPerToken = false;
removeUninstalledOption.AddAlias("-ru");
removeUninstalledOption.SetDefaultValue(false);
rootCommand.AddOption(removeUninstalledOption);

var ensureDesktopAppOption = new Option<bool>("--ensure-desktop-app", "Ensures that the 'Desktop' app is there");
ensureDesktopAppOption.AllowMultipleArgumentsPerToken = false;
ensureDesktopAppOption.AddAlias("-desktop");
ensureDesktopAppOption.SetDefaultValue(false);
rootCommand.AddOption(ensureDesktopAppOption);

var ensureSteamBigPictureOption = new Option<bool>("--ensure-steam-big-picture", "Ensures that the 'Steam Big Picture' app is there");
ensureSteamBigPictureOption.AllowMultipleArgumentsPerToken = false;
ensureSteamBigPictureOption.AddAlias("-bigpicture");
ensureSteamBigPictureOption.SetDefaultValue(false);
rootCommand.AddOption(ensureSteamBigPictureOption);


Logger.Log($@"
Thanks for using the Sunshine Game Finder! App Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version} - Runtime: {System.Environment.Version}

Searches your computer for various common game install paths for the Sunshine application. After running it, all games that did not already exist will be added to the apps.json, meaning your Moonlight client should see them next time it is started.

Have an issue or an idea? Come contribute at https://github.com/JMTK/SunshineGameFinder
");
// options handler
rootCommand.SetHandler((addlDirectories, addlExeExclusionWords, sunshineConfigLocation, forceUpdate, removeUninstalled, ensureDesktop, ensureSteamBigPicture) =>
{
    foreach (var dir in addlDirectories)
    {
        if (Directory.Exists(dir))
        {
            gameDirs.Add(dir);
        }
    }
    exeExclusionWords.AddRange(addlExeExclusionWords);
    var sunshineAppsJson = sunshineConfigLocation;
    var sunshineRootFolder = Path.GetDirectoryName(sunshineAppsJson);

    if (!File.Exists(sunshineAppsJson))
    {
        Logger.Log($"Could not find Sunshine Apps config at specified path: {sunshineAppsJson}", LogLevel.Error);
        return;
    }
    var sunshineAppInstance = JsonSerializer.Deserialize<SunshineConfig>(File.ReadAllText(sunshineAppsJson), SourceGenerationContext.Default.SunshineConfig);

    sunshineAppInstance ??= new SunshineConfig();
    sunshineAppInstance.apps ??= new List<SunshineApp>();

    var gamesAdded = 0;
    var gamesRemoved = 0;

    if (removeUninstalled)
    {
        for (int i = sunshineAppInstance.apps.Count() - 1; i >= 0; i--) //keep tolist so we can remove elements while iterating on the "copy"
        {
            var existingApp = sunshineAppInstance.apps[i];
            if (existingApp != null)
            {
                var exeStillExists = existingApp.Cmd == null && existingApp.Detached == null ||
                                     existingApp.Cmd != null && File.Exists(existingApp.Cmd) ||
                                     existingApp.Detached != null && existingApp.Detached.Any(detachedCommand =>
                                     {
                                         return detachedCommand == null ||
                                          !detachedCommand.Contains("exe") ||
                                          detachedCommand != null && detachedCommand.EndsWith("exe") && File.Exists(detachedCommand);
                                     });
                if (!exeStillExists)
                {
                    Logger.Log($"{existingApp.Name} no longer has an exe, removing from apps config...");
                    sunshineAppInstance.apps.RemoveAt(i);
                    gamesRemoved++;
                }
            }
        }
    }

    if (sunshineAppInstance == null)
    {
        Logger.Log($"Sunshine app list is null", LogLevel.Error);
        return;
    }

    var foldersScanned = new HashSet<string>();
    void ScanFolder(string folder)
    {
        if (folder == null)
            return;

        if (foldersScanned.Contains(folder))
            return;

        foldersScanned.Add(folder);
        Logger.Log($"Scanning for games in {folder}...");
        var di = new DirectoryInfo(folder);
        if (!di.Exists)
        {
            Logger.Log($"Directory for platform {di.Name} does not exist, skipping...", LogLevel.Warning);
            return;
        }
        foreach (var gameDir in di.GetDirectories())
        {
            try
            {
                Logger.Log($"\tLooking in {gameDir.FullName.Replace(folder, "")}...", false);
                var gameName = CleanGameName(gameDir.Name);
                if (exclusionWords.Any(ew => gameName.Contains(ew)))
                {
                    Logger.Log($"Skipping due to excluded word match", LogLevel.Trace);
                    continue;
                }
                var exe = Directory.GetFiles(gameDir.FullName, "*.exe", SearchOption.AllDirectories).FirstOrDefault(exefile =>
                {
                    var exeName = new FileInfo(exefile).Name.ToLower();
                    return exeName == gameDir.Name.ToLower() || exeName == gameName.ToLower() || !exeExclusionWords.Any(ew => exeName.Contains(ew.ToLower()));
                });
                if (string.IsNullOrEmpty(exe))
                {
                    Logger.Log($"EXE not be found", LogLevel.Warning);
                    continue;
                }

                var existingApp = sunshineAppInstance.apps?.FirstOrDefault(g => g.Cmd == exe || g.Name == gameName);
                if (forceUpdate || existingApp == null)
                {
                    if (forceUpdate && existingApp != null)
                    {
                        sunshineAppInstance.apps.Remove(existingApp);
                    }
                    if (exe.Contains("gamelaunchhelper.exe"))
                    {
                        //xbox game pass game
                        existingApp = new SunshineApp()
                        {
                            Name = gameName,
                            Detached = new List<string>()
                        {
                            exe
                        },
                            WorkingDir = ""
                        };
                    }
                    else
                    {
                        existingApp = new SunshineApp()
                        {
                            Name = gameName,
                            Cmd = exe,
                            WorkingDir = ""
                        };
                    }
                    string coversFolderPath = Path.GetFullPath(sunshineRootFolder.Replace("\\", "/") + "/covers/");
                    string fullPathOfCoverImage = ImageScraper.SaveIGDBImageToCoversFolder(gameName, coversFolderPath).Result;
                    if (!string.IsNullOrEmpty(fullPathOfCoverImage))
                    {
                        existingApp.ImagePath = fullPathOfCoverImage;
                    }
                    else
                    {
                        Logger.Log("Failed to find cover image for " + gameName, LogLevel.Warning);
                    }
                    gamesAdded++;
                    Logger.Log($"Adding new game to Sunshine apps: {gameName} - {exe}", LogLevel.Success);
                    sunshineAppInstance.apps.Add(existingApp);
                }
                else
                {
                    Logger.Log($"Found existing Sunshine app for {gameName} already!: " + (existingApp.Cmd ?? existingApp.Detached?.FirstOrDefault() ?? existingApp.Name).Trim());
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogLevel.Error);
            }
        }
        Console.WriteLine(""); //blank line to separate platforms
    }

    var logicalDrives = DriveInfo.GetDrives();
    var wildcatDriveLetter = new Regex(Regex.Escape(wildcatDrive));

    foreach (var drive in logicalDrives)
    {
        var libraryFoldersPath = drive.Name + steamLibraryFolders;
        var file = new FileInfo(libraryFoldersPath);
        if (!file.Exists)
        {
            Logger.Log($"libraryfolders.vdf not found on {file.DirectoryName}, skipping...", LogLevel.Warning);
            continue;
        }
        var libraries = VdfConvert.Deserialize(File.ReadAllText(libraryFoldersPath));
        foreach (var library in libraries.Value)
        {
            if (library is not VProperty libProp)
                continue;

            gameDirs.Add($@"{libProp.Value.Value<string>("path")}\steamapps\common");
        }
    }

    foreach (var platformDir in gameDirs)
    {
        if (platformDir.StartsWith(wildcatDrive))
        {
            foreach (var drive in logicalDrives)
                ScanFolder(wildcatDriveLetter.Replace(platformDir, drive.Name, 1));
        }
        else
        {
            ScanFolder(platformDir);
        }
    }

    if (ensureDesktop)
    {
        sunshineAppInstance.apps.Add(new DesktopApp());
    }
    if (ensureSteamBigPicture)
    {
        sunshineAppInstance.apps.Add(new SteamBigPictureApp());
    }

        Logger.Log("Finding Games Completed");
    if (gamesAdded > 0 || gamesRemoved > 0)
    {
        if (FileWriter.UpdateConfig(sunshineAppsJson, sunshineAppInstance))
        {
            Logger.Log($"Apps config is updated! {gamesAdded} apps were added. {gamesRemoved} apps were removed. Check Sunshine to ensure all games were added.", LogLevel.Success);
        }
    }
    else
    {
        Logger.Log("No new games were found to be added to Sunshine");
    }

}, addlDirectoriesOption, addlExeExclusionWords, sunshineConfigLocationOption, forceOption, removeUninstalledOption, ensureDesktopAppOption, ensureSteamBigPictureOption);

string CleanGameName(string name)
{
    string[] toReplace = new string[] { "Win10", "Windows 10", "Win11", "Windows 11" };
    foreach (string toRemove in toReplace)
    {
        name = name.Replace(toRemove, "");
    }

    return name.Trim();
}

rootCommand.Invoke(args);