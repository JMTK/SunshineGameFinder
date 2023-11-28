// See https://aka.ms/new-console-template for more information

using Microsoft.Win32;

internal class Program
{
    private static void Main(string[] args)
    {
        var registryPaths = new List<string>()
        {
            @"SOFTWARE\Wow6432Node\Valve\Steam",
            @"SOFTWARE\Valve\Steam"
        };

        var installPaths = new List<string>();

        if (OperatingSystem.IsWindows())
        {

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
                string gooderPath = temp.Replace("/", @"\");

                return gooderPath;
            }
        }
    }
}