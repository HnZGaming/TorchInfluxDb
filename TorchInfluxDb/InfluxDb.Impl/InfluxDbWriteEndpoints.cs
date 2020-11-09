﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TorchUtils;

namespace InfluxDb.Impl
{
    // https://docs.influxdata.com/influxdb/v2.0/write-data/developer-tools/api/
    internal sealed class InfluxDbWriteEndpoints : IDisposable
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        readonly IInfluxDbConfig _config;
        readonly HttpClient _httpClient;
        readonly CancellationTokenSource _cancellationTokenSource;

        public InfluxDbWriteEndpoints(IInfluxDbConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _httpClient.Dispose();
        }

        public async Task WriteAsync(IEnumerable<string> lines)
        {
            lines.ThrowIfNull(nameof(lines));
            if (!lines.Any()) return;

            _config.HostUrl.ThrowIfNullOrEmpty(nameof(_config.HostUrl));
            _config.Organization.ThrowIfNullOrEmpty(nameof(_config.Organization));
            _config.Bucket.ThrowIfNullOrEmpty(nameof(_config.Bucket));

            var url = $"{_config.HostUrl}/api/v2/write?org={_config.Organization}&bucket={_config.Bucket}&precision=ms";
            var req = new HttpRequestMessage(HttpMethod.Post, url);

            var content = string.Join("\n", lines);
            req.Content = new StringContent(content);

            Log.Trace($"content: \n{content}");

            if (!string.IsNullOrEmpty(_config.AuthenticationToken))
            {
                req.Headers.TryAddWithoutValidation("Authorization", $"Token {_config.AuthenticationToken}");
            }

            HttpResponseMessage res;
            try
            {
                res = await _httpClient.SendAsync(req, _cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                if (!_config.SuppressResponseError)
                {
                    Log.Error($"Failed to send: \"{e.Message}\"");
                }

                return;
            }

            using (res)
            {
                if (res.IsSuccessStatusCode) return; // success
                if (_config.SuppressResponseError) return; // ignore error message

                var msgBuilder = new StringBuilder();

                msgBuilder.AppendLine($"Failed to write ({res.StatusCode}: \"{res.ReasonPhrase}\");");
                msgBuilder.AppendLine($"Host URL: {_config.HostUrl}");
                msgBuilder.AppendLine($"Bucket: {_config.Bucket}");
                msgBuilder.AppendLine($"Organization: {_config.Organization}");

                if (_config.AuthenticationToken != null)
                {
                    msgBuilder.AppendLine($"Authentication Token: {_config.AuthenticationToken.HideCredential(4)}");
                }

                foreach (var line in lines)
                {
                    msgBuilder.AppendLine($"Line: {line}");
                }

                Log.Error(msgBuilder.ToString);
            }
        }
    }
}