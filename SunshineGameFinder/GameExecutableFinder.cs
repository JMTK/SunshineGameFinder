namespace SunshineGameFinder
{
    internal static class GameExecutableFinder
    {
        // Skip reparse points so symlink loops (common on Linux, e.g. Wine dosdevices) can't recurse forever
        private static readonly EnumerationOptions RecursiveOptions = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        private static readonly string[] UnixExecutableExtensions = ["", ".sh", ".x86_64", ".x86", ".appimage"];

        public static string? Find(DirectoryInfo gameDir, string gameName, IReadOnlyCollection<string> exclusionWords, bool isRockstar)
        {
            var files = OperatingSystem.IsWindows()
                ? gameDir.EnumerateFiles("*.exe", RecursiveOptions)
                : gameDir.EnumerateFiles("*", RecursiveOptions).Where(IsUnixExecutable);

            return files
                .Where(f =>
                {
                    var name = Path.GetFileNameWithoutExtension(f.Name);
                    return IsNameMatch(name, gameDir.Name, gameName) || !exclusionWords.Any(ew => f.Name.Contains(ew, StringComparison.OrdinalIgnoreCase));
                })
                .Select(f => (File: f, Depth: GetDepth(gameDir, f)))
                .OrderBy(c => Rank(c.File, c.Depth, gameDir.Name, gameName, isRockstar))
                .ThenBy(c => c.Depth)
                .ThenByDescending(c => c.File.Length)
                .Select(c => c.File.FullName)
                .FirstOrDefault();
        }

        private static int Rank(FileInfo file, int depth, string dirName, string gameName, bool isRockstar)
        {
            var name = Path.GetFileNameWithoutExtension(file.Name);
            if (name.Equals("gamelaunchhelper", StringComparison.OrdinalIgnoreCase))
                return 0; // Xbox / Game Pass
            if (name.Equals("start_protected_game", StringComparison.OrdinalIgnoreCase))
                return 1; // EasyAntiCheat bootstrapper; launching the raw game exe skips EAC
            if (isRockstar && depth == 0 && name.StartsWith("Play", StringComparison.OrdinalIgnoreCase))
                return 2; // PlayGTAV.exe / PlayRDR2.exe go through the Rockstar Games Launcher
            if (IsNameMatch(name, dirName, gameName))
                return 3;
            return 4;
        }

        private static bool IsNameMatch(string exeName, string dirName, string gameName) =>
            exeName.Equals(dirName, StringComparison.OrdinalIgnoreCase) || exeName.Equals(gameName, StringComparison.OrdinalIgnoreCase);

        private static int GetDepth(DirectoryInfo root, FileInfo file)
        {
            var relative = Path.GetRelativePath(root.FullName, file.DirectoryName ?? root.FullName);
            return relative == "." ? 0 : relative.Split(Path.DirectorySeparatorChar).Length;
        }

        private static bool IsUnixExecutable(FileInfo file)
        {
            if (OperatingSystem.IsWindows())
                return false;
            if (!UnixExecutableExtensions.Contains(file.Extension.ToLowerInvariant()))
                return false;
            return (file.UnixFileMode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
        }
    }
}
