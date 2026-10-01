namespace SunshineGameFinder.Tests;

public sealed class SteamLibraryTests
{
    [Theory]
    [InlineData("steam://rungameid/12345")]
    [InlineData("setsid steam steam://rungameid/12345")]
    [InlineData("open steam://rungameid/12345")]
    public void GetAppIdReadsLaunchCommands(string command)
    {
        var app = new SunshineApp { Detached = [command] };

        Assert.Equal("12345", SteamLibrary.GetAppId(app));
    }

    [Theory]
    [InlineData("228980", "Steamworks Common Redistributables")]
    [InlineData("1", "Proton Experimental")]
    [InlineData("2", "Steam Linux Runtime 3.0")]
    public void IsToolRecognizesSteamRuntimeEntries(string appId, string name)
    {
        Assert.True(SteamLibrary.IsTool(new SteamGame(appId, name, "unused")));
    }
}