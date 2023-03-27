// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using SunshineGameFinder;
using System.CommandLine;

// default values
var gameDirs = new List<string>() { @"C:\Program Files (x86)\Steam\steamapps\common", @"C:\XboxGames", @"C:\Program Files\EA Games" };
var exclusionWords = new List<string>() { "Steam" };
var exeExclusionWords = new List<string>() { "Steam", "Cleanup", "DX", "Uninstall", "Touchup", "redist", "Crash", "Editor" };

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

Logger.Log($@"
Thanks for using the Sunshine Game Finder! 
Searches your computer for various common game install paths for the Sunshine application. After running it, all games that did not already exist will be added to the apps.json, meaning your Moonlight client should see them next time it is started.

Have an issue or an idea? Come contribute at https://github.com/JMTK/SunshineGameFinder
");
// options handler
rootCommand.SetHandler((addlDirectories, addlExeExclusionWords, sunshineConfigLocation, forceUpdate) =>
{
    gameDirs.AddRange(addlDirectories);
    exeExclusionWords.AddRange(addlExeExclusionWords);
    var sunshineAppsJson = sunshineConfigLocation;

    if (!File.Exists(sunshineAppsJson))
    {
        Logger.Log($"Could not find Sunshine Apps config at specified path: {sunshineAppsJson}", LogLevel.Error);
        return;
    }
    var sunshineAppInstance = Newtonsoft.Json.JsonConvert.DeserializeObject<SunshineConfig>(File.ReadAllText(sunshineAppsJson));
    var gamesAdded = 0;
    foreach (var platformDir in gameDirs)
    {
        Logger.Log($"Scanning for games in {platformDir}...");
        var di = new DirectoryInfo(platformDir);
        if (!di.Exists)
        {
            Logger.Log($"Directory for platform {di.Name} does not exist, skipping...", LogLevel.Warning);
            continue;
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
            if (forceUpdate || existingApp == null)
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
                gamesAdded++;
                Logger.Log($"Adding new game to Sunshine apps: {gameName} - {exe}");
                sunshineAppInstance.apps.Add(existingApp);
            }
            else
            {
                Logger.Log($"Found existing Sunshine app for {gameName} already!: " + (existingApp.cmd ?? existingApp.detached.FirstOrDefault() ?? existingApp.name).Trim());
            }
        }
        Console.WriteLine(""); //blank line to separate platforms
    }
    Logger.Log("Finding Games Completed!");
    if (gamesAdded > 0)
    {
        if (FileWriter.UpdateConfig(sunshineAppsJson, sunshineAppInstance))
        {
            Logger.Log($"Apps config is updated! {gamesAdded} games were added. Check Sunshine to ensure all games were added.", LogLevel.Success);
        }
    }
    else
    {
        Logger.Log("No new games were found to be added to Sunshine");
    }

}, addlDirectoriesOption, addlExeExclusionWords, sunshineConfigLocationOption, forceOption);
rootCommand.Invoke(args);