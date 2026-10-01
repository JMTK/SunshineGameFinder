## Sunshine Game Finder
Searches your computer for various common game install paths for the [Sunshine](https://github.com/LizardByte/Sunshine) application. After running it, all games that did not already exist will be added to the `apps.json`, meaning your [Moonlight client](https://github.com/moonlight-stream/moonlight-qt) should see them next time it is started.

## Running the program from release
1. Download the [latest release](https://github.com/JMTK/SunshineGameFinder/releases) for your platform (`win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`).
2. **Windows:** the program elevates itself (UAC) because Sunshine's config lives in `Program Files`.
3. **Linux/macOS:** extract with `tar -xzf` and run `./SunshineGameFinder` as your normal user (not `sudo`) - Sunshine's config is in `~/.config/sunshine/apps.json`.

Alternatively install it as a .NET tool from GitHub Packages:
```
dotnet tool install -g SunshineGameFinder --add-source https://nuget.pkg.github.com/JMTK/index.json
```

## What gets detected
| Source | How |
|---|---|
| Steam (Windows/Linux/macOS) | Steam install from the registry (Windows) or standard locations, all libraries from `libraryfolders.vdf`, games from `appmanifest_*.acf`. Launched via `steam://rungameid/<id>` |
| Rockstar Games Launcher | Registry install folders + `Program Files\Rockstar Games`. Uses `Play*.exe` so the launcher starts |
| Xbox / Game Pass, EA, Epic, Ubisoft | Default install folders, exe heuristics |
| Anything else | `--addlDirectories` |

### Anti-cheat games (EasyAntiCheat, BattlEye)
Games such as Rematch fail when their exe is started directly because the anti-cheat bootstrapper and Steam DRM are skipped. Steam games are now added as `steam://rungameid/<id>` so Steam starts them exactly like the Play button. For other launchers `start_protected_game.exe` is preferred when present. To migrate existing exe-based entries, run once with `--force`.

## Command Line Arguments
| Option                                    | Description                                                                                               |
|-------------------------------------------|-----------------------------------------------------------------------------------------------------------|
| -d, --addlDirectories <addlDirectories>   | Additional platform directories to search. ONLY looks for game directories in the top level of this folder. |
| -exeExclude, --addlExeExclusionWords      | More words to exclude if the EXE matches any part of this <addlExeExclusionWords>                         |
| -c, --sunshineConfigLocation <sunshineConfigLocation> | Specify the Sunshine apps.json location [default: `C:\Program Files\Sunshine\config\apps.json` on Windows, `~/.config/sunshine/apps.json` on Linux/macOS] |
| -f, --force                               | Force update apps.json even if games already existed [default: False]                                     |
| -ru, --remove-uninstalled                 | Removes apps whose exes can not be found or whose Steam game is no longer installed [default: False]       |
| -desktop, --ensure-desktop-app            | Ensures that the 'Desktop' app is there [default: False]                                                  |
| -bigpicture, --ensure-steam-big-picture    | Ensures that the 'Steam Big Picture' app is there [default: False]                                        |
| --no-wait                                 | Don't wait for user input after finishing                                                                  |
| -n, --notify                              | Send a system notification with the results (Windows toast, `notify-send` on Linux, Notification Center on macOS). Combine with `--no-wait` for scheduled/background runs |
| --version                                 | Show version information                                                                                   |
| -?, -h, --help                            | Show help and usage information                                                                            |

## Releases
Every push to `main` builds all platforms, bumps the patch version, creates a GitHub Release and publishes the .NET tool to GitHub Packages. Put `#minor` or `#major` in the commit message (or use the manual workflow trigger) for bigger bumps.

## Running it from Visual Studio
To run it, open the solution and click "Run" or F5 in Visual Studio

![image](https://user-images.githubusercontent.com/877114/227733782-922c06f1-12b9-44bc-bbf4-0bd012559440.png)

![image](https://user-images.githubusercontent.com/877114/227733789-6068f7ff-7c7e-40c2-b461-ae82d2c708c3.png)

## Tests
Run the fixture-backed test suite locally with:

```shell
dotnet test SunshineGameFinder/SunshineGameFinder.sln --configuration Release
```

The suite uses a mock `apps.json` and temporary directories, so it does not touch your installed Sunshine configuration. The same command runs automatically for pull requests.

## Contributing
Consult the CONTRIBUTING.md for more info
