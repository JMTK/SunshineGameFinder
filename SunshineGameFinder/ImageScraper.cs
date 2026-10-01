using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SunshineGameFinder
{
    internal class BucketGame
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    internal class IgdbCover
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    internal class IgdbGame
    {
        [JsonPropertyName("cover")]
        public IgdbCover? Cover { get; set; }
    }

    // Source-generated so it keeps working in trimmed release builds
    [JsonSerializable(typeof(Dictionary<int, BucketGame>))]
    [JsonSerializable(typeof(IgdbGame))]
    internal partial class ImageScraperJsonContext : JsonSerializerContext
    {
    }

    internal partial class ImageScraper
    {
        static string bucketTemplate = "https://raw.githubusercontent.com/LizardByte/GameDB/gh-pages/buckets/@FIRSTTWOLETTERS.json";
        static string gameTemplate = "https://raw.githubusercontent.com/LizardByte/GameDB/gh-pages/games/@ID.json";
        static readonly HttpClient HttpClient = new HttpClient();
        static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        [GeneratedRegex(@"\.(jpe?g|webp)$", RegexOptions.IgnoreCase)]
        private static partial Regex ImageExtensionRegex();


        /// <summary>
        /// https://stackoverflow.com/a/40775015/1799147
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static int LevenshteinDistance(string source, string target)
        {
            // degenerate cases
            if (source == target) return 0;
            if (source.Length == 0) return target.Length;
            if (target.Length == 0) return source.Length;

            // create two work vectors of integer distances
            int[] v0 = new int[target.Length + 1];
            int[] v1 = new int[target.Length + 1];

            // initialize v0 (the previous row of distances)
            // this row is A[0][i]: edit distance for an empty s
            // the distance is just the number of characters to delete from t
            for (int i = 0; i < v0.Length; i++)
                v0[i] = i;

            for (int i = 0; i < source.Length; i++)
            {
                // calculate v1 (current row distances) from the previous row v0

                // first element of v1 is A[i+1][0]
                //   edit distance is delete (i+1) chars from s to match empty t
                v1[0] = i + 1;

                // use formula to fill in the rest of the row
                for (int j = 0; j < target.Length; j++)
                {
                    var cost = (source[i] == target[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min(v1[j] + 1, Math.Min(v0[j + 1] + 1, v0[j] + cost));
                }

                // copy v1 (current row) to v0 (previous row) for next iteration
                for (int j = 0; j < v0.Length; j++)
                    v0[j] = v1[j];
            }

            return v1[target.Length];
        }

        /// <summary>
        /// https://stackoverflow.com/a/40775015/1799147
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static double CalculateSimilarity(string source, string target)
        {
            if ((source == null) || (target == null)) return 0.0;
            if ((source.Length == 0) || (target.Length == 0)) return 0.0;
            if (source == target) return 1.0;

            int stepsToSame = LevenshteinDistance(source, target);
            return (1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length)));
        }

        /// <summary>
        /// Using LizardByte's pre-scraped buckets, get the ID for a game, and then download that image to the appropriate folder
        /// </summary>
        private static async Task<int> GetIDForGame(string gameName)
        {
            try
            {
                var bucketUrl = bucketTemplate.Replace("@FIRSTTWOLETTERS", string.Join("", gameName.Take(2)).ToLowerInvariant());
                using var response = await HttpClient.GetAsync(bucketUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Log($"\t\tNo games bucket found for: {gameName}", LogLevel.Warning);
                    return -1;
                }
                var rawJson = await response.Content.ReadAsStringAsync();
                var dict = JsonSerializer.Deserialize(rawJson, ImageScraperJsonContext.Default.DictionaryInt32BucketGame);
                
                if (dict == null || dict.Count == 0)
                {
                    Logger.Log($"\t\tNo games found in bucket for: {gameName}", LogLevel.Warning);
                    return -1;
                }

                // Pick the best match rather than the first one over the threshold
                var target = gameName.ToLowerInvariant();
                var best = dict
                    .Where(kvp => !string.IsNullOrEmpty(kvp.Value?.Name))
                    .Select(kvp => (Id: kvp.Key, Score: CalculateSimilarity(kvp.Value.Name!.ToLowerInvariant(), target)))
                    .OrderByDescending(m => m.Score)
                    .FirstOrDefault();
                
                if (best.Score < 0.75)
                {
                    Logger.Log($"\t\tCould not find game ID for: {gameName}", LogLevel.Warning);
                    return -1;
                }
                
                return best.Id;
            }
            catch (Exception ex)
            {
                Logger.Log($"\t\tError getting game ID for {gameName}: {ex.Message}", LogLevel.Error);
                return -1;
            }
        }

        public static async Task<string?> SaveIGDBImageToCoversFolder(string gameName, string coversFolderPath)
        {
            try
            {
                // Ensure the covers directory exists
                if (!Directory.Exists(coversFolderPath))
                {
                    Directory.CreateDirectory(coversFolderPath);
                }
                
                int gameId = await GetIDForGame(gameName);
                if (gameId == -1)
                {
                    return null;
                }

                string fullpath = Path.Combine(coversFolderPath, gameId.ToString() + ".png");
                if (File.Exists(fullpath) && IsPng(await File.ReadAllBytesAsync(fullpath)))
                {
                    return fullpath;
                }
                
                var gameUrl = gameTemplate.Replace("@ID", gameId.ToString());
                var rawJson = await HttpClient.GetStringAsync(gameUrl);
                var game = JsonSerializer.Deserialize(rawJson, ImageScraperJsonContext.Default.IgdbGame);
                
                if (game == null)
                {
                    Logger.Log($"\t\tFailed to deserialize game data for ID: {gameId}", LogLevel.Warning);
                    return null;
                }
                
                var coverUrl = game.Cover?.Url;
                if (string.IsNullOrEmpty(coverUrl))
                {
                    Logger.Log($"\t\tNo cover URL found for game: {gameName} (ID: {gameId})", LogLevel.Warning);
                    return null;
                }

                // IGDB serves JPEG by default but Sunshine only accepts real PNG files, so request the .png variant
                var imageUrl = (coverUrl.StartsWith("//") ? "https:" + coverUrl : coverUrl).Replace("t_thumb", "t_cover_big");
                imageUrl = ImageExtensionRegex().Replace(imageUrl, ".png");
                var bytes = await HttpClient.GetByteArrayAsync(imageUrl);
                if (!IsPng(bytes))
                {
                    Logger.Log($"\t\tDownloaded cover for {gameName} is not a valid PNG: {imageUrl}", LogLevel.Warning);
                    return null;
                }

                await File.WriteAllBytesAsync(fullpath, bytes);
                return fullpath;
            }
            catch (Exception ex)
            {
                Logger.Log($"\t\tError downloading cover for {gameName}: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        private static bool IsPng(byte[] bytes) => bytes.AsSpan().StartsWith(PngSignature);
    }
}
