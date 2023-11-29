// See https://aka.ms/new-console-template for more information
// See https://aka.ms/new-console-template for more information

using Microsoft.Win32;

internal partial class Program
{

    private static void Main(string[] args)
    {
        var installPaths = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            var registryPaths = new List<string>()
            {
                @"SOFTWARE\Wow6432Node\Valve\Steam",
                @"SOFTWARE\Valve\Steam"
            };

            foreach (var path in registryPaths)
            {
                if (path.ToLower().Contains("steam"))
                {
                    RegistryKey? steamRegistry = Registry.LocalMachine.OpenSubKey(path);
                    if (steamRegistry != null && steamRegistry.GetValue("SteamPath") != null)
                    {
                        installPaths.Add(steamRegistry.GetValue("SteamPath").ToString());
                    }
                    else
                    {
                        steamRegistry = Registry.CurrentUser.OpenSubKey(path);
                        if (steamRegistry != null && steamRegistry.GetValue("SteamPath") != null)
                        {
                            //temp string here to remove drive letter and replace with *
                            string temp = steamRegistry.GetValue("SteamPath").ToString();
                            temp = MakePathGooder(temp);
                            installPaths.Add(temp);
                        }
                    }
                }

            }

            foreach (var path in installPaths)
            {
                Console.WriteLine(path);

            }

            string MakePathGooder(string path)
            {
                string temp = string.Concat("*", path.AsSpan(1));
                string gooderPath = temp.Replace('/', Path.DirectorySeparatorChar);

                return gooderPath;
            }
        }
        else
        {
            string userInfo = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (OperatingSystem.IsLinux())
            {
                var linuxPaths = new List<string>()
                {
                    @"/.local/share/Steam/SteamApps/common",
                    @"/.local/share/Steam/steamapps/common",
                    @"/.local/share/Steam/SteamApps/compatdata",
                    @"/.local/share/Steam/steamapps/compatdata"
                };

                foreach (var path in linuxPaths)
                {
                    string linuxInstallPath = string.Concat(userInfo, path);
                    if (Directory.Exists(linuxInstallPath))
                    {
                        installPaths.Add(linuxInstallPath);
                    }
                }

                
            }
            else if (OperatingSystem.IsMacOS())
            {
                var macOSPaths = new List<string>()
                {
                    @"/Library/Application Support/Steam/SteamApps/common"
                };

                foreach (var path in macOSPaths)
                {
                    string macInstallPath = string.Concat(userInfo, path);
                    installPaths.Add(macInstallPath);
                }

            }
        }
    }
}

