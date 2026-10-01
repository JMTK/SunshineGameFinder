namespace SunshineGameFinder.Tests;

public sealed class GameExecutableFinderTests
{
    [Fact]
    public void FindPrefersAntiCheatBootstrapper()
    {
        using var directory = new TemporaryDirectory();
        CreateExecutable(directory.Path, "Example Game");
        var bootstrapper = CreateExecutable(directory.Path, "start_protected_game");

        var result = GameExecutableFinder.Find(
            new DirectoryInfo(directory.Path),
            "Example Game",
            ["EasyAntiCheat", "BattlEye"],
            isRockstar: false);

        Assert.Equal(bootstrapper, result);
    }

    [Fact]
    public void FindPrefersRockstarPlayExecutable()
    {
        using var directory = new TemporaryDirectory();
        CreateExecutable(directory.Path, "GTA5");
        var launcher = CreateExecutable(directory.Path, "PlayGTAV");

        var result = GameExecutableFinder.Find(
            new DirectoryInfo(directory.Path),
            "Grand Theft Auto V",
            [],
            isRockstar: true);

        Assert.Equal(launcher, result);
    }

    private static string CreateExecutable(string directory, string name)
    {
        var path = Path.Combine(directory, OperatingSystem.IsWindows() ? $"{name}.exe" : name);
        File.WriteAllText(path, name);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        }

        return path;
    }
}