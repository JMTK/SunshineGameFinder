namespace SunshineGameFinder
{
    internal static class Notifier
    {
        private const string AppName = "Sunshine Game Finder";

        public static void Send(string title, string message)
        {
            try
            {
                bool sent;
                if (OperatingSystem.IsWindows())
                {
                    sent = ProcessRunner.Run("powershell.exe",
                        ["-NoProfile", "-NonInteractive", "-Command",
                         "Add-Type -AssemblyName System.Windows.Forms; " +
                         "$n = New-Object System.Windows.Forms.NotifyIcon; " +
                         "$n.Icon = [System.Drawing.SystemIcons]::Information; " +
                         "$n.Visible = $true; " +
                         "$n.ShowBalloonTip(5000, $env:SGF_NOTIFY_TITLE, $env:SGF_NOTIFY_MESSAGE, [System.Windows.Forms.ToolTipIcon]::Info); " +
                         "Start-Sleep -Milliseconds 1100; " +
                         "$n.Dispose()"],
                        new Dictionary<string, string>
                        {
                            ["SGF_NOTIFY_TITLE"] = title,
                            ["SGF_NOTIFY_MESSAGE"] = message
                        });
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
