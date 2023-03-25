// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using SunshineGameFinder;

Console.WriteLine("Hello, World!");
var gameDirs = new string[] { @"C:\Program Files (x86)\Steam\steamapps\common", @"C:\XboxGames", @"C:\Program Files\EA Games" };
var exclusionWords = new string[] { "Steam" };
var exeExclusionWords = new string[] { "Steam" };
var sunshineAppsJson = @"C:\Program Files\Sunshine\config\apps.json";
var sunshineAppInstance = Newtonsoft.Json.JsonConvert.DeserializeObject<SunshineConfig>(File.ReadAllText(sunshineAppsJson));
foreach (var platformDir in gameDirs)
{
    foreach (var gameDir in Directory.GetDirectories(platformDir))
    {
        var gameName = new DirectoryInfo(gameDir).Name;
        if (exclusionWords.Any(ew => gameName.Contains(ew))) continue;
        var exe = Directory.GetFiles(gameDir, "*.exe", SearchOption.AllDirectories).FirstOrDefault(exefile => !exeExclusionWords.Any(ew => new FileInfo(exefile).Name.Contains(ew)));
        if (string.IsNullOrEmpty(exe)) {
            Console.WriteLine($"EXE could not be found for game '${gameName}'");
            continue;
        }
        
        var existingApp = sunshineAppInstance.apps.FirstOrDefault(g => g.cmd == exe || g.name == gameName);
        if (existingApp == null)
        {
            existingApp = new SunshineApp()
            {
                name = gameName,
                cmd = exe
            };
            Console.WriteLine($"Adding new game to Sunshine apps: {gameName} - {exe}");
            sunshineAppInstance.apps.Add(existingApp);
        }
        else
        {
            Console.WriteLine($"Found existing Sunshine app for ${gameName} already!: " + existingApp.cmd ?? existingApp.detached.FirstOrDefault() ?? existingApp.name);
        }
    }
}
File.WriteAllText(sunshineAppsJson, JsonConvert.SerializeObject(sunshineAppInstance, Newtonsoft.Json.Formatting.Indented));