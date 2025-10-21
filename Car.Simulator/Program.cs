using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Car.Shared;

namespace Car.Simulator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Load ThingSpeak Write API key from environment (don’t hardcode secrets)
            var writeKey = Environment.GetEnvironmentVariable("THINGSPEAK_WRITE_KEY");
            if (string.IsNullOrWhiteSpace(writeKey))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Set the THINGSPEAK_WRITE_KEY environment variable first.");
                Console.ResetColor();
                return;
            }

            // Single HttpClient instance; short timeout to avoid hangs
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var rng = new Random();

            // Initial telemetry
            double speed = 0;   // km/h
            double fuel = 100; // %
            double temp = 90;  // °C
            int rpm = 900; // rev/min

            // Allow clean shutdown with Ctrl+C
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;  // don’t terminate immediately
                cts.Cancel();     // signal the loop to stop
            };

            Console.WriteLine("Car Simulator is running... Press Ctrl + C to stop.");

            while (!cts.IsCancellationRequested)
            {
                // 1) Speed: random +/- up to 3 km/h, clamp to [0..120]
                speed = Math.Clamp(speed + rng.NextDouble() * 6 - 3, 0, 120);

                // 2) RPM: random wiggle plus relation to speed, clamp to [800..6000]
                rpm = (int)Math.Clamp(rpm + rng.Next(-300, 301) + speed * 20, 800, 6000);

                // 3) Temp: slight drift up when fast (>80), down when slow, with jitter; clamp [80..100]
                temp = Math.Clamp(
                    temp + (speed > 80 ? 0.2 : -0.1) + (rng.NextDouble() * 0.2 - 0.1),
                    80, 100
                );

                // 4) Fuel: drain slowly; scale by speed (use 120 to match speed cap)
                fuel = Math.Clamp(fuel - (speed / 120.0) * 0.02, 0, 100);

                // Build the data point
                var carData = new CarModel(
                    Timestamp: DateTimeOffset.UtcNow,
                    Rpm: rpm,
                    SpeedKph: speed,
                    FuelPercent: fuel,
                    EngineTempC: temp
                );

                // Prepare ThingSpeak form fields
                var form = new Dictionary<string, string>
                {
                    ["api_key"] = writeKey,
                    ["field1"] = carData.Rpm.ToString(),            // RPM
                    ["field2"] = carData.SpeedKph.ToString("F1"),   // Speed
                    ["field3"] = carData.FuelPercent.ToString("F1"),// Fuel
                    ["field4"] = carData.EngineTempC.ToString("F1") // Temp
                };

                try
                {
                    using var content = new FormUrlEncodedContent(form);
                    var resp = await http.PostAsync("https://api.thingspeak.com/update.json", content, cts.Token);

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed to send data to ThingSpeak: {(int)resp.StatusCode} - {resp.ReasonPhrase}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Data sent! RPM={rpm}, Speed={speed:F1}, Fuel={fuel:F1}, Temp={temp:F1}");
                        Console.ResetColor();
                    }
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    break; // Ctrl+C pressed
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Network error: {ex.Message}");
                    Console.ResetColor();
                }

                // Wait between samples (ThingSpeak free tier often needs ~15s; increase if you see 0 responses)
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                    // If updates are dropped (API returns 0), change to: TimeSpan.FromSeconds(15)
                }
                catch (OperationCanceledException) { break; }
            }

            Console.WriteLine("Simulator stopped. Bye!");
        }
    }
}
