using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using Utils.General;

namespace InfluxDb.Client.Write
{
    /// <summary>
    /// Holds onto points until the next time interval.
    /// Reduces the number of HTTP calls.
    /// </summary>
    public sealed class InfluxDbWriteClient
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        readonly IInfluxDbWriteEndpoints _endpoint;
        readonly ThreadSafeThrottle<string> _throttle;

        public InfluxDbWriteClient(IInfluxDbWriteEndpoints endpoint, TimeSpan throttleInterval)
        {
            _endpoint = endpoint;

            _throttle = new ThreadSafeThrottle<string>(
                throttleInterval,
                ps => OnThrottleFlush(ps));
        }

        public void Queue(string point)
        {
            point.ThrowIfNull(nameof(point));

            _throttle.Add(point);
        }

        void OnThrottleFlush(IReadOnlyList<string> points)
        {
            Log.Trace($"pending points: {points.Count}");

            // don't send if empty
            if (points.Count == 0) return;

            _endpoint.WriteAsync(points.ToArray()).Forget(Log);
        }

        public void SetRunning(bool enable)
        {
            if (enable)
            {
                StartWriting();
            }
            else
            {
                StopWriting();
            }
        }

        public void StartWriting()
        {
            Log.Debug("start writing");
            
            if (!_throttle.Start())
            {
                Log.Warn("Aborted starting; already started");
            }
        }

        public void StopWriting()
        {
            Log.Debug("stop writing");

            if (!_throttle.Stop())
            {
                Log.Warn("Aborted stopping; not running");
                return;
            }

            Flush();
        }

        public void Flush()
        {
            _throttle.Flush(); // write remaining points
            Thread.Sleep(1000); // wait for writes to finish
        }
    }
}