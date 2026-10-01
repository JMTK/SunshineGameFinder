namespace SunshineGameFinder
{
    internal static class PlatformPaths
    {
        public static StringComparer PathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        /// <summary>
        /// Home directory of the real user, even when invoked through sudo on Linux/macOS.
        /// </summary>
        public static string UserHome
        {
            get
            {
                if (!OperatingSystem.IsWindows() && Environment.UserName == "root")
                {
                    var sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
                    if (!string.IsNullOrEmpty(sudoUser) && sudoUser != "root")
                    {
                        var home = Path.Combine(OperatingSystem.IsMacOS() ? "/Users" : "/home", sudoUser);
                        if (Directory.Exists(home))
                            return home;
                    }
                }
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
        }

        public static string NormalizeDir(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

        public static string GetDefaultSunshineAppsJson()
        {
            var candidates = GetSunshineAppsJsonCandidates().ToList();
            return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
        }

        private static IEnumerable<string> GetSunshineAppsJsonCandidates()
        {
            if (OperatingSystem.IsWindows())
            {
                yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Sunshine", "config", "apps.json");
                yield break;
            }

            var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrEmpty(xdgConfig))
                yield return Path.Combine(xdgConfig, "sunshine", "apps.json");

            yield return Path.Combine(UserHome, ".config", "sunshine", "apps.json");

            if (OperatingSystem.IsLinux())
                yield return Path.Combine(UserHome, ".var", "app", "dev.lizardbyte.app.Sunshine", "config", "sunshine", "apps.json");
        }
    }
}
