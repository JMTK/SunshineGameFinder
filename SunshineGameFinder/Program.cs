// See https://aka.ms/new-console-template for more information
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Newtonsoft.Json;
using SunshineGameFinder;
using System.CommandLine;
using System.Text.RegularExpressions;

// constants
const string wildcatDrive = @"*:\";
const string steamLibraryFolders = @"Program Files (x86)\Steam\steamapps\libraryfolders.vdf";

// default values
var gameDirs = new List<string>() { @"*:\Program Files (x86)\Steam\steamapps\common", @"*:\XboxGames", @"*:\Program Files\EA Games" };
var exclusionWords = new List<string>() { "Steam" };
var exeExclusionWords = new List<string>() { "Steam", "Cleanup", "DX", "Uninstall", "Touchup", "redist", "Crash", "Editor" };

// command setup
RootCommand rootCommand = new RootCommand("Searches your computer for various common game install paths for the Sunshine application. After running it, all games that did not already exist will be added to the apps.json, meaning your Moonlight client should see them next time it is started.\r\n\r\n");
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

// options handler
rootCommand.SetHandler((addlDirectories, addlExeExclusionWords, sunshineConfigLocation) =>
{
    gameDirs.AddRange(addlDirectories);
    exeExclusionWords.AddRange(addlExeExclusionWords);
    var sunshineAppsJson = sunshineConfigLocation;

    if (!File.Exists(sunshineAppsJson))
    {
        Logger.Log($"Could not find Sunshine Apps config at specified path: {sunshineAppsJson}", LogLevel.Error);
        return;
    }
    var sunshineAppInstance = JsonConvert.DeserializeObject<SunshineConfig>(File.ReadAllText(sunshineAppsJson));
    if (sunshineAppInstance == null)
        return;

    void ScanFolder(string folder)
    {
        Logger.Log($"Scanning for games in {folder}...");
        var di = new DirectoryInfo(folder);
        if (!di.Exists)
        {
            Logger.Log($"Directory for platform {di.Name} does not exist, skipping...", LogLevel.Warning);
            return;
        }
        foreach (var gameDir in di.GetDirectories())
        {
            Logger.Log($"Looking for game exe in {gameDir}...");
            var gameName = gameDir.Name;
            if (exclusionWords.Any(ew => gameName.Contains(ew)))
            {
                Logger.Log($"Skipping {gameName} as it was an excluded word match...");
                continue;
            }
            var exe = Directory.GetFiles(gameDir.FullName, "*.exe", SearchOption.AllDirectories).FirstOrDefault(exefile => {
                var exeName = new FileInfo(exefile).Name.ToLower();
                return exeName == gameName.ToLower() || !exeExclusionWords.Any(ew => exeName.Contains(ew.ToLower()));
            });
            if (string.IsNullOrEmpty(exe))
            {
                Logger.Log($"EXE could not be found for game '{gameName}'", LogLevel.Warning);
                continue;
            }

            var existingApp = sunshineAppInstance.apps.FirstOrDefault(g => g.cmd == exe || g.name == gameName);
            if (existingApp == null)
            {
                if (exe.Contains("gamelaunchhelper.exe"))
                {
                    //xbox game pass game
                    existingApp = new SunshineApp()
                    {
                        name = gameName,
                        detached = new List<string>()
                    {
                        exe
                    },
                        workingdir = ""
                    };
                }
                else
                {
                    existingApp = new SunshineApp()
                    {
                        name = gameName,
                        cmd = exe,
                        workingdir = ""
                    };
                }
                Logger.Log($"Adding new game to Sunshine apps: {gameName} - {exe}");
                sunshineAppInstance.apps.Add(existingApp);
            }
            else
            {
                Logger.Log($"Found existing Sunshine app for {gameName} already!: " + (existingApp.cmd ?? existingApp.detached.FirstOrDefault() ?? existingApp.name).Trim());
            }
        }
    }

    var logicalDrives = DriveInfo.GetDrives();
    var wildcatDriveLetter = new Regex(Regex.Escape(wildcatDrive));

    foreach (var drive in logicalDrives)
    {
        var libraryFoldersPath = drive.Name + steamLibraryFolders;
        var file = new FileInfo(libraryFoldersPath);
        if (!file.Exists)
        {
            Logger.Log($"{file.FullName} does not exist, skipping...", LogLevel.Warning);
            continue;
        }
        var libraries = VdfConvert.Deserialize(File.ReadAllText(libraryFoldersPath));
        foreach(var library in libraries.Value)
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
    Logger.Log("Finding Games Completed");
    if (FileWriter.UpdateConfig(sunshineAppsJson, sunshineAppInstance))
    {
        Logger.Log("Saving Changes!", LogLevel.Success);
    }

}, addlDirectoriesOption, addlExeExclusionWords, sunshineConfigLocationOption);
rootCommand.Invoke(args);