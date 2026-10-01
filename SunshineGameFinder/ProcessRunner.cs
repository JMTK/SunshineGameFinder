using System.Diagnostics;

namespace SunshineGameFinder
{
    internal static class ProcessRunner
    {
        /// <summary>
        /// Runs a process to completion. Arguments are passed via ArgumentList so no shell quoting is involved.
        /// </summary>
        public static bool Run(string fileName, IEnumerable<string> arguments, IDictionary<string, string>? environment = null)
        {
            var psi = new ProcessStartInfo(fileName)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (var argument in arguments)
                psi.ArgumentList.Add(argument);
            if (environment != null)
            {
                foreach (var (key, value) in environment)
                    psi.Environment[key] = value;
            }

            using var proc = Process.Start(psi);
            if (proc == null)
                return false;

            var stdout = proc.StandardOutput.ReadToEndAsync();
            var stderr = proc.StandardError.ReadToEndAsync();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                Logger.Log($"'{fileName}' exited with code {proc.ExitCode}: {stderr.Result.Trim()} {stdout.Result.Trim()}", LogLevel.Trace);
            }
            return proc.ExitCode == 0;
        }
    }
}
