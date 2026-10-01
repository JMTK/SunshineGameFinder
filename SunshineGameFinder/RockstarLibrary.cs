using Microsoft.Win32;
using System.Runtime.Versioning;

namespace SunshineGameFinder
{
    internal static class RockstarLibrary
    {
        private static readonly string[] RegistryRoots = [@"SOFTWARE\WOW6432Node\Rockstar Games", @"SOFTWARE\Rockstar Games"];

        /// <summary>
        /// Each installed Rockstar Games Launcher title registers an InstallFolder under HKLM\SOFTWARE\Rockstar Games\&lt;Title&gt;.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static List<(string Name, string InstallFolder)> GetInstalledGames()
        {
            var games = new List<(string Name, string InstallFolder)>();
            foreach (var rootPath in RegistryRoots)
            {
                try
                {
                    using var root = Registry.LocalMachine.OpenSubKey(rootPath);
                    if (root == null)
                        continue;

                    foreach (var subKeyName in root.GetSubKeyNames())
                    {
                        using var gameKey = root.OpenSubKey(subKeyName);
                        if (gameKey?.GetValue("InstallFolder") is string folder && Directory.Exists(folder))
                            games.Add((subKeyName, folder));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to read Rockstar registry key {rootPath}: {ex.Message}", LogLevel.Trace);
                }
            }
            return games;
        }
    }
}
