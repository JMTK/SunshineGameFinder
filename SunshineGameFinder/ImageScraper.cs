﻿using System.Text.Json;

namespace SunshineGameFinder
{
    internal class ImageScraper
    {
        static string bucketTemplate = "https://db.lizardbyte.dev/buckets/@FIRSTTWOLETTERS.json";
        static string gameTemplate = "https://db.lizardbyte.dev/games/@ID.json";
        static readonly HttpClient HttpClient = new HttpClient();
        private class GamesForBucket
        {
            public string name { get; set; }
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Artwork
        {
            public int id { get; set; }
            public string url { get; set; }
        }

        public class Cover
        {
            public int id { get; set; }
            public string url { get; set; }
        }

        public class Genre
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        public class Game
        {
            public int id { get; set; }
            public List<Artwork> artworks { get; set; }
            public Cover cover { get; set; }
            public List<Genre> genres { get; set; }
            public string name { get; set; }
            public List<Screenshot> screenshots { get; set; }
            public string slug { get; set; }
            public string summary { get; set; }
            public List<Theme> themes { get; set; }
            public string url { get; set; }
        }

        public class Screenshot
        {
            public int id { get; set; }
            public string url { get; set; }
        }

        public class Theme
        {
            public int id { get; set; }
            public string name { get; set; }
        }



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
            var rawJson = await (await HttpClient.GetAsync(bucketTemplate.Replace("@FIRSTTWOLETTERS", string.Join("", gameName.Take(2)).ToLower()))).Content.ReadAsStringAsync();
            var dict = JsonSerializer.Deserialize<Dictionary<int, GamesForBucket>>(rawJson);
            KeyValuePair<int, GamesForBucket>? FindGameFuzzy(double percentage)
            {
                return dict.FirstOrDefault(kvp => CalculateSimilarity(kvp.Value.name.ToLower(), gameName.ToLower()) > (percentage / 100));
            }
            var game = FindGameFuzzy(90) ?? FindGameFuzzy(75);
            if (game == null)
            {
                //couldn't find game
                return -1;
            }
            return game.Value.Key;
        }

        public static async Task<string> SaveIGDBImageToCoversFolder(string gameName, string coversFolderPath)
        {
            try
            {
                int gameId = await GetIDForGame(gameName);
                var rawJson = await (await HttpClient.GetAsync(gameTemplate.Replace("@ID", gameId.ToString()))).Content.ReadAsStringAsync();
                var game = JsonSerializer.Deserialize<Game>(rawJson);
                if (game == null) return null;
                var coverUrl = game.cover.url;
                var stream = await (await HttpClient.GetAsync("https:" + coverUrl.Replace("thumb", "cover_big"))).Content.ReadAsStreamAsync();

                string fullpath = coversFolderPath + gameId.ToString() + ".png";
                using FileStream fs = new(fullpath, FileMode.OpenOrCreate);
                stream.Position = 0;
                await stream.CopyToAsync(fs);
                return fullpath;
            }
            catch(Exception)
            {
                return null;
            }
        }
    }
}
