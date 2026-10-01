# Setup
Thanks for wanting to help out or improve this scanning tool. Install the .NET 10 SDK, then pull the code down and open `SunshineGameFinder/SunshineGameFinder.sln` in Visual Studio 2022 or VS Code.

# Making changes
Check out the [good first issues](https://github.com/JMTK/SunshineGameFinder/issues?q=is%3Aissue+is%3Aopen+label%3A"good+first+issue") list if you're unsure on how to start. Just fork the repo, make your change, and make a PR back to the `main` branch here

# Testing
Run the automated suite from the repository root:

```shell
dotnet test SunshineGameFinder/SunshineGameFinder.sln --configuration Release
```

Tests use temporary directories and the mock `SunshineGameFinder.Tests/Fixtures/apps.json`; they do not read or modify your real Sunshine configuration. Add representative entries to that fixture when covering new config behavior.

Pull requests run the same suite through GitHub Actions.

For manual launch verification, pass a disposable config explicitly with `-c /path/to/apps.json --no-wait`. Never use your real `apps.json` without making a backup first.
