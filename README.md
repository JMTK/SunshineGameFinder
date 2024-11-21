## Sunshine Game Finder
Searches your computer for various common game install paths for the [Sunshine](https://github.com/LizardByte/Sunshine) application. After running it, all games that did not already exist will be added to the `apps.json`, meaning your [Moonlight client](https://github.com/moonlight-stream/moonlight-qt) should see them next time it is started.

## Running the program from release
1. Download the [latest release](https://github.com/JMTK/SunshineGameFinder/releases). 
2. Ensure that you run the program as administrator depending on the permissions of the user executing it since it will be attempting to look through some of the program file directories.

## Command Line Arguments
| Option                                    | Description                                                                                               |
|-------------------------------------------|-----------------------------------------------------------------------------------------------------------|
| -d, --addlDirectories <addlDirectories>   | Additional platform directories to search. ONLY looks for game directories in the top level of this folder. |
| -exeExclude, --addlExeExclusionWords      | More words to exclude if the EXE matches any part of this <addlExeExclusionWords>                         |
| -c, --sunshineConfigLocation <sunshineConfigLocation> | Specify the Sunshine apps.json location [default: C:\Program Files\Sunshine\config\apps.json] |
| -f, --force                               | Force update apps.json even if games already existed [default: False]                                     |
| -ru, --remove-uninstalled                 | Removes apps whose exes can not be found [default: False]                                                 |
| -desktop, --ensure-desktop-app            | Ensures that the 'Desktop' app is there [default: False]                                                  |
| -bigpicture, --ensure-steam-big-picture    | Ensures that the 'Steam Big Picture' app is there [default: False]                                        |
| --version                                 | Show version information                                                                                   |
| -?, -h, --help                            | Show help and usage information                                                                            |

## Running it from Visual Studio
To run it, open the solution and click "Run" or F5 in Visual Studio

![image](https://user-images.githubusercontent.com/877114/227733782-922c06f1-12b9-44bc-bbf4-0bd012559440.png)

![image](https://user-images.githubusercontent.com/877114/227733789-6068f7ff-7c7e-40c2-b461-ae82d2c708c3.png)

## Contributing
Consult the CONTRIBUTING.md for more info
