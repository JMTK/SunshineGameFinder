using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace SunshineGameFinder
{
    internal record SteamGame(string AppId, string Name, string InstallDir);

    internal record SteamScanResult(IReadOnlyList<string> LibraryPaths, IReadOnlyList<SteamGame> Games);

    internal static partial class SteamLibrary
    {
        // Steamworks Common Redistributables
        private static readonly HashSet<string> ToolAppIds = ["228980"];

        [GeneratedRegex(@"steam://rungameid/(\d+)", RegexOptions.IgnoreCase)]
        private static partial Regex RunGameIdRegex();

        public static SteamScanResult Scan()
        {
            var libraries = new HashSet<string>(PlatformPaths.PathComparer);
            foreach (var root in GetSteamRoots().Where(Directory.Exists).Select(PlatformPaths.NormalizeDir).Distinct(PlatformPaths.PathComparer))
            {
                Logger.Log($"Found Steam install at {root}");
                foreach (var library in GetLibraryFolders(root).Where(Directory.Exists))
                    libraries.Add(PlatformPaths.NormalizeDir(library));
            }

            var games = new Dictionary<string, SteamGame>();
            foreach (var library in libraries)
            {
                var steamApps = Path.Combine(library, "steamapps");
                if (!Directory.Exists(steamApps))
                    continue;
                foreach (var manifest in Directory.EnumerateFiles(steamApps, "appmanifest_*.acf"))
                {
                    var game = ReadManifest(manifest, library);
                    if (game != null)
                        games.TryAdd(game.AppId, game);
                }
            }

            return new SteamScanResult(libraries.ToList(), games.Values.OrderBy(g => g.Name).ToList());
        }

        public static bool IsTool(SteamGame game) =>
            ToolAppIds.Contains(game.AppId) ||
            game.Name.StartsWith("Proton", StringComparison.OrdinalIgnoreCase) ||
            game.Name.StartsWith("Steam Linux Runtime", StringComparison.OrdinalIgnoreCase) ||
            game.Name.StartsWith("Steamworks", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Launching through Steam (instead of the exe) keeps Steam DRM and anti-cheat bootstrappers such as EasyAntiCheat working.
        /// Commands follow the Sunshine app examples for each OS.
        /// </summary>
        public static SunshineApp CreateApp(string name, string appId)
        {
            var uri = $"steam://rungameid/{appId}";
            var command = OperatingSystem.IsWindows() ? uri
                : OperatingSystem.IsMacOS() ? $"open {uri}"
                : $"setsid steam {uri}";
            return new SunshineApp
            {
                Name = name,
                Detached = [command],
                WorkingDir = ""
            };
        }

        public static string? GetAppId(SunshineApp app) =>
            new[] { app.Cmd }
                .Concat(app.Detached ?? [])
                .Where(c => !string.IsNullOrEmpty(c))
                .Select(c => RunGameIdRegex().Match(c!))
                .FirstOrDefault(m => m.Success)?.Groups[1].Value;

        private static IEnumerable<string> GetSteamRoots()
        {
            if (OperatingSystem.IsWindows())
            {
                foreach (var path in GetRegistrySteamRoots())
                    yield return path;

                var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (!string.IsNullOrEmpty(programFilesX86))
                    yield return Path.Combine(programFilesX86, "Steam");

                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                    yield return Path.Combine(drive.Name, "Program Files (x86)", "Steam");
                yield break;
            }

            var home = PlatformPaths.UserHome;
            if (OperatingSystem.IsMacOS())
            {
                yield return Path.Combine(home, "Library", "Application Support", "Steam");
                yield break;
            }

            yield return Path.Combine(home, ".steam", "steam");
            yield return Path.Combine(home, ".steam", "root");
            yield return Path.Combine(home, ".local", "share", "Steam");
            var xdgData = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (!string.IsNullOrEmpty(xdgData))
                yield return Path.Combine(xdgData, "Steam");
            yield return Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam");
            yield return Path.Combine(home, "snap", "steam", "common", ".local", "share", "Steam");
        }

        [SupportedOSPlatform("windows")]
        private static List<string> GetRegistrySteamRoots()
        {
            var results = new List<string>();
            AddRegistryValue(results, Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath");
            AddRegistryValue(results, Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath");
            AddRegistryValue(results, Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath");
            return results;
        }

        [SupportedOSPlatform("windows")]
        private static void AddRegistryValue(List<string> results, RegistryKey hive, string subKey, string valueName)
        {
            try
            {
                using var key = hive.OpenSubKey(subKey);
                if (key?.GetValue(valueName) is string path && !string.IsNullOrWhiteSpace(path))
                    results.Add(path);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to read registry {hive.Name}\\{subKey}\\{valueName}: {ex.Message}", LogLevel.Trace);
            }
        }

        private static List<string> GetLibraryFolders(string steamRoot)
        {
            var folders = new List<string> { steamRoot };
            string[] vdfPaths = [Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf"), Path.Combine(steamRoot, "config", "libraryfolders.vdf")];
            foreach (var vdfPath in vdfPaths.Where(File.Exists))
            {
                try
                {
                    var root = VdfConvert.Deserialize(File.ReadAllText(vdfPath));
                    foreach (var token in root.Value)
                    {
                        if (token is not VProperty entry)
                            continue;

                        // New format: "0" { "path" "..." }. Old format: "1" "D:\\SteamLibrary"
                        string? path = entry.Value switch
                        {
                            VObject obj => (obj["path"] as VValue)?.Value?.ToString(),
                            VValue value when int.TryParse(entry.Key, out _) => value.Value?.ToString(),
                            _ => null
                        };
                        if (!string.IsNullOrWhiteSpace(path))
                            folders.Add(path.Replace(@"\\", @"\"));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to parse {vdfPath}: {ex.Message}", LogLevel.Warning);
                }
            }
            return folders;
        }

        private static SteamGame? ReadManifest(string manifestPath, string library)
        {
            try
            {
                var root = VdfConvert.Deserialize(File.ReadAllText(manifestPath));
                if (root.Value is not VObject state)
                    return null;

                string? Get(string key) => (state[key] as VValue)?.Value?.ToString();
                var appId = Get("appid");
                var name = Get("name");
                var installDir = Get("installdir");
                if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(installDir))
                    return null;

                var fullPath = Path.Combine(library, "steamapps", "common", installDir);
                return Directory.Exists(fullPath) ? new SteamGame(appId, name, PlatformPaths.NormalizeDir(fullPath)) : null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to parse {manifestPath}: {ex.Message}", LogLevel.Warning);
                return null;
            }
        }
    }
}
