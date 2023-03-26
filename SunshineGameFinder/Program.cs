// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using SunshineGameFinder;
using System.CommandLine;

// default values
var gameDirs = new List<string>() { @"C:\Program Files (x86)\Steam\steamapps\common", @"C:\XboxGames", @"C:\Program Files\EA Games" };
var exclusionWords = new List<string>() { "Steam" };
var exeExclusionWords = new List<string>() { "Steam", "Cleanup", "DX", "Uninstall", "Touchup", "redist", "Crash" };

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
        Console.WriteLine($"Could not find Sunshine Apps config at specified path: {sunshineAppsJson}");
        return;
    }
    var sunshineAppInstance = Newtonsoft.Json.JsonConvert.DeserializeObject<SunshineConfig>(File.ReadAllText(sunshineAppsJson));
    foreach (var platformDir in gameDirs)
    {
        Console.WriteLine($"Scanning for games in {platformDir}...");
        var di = new DirectoryInfo(platformDir);
        if (!di.Exists)
        {
            Console.WriteLine($"Directory for platform {di.Name} does not exist, skipping...");
            continue;
        }
        foreach (var gameDir in di.GetDirectories())
        {
            Console.WriteLine($"Looking for game exe in {gameDir}...");
            var gameName = gameDir.Name;
            if (exclusionWords.Any(ew => gameName.Contains(ew)))
            {
                Console.WriteLine($"Skipping {gameName} as it was an excluded word match...");
                continue;
            }
            var exe = Directory.GetFiles(gameDir.FullName, "*.exe", SearchOption.AllDirectories).FirstOrDefault(exefile => !exeExclusionWords.Any(ew => new FileInfo(exefile).Name.Contains(ew)));
            if (string.IsNullOrEmpty(exe))
            {
                Console.WriteLine($"EXE could not be found for game '{gameName}'");
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
                Console.WriteLine($"Adding new game to Sunshine apps: {gameName} - {exe}");
                sunshineAppInstance.apps.Add(existingApp);
            }
            else
            {
                Console.WriteLine($"Found existing Sunshine app for {gameName} already!: " + (existingApp.cmd ?? existingApp.detached.FirstOrDefault() ?? existingApp.name).Trim());
            }
        }
    }
    Console.WriteLine("Complete!");
    File.WriteAllText(sunshineAppsJson, JsonConvert.SerializeObject(sunshineAppInstance, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));

}, addlDirectoriesOption, addlExeExclusionWords, sunshineConfigLocationOption);
rootCommand.Invoke(args);