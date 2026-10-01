using System.Text.Json;

namespace SunshineGameFinder.Tests;

public sealed class SunshineConfigTests
{
    [Fact]
    public void MockAppsJsonDeserializesSunshineValues()
    {
        var json = File.ReadAllText(FixturePath("apps.json"));

        var config = JsonSerializer.Deserialize<SunshineConfig>(json);

        Assert.NotNull(config);
        Assert.Equal(2, config.apps?.Count);
        var existingGame = Assert.Single(config.apps!, app => app.Name == "Existing Game");
        Assert.Equal("true", existingGame.Elevated);
        Assert.Equal("5", existingGame.ExitTimeout);
        Assert.Equal("false", Assert.Single(existingGame.PrepCmd!).Elevated);
        Assert.Equal("12345", SteamLibrary.GetAppId(Assert.Single(config.apps!, app => app.Name == "Steam Game")));
    }

    [Fact]
    public void UpdateConfigBacksUpAndWritesMockAppsJson()
    {
        using var directory = new TemporaryDirectory();
        var appsJson = Path.Combine(directory.Path, "apps.json");
        File.Copy(FixturePath("apps.json"), appsJson);
        var config = JsonSerializer.Deserialize<SunshineConfig>(File.ReadAllText(appsJson))!;
        config.apps!.Add(new SunshineApp { Name = "New Game", Cmd = "game.exe", WorkingDir = "" });

        var updated = FileWriter.UpdateConfig(appsJson, config);

        Assert.True(updated);
        Assert.Single(Directory.GetFiles(directory.Path, "*.bak"));
        var written = JsonSerializer.Deserialize<SunshineConfig>(File.ReadAllText(appsJson));
        Assert.Contains(written!.apps!, app => app.Name == "New Game");
    }

    private static string FixturePath(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
}