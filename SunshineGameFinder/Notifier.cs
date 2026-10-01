#if WINDOWS_BUILD
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
#endif

namespace SunshineGameFinder
{
    internal static class Notifier
    {
        private const string AppName = "Sunshine Game Finder";

        public static void Send(string title, string message)
        {
            try
            {
                bool sent = false;
#if WINDOWS_BUILD
                if (OperatingSystem.IsWindows())
                {
                    sent = ShowWindowsNotification(title, message);
                }
#endif
                if (OperatingSystem.IsMacOS())
                {
                    sent = ProcessRunner.Run("osascript",
                        ["-e", "on run argv", "-e", "display notification (item 2 of argv) with title (item 1 of argv)", "-e", "end run", title, message]);
                }
                else if (OperatingSystem.IsLinux())
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

#if WINDOWS_BUILD
        [SupportedOSPlatform("windows")]
        private static bool ShowWindowsNotification(string title, string message)
        {
            try
            {
                var nid = new NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                    hWnd = IntPtr.Zero,
                    uID = 1001,
                    uFlags = 0x00000002 | 0x00000010, // NIF_ICON | NIF_INFO
                    hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32516), // IDI_INFORMATION
                    szInfoTitle = title.Length > 63 ? title[..63] : title,
                    szInfo = message.Length > 255 ? message[..255] : message,
                    dwInfoFlags = 0x00000001 // NIIF_INFO
                };

                Shell_NotifyIcon(0, ref nid); // NIM_ADD
                Thread.Sleep(1000);
                Shell_NotifyIcon(2, ref nid); // NIM_DELETE
                return true;
            }
            catch
            {
                return false;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "Shell_NotifyIconW")]
        private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadIconW")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);
#endif
    }
}
