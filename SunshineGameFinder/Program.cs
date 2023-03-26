// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using SunshineGameFinder;

var gameDirs = new string[] { @"C:\Program Files (x86)\Steam\steamapps\common", @"C:\XboxGames", @"C:\Program Files\EA Games" };
var exclusionWords = new string[] { "Steam" };
var exeExclusionWords = new string[] { "Steam", "Cleanup", "DX", "Uninstall", "Touchup", "redist", "Crash" };
var sunshineAppsJson = @"C:\Program Files\Sunshine\config\apps.json";


if (!File.Exists(sunshineAppsJson))
{
    Logger.Log($"Could not find Sunshine Apps config at specified path: {sunshineAppsJson}", LogLevel.Error);
    return;
}
var sunshineAppInstance = Newtonsoft.Json.JsonConvert.DeserializeObject<SunshineConfig>(File.ReadAllText(sunshineAppsJson));
foreach (var platformDir in gameDirs)
{
    Logger.Log($"Scanning for games in {platformDir}...");
    foreach (var gameDir in Directory.GetDirectories(platformDir))
    {
        Logger.Log($"Looking for game exe in {gameDir}...");
        var gameName = new DirectoryInfo(gameDir).Name;
        if (exclusionWords.Any(ew => gameName.Contains(ew)))
        {
            Logger.Log($"Skipping {gameName} as it was an excluded word match...");
            continue;
        }
        var exe = Directory.GetFiles(gameDir, "*.exe", SearchOption.AllDirectories).FirstOrDefault(exefile => !exeExclusionWords.Any(ew => new FileInfo(exefile).Name.Contains(ew)));
        if (string.IsNullOrEmpty(exe)) {
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
            Logger.Log($"Adding new game to Sunshine apps: {gameName} - {exe}", LogLevel.Success);
            sunshineAppInstance.apps.Add(existingApp);
        }
        else
        {
            Logger.Log($"Found existing Sunshine app for ${gameName} already!: " + (existingApp.cmd ?? existingApp.detached.FirstOrDefault() ?? existingApp.name).Trim());
        }
    }
}
Logger.Log("Finding Games Completed");
File.WriteAllText(sunshineAppsJson, JsonConvert.SerializeObject(sunshineAppInstance, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
Logger.Log("Saving Changes!");