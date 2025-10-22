using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Car.Analytics
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 1) Read config from environment and trim
            var channelId = Environment.GetEnvironmentVariable("THINGSPEAK_CHANNEL_ID")?.Trim();
            var readKey = Environment.GetEnvironmentVariable("THINGSPEAK_READ_KEY")?.Trim();

            if (string.IsNullOrWhiteSpace(channelId) || string.IsNullOrWhiteSpace(readKey))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Set THINGSPEAK_CHANNEL_ID and THINGSPEAK_READ_KEY environment first");
                Console.ResetColor();
                return;
            }

            // 2) Ask the user which window to analyze
            Console.WriteLine("Choose data range: ");
            Console.WriteLine(" 1) Last 24 hours");
            Console.WriteLine(" 2) Last 100 points");
            Console.Write("Select: ");
            var choice = Console.ReadLine();

            string query = choice == "1" ? "days=1" : "results=100";
            string url = $"https://api.thingspeak.com/channels/{channelId}/feeds.json?api_key={readKey}&{query}";

            // 3) Fetch JSON
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            string json;
            try
            {
                json = await http.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Network error: {ex.Message}");
                Console.ResetColor();
                return;
            }

            // 4) Parse and collect RPM + Speed
            var speeds = new List<double>();
            var rpms = new List<int>();   // <-- make this int

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("feeds", out var feeds) ||
                    feeds.ValueKind != JsonValueKind.Array)
                {
                    Console.WriteLine("No 'feeds' array found in response. Check channel/keys.");
                    return;
                }

                foreach (var f in feeds.EnumerateArray())
                {
                    // field1 = RPM (int)
                    if (f.TryGetProperty("field1", out var rEl) &&
                        rEl.ValueKind == JsonValueKind.String &&
                        int.TryParse(rEl.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var rpm))
                    {
                        rpms.Add(rpm);
                    }

                    // field2 = SpeedKph (double) — accept "4,0" or "4.0"
                    if (f.TryGetProperty("field2", out var sEl) &&
                        sEl.ValueKind == JsonValueKind.String)
                    {
                        var sStr = sEl.GetString();
                        if (!string.IsNullOrWhiteSpace(sStr))
                        {
                            sStr = sStr.Replace(',', '.'); // normalize
                            if (double.TryParse(sStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var sp))
                                speeds.Add(sp);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                Console.WriteLine("Unexpected JSON format from ThingSpeak. Verify channel ID and keys.");
                return;
            }

            // 5) Basic validation
            if (speeds.Count == 0 || rpms.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No usable data points yet. Let the simulator run a bit and try again.");
                Console.ResetColor();
                return;
            }

            // 6) Compute averages
            double avgSpeed = 0;
            foreach (var s in speeds) avgSpeed += s;
            avgSpeed /= speeds.Count;

            double avgRpm = 0;
            foreach (var r in rpms) avgRpm += r;
            avgRpm /= rpms.Count;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine($"Data points analyzed: Speed={speeds.Count}, RPM={rpms.Count}");
            Console.WriteLine($"Average speed: {avgSpeed:F1} km/h");
            Console.WriteLine($"Average RPM:   {avgRpm:F0}");
            Console.ResetColor();
        }
    }
}
