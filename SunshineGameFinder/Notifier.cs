using System.Text;

namespace SunshineGameFinder
{
    internal static class Notifier
    {
        private const string AppName = "Sunshine Game Finder";

        // Requires Windows PowerShell 5.1 (pwsh lacks WinRT projections). Text arrives via env vars so it can't inject script.
        private const string WindowsToastScript = """
            $ErrorActionPreference = 'Stop'
            [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
            [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
            $title = [System.Security.SecurityElement]::Escape($env:SGF_NOTIFY_TITLE)
            $message = [System.Security.SecurityElement]::Escape($env:SGF_NOTIFY_MESSAGE)
            $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
            $xml.LoadXml("<toast><visual><binding template='ToastGeneric'><text>$title</text><text>$message</text></binding></visual></toast>")
            $appId = '{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}\WindowsPowerShell\v1.0\powershell.exe'
            [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($appId).Show([Windows.UI.Notifications.ToastNotification]::new($xml))
            """;

        public static void Send(string title, string message)
        {
            try
            {
                bool sent;
                if (OperatingSystem.IsWindows())
                {
                    var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(WindowsToastScript));
                    sent = ProcessRunner.Run("powershell.exe",
                        ["-NoProfile", "-NonInteractive", "-EncodedCommand", encoded],
                        new Dictionary<string, string> { ["SGF_NOTIFY_TITLE"] = title, ["SGF_NOTIFY_MESSAGE"] = message });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    sent = ProcessRunner.Run("osascript",
                        ["-e", "on run argv", "-e", "display notification (item 2 of argv) with title (item 1 of argv)", "-e", "end run", title, message]);
                }
                else
                {
                    sent = ProcessRunner.Run("notify-send", ["-a", AppName, title, message]);
                }

                if (!sent)
                    Logger.Log("Failed to send system notification", LogLevel.Warning);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to send system notification: {ex.Message}", LogLevel.Warning);
            }
        }
    }
}
