using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

namespace SunshineGameFinder
{
    internal class FileWriter
    {
        private const string backupFileExtension = "bak";
        private const int backupsToKeep = 5;

        /// <summary>
        /// Creates a backup of the original file and writes the new configuration in it's place.
        /// </summary>
        /// <param name="filePath">The path the configuration will get written too.</param>
        /// <param name="config">The configuration to save.</param>
        /// <returns></returns>
        internal static bool UpdateConfig(string filePath, SunshineConfig config)
        {
            var folderPath = Path.GetDirectoryName(filePath);
            try
            {
                // Insert the current datetime into the file name for the backup to ensure we can track the backups. 
                string backUpFilePath = Path.Combine(folderPath, $"{Path.GetFileNameWithoutExtension(filePath)}_{DateTime.Now.ToString("MMddyyyy_HHmmss")}.{backupFileExtension}");
                File.Move(filePath, backUpFilePath);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string serializedJson = JsonSerializer.Serialize(config, options);
                File.WriteAllText(filePath, serializedJson);
            }
            catch (Exception e)
            {
                Logger.Log($"An error occurred while trying to update the configuraton file at {filePath}. Exception:{e.Message}", LogLevel.Error);
                return false;
            }
            CleanUpBackups(folderPath);
            return true;
        }

        /// <summary>
        /// Ensure the backup files don't blow out.
        /// </summary>
        internal static void CleanUpBackups(string folderPath)
        {
            try
            {
                DirectoryInfo info = new DirectoryInfo(folderPath);
                FileInfo[] files = info.GetFiles().Where(f => f.Extension == $".{backupFileExtension}").OrderByDescending(f => f.CreationTime).ToArray();

                var backupCount = files.Length;

                for (int i = backupsToKeep; i < backupCount; i++)
                {
                    files[i].Delete();
                }
            }
            catch (Exception e)
            {
                Logger.Log($"An error occurred while trying to clean up historic backup files. This should not impact the validity of the new configuration. Exception:{e.Message}", LogLevel.Warning);
            }
        }
    }
}