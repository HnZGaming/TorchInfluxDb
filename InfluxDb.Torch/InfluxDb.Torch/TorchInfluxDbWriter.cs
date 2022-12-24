using System;
using System.Threading.Tasks;
using InfluxDb.Client.Write;
using NLog;
using Utils.General;

namespace InfluxDb.Torch
{
    /// <summary>
    /// Massive syntax sugar
    /// </summary>
    public static class TorchInfluxDbWriter
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        internal static IInfluxDbWriteEndpoints WriteEndpoints { private get; set; }
        internal static InfluxDbWriteClient WriteClient { private get; set; }
        internal static bool Enabled { get; set; }

        public static InfluxDbPoint Measurement(string measurement)
        {
            measurement.ThrowIfNullOrEmpty(nameof(measurement));
            WriteClient.ThrowIfNull("Integration not initialized");

            var point = new InfluxDbPoint(measurement);

            point.Time(DateTime.UtcNow); // default time

            return point;
        }

        public static void Write(this InfluxDbPoint point)
        {
            point.ThrowIfNull(nameof(point));
            WriteClient.ThrowIfNull("Integration not initialized");
            if (!Enabled)
            {
                Log.Debug($"plugin disabled: {point}");
                return;
            }

            WriteClient.Queue(point.BuildLine());
        }

        public static async Task WriteLineAsync(string line)
        {
            line.ThrowIfNullOrEmpty(nameof(line));
            WriteEndpoints.ThrowIfNull("Integration not initialized");
            if (!Enabled)
            {
                throw new Exception("Not enabled");
            }

            await WriteEndpoints.WriteAsync(new[] { line });
        }
    }
}