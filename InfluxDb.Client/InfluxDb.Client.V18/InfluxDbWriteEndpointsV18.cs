using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InfluxDb.Client.Write;
using NLog;
using Utils.General;

namespace InfluxDb.Client.V18
{
    public sealed class InfluxDbWriteEndpointsV18 : IInfluxDbWriteEndpoints
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly InfluxDbAuth _auth;
        readonly HttpClient _httpClient;
        readonly CancellationTokenSource _cancellationTokenSource;

        public InfluxDbWriteEndpointsV18(InfluxDbAuth auth)
        {
            _auth = auth;
            _httpClient = new HttpClient();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _httpClient.Dispose();
        }

        public async Task WriteAsync(IReadOnlyCollection<string> lines)
        {
            var urlBuilder = new HttpUrlBuilder(_auth.HostUrl);
            urlBuilder.SetPath("write");
            urlBuilder.AddArgument("db", _auth.Bucket);
            urlBuilder.AddArgument("precision", "ms");

            if (!string.IsNullOrEmpty(_auth.Username) &&
                !string.IsNullOrEmpty(_auth.Password))
            {
                urlBuilder.AddArgument("u", _auth.Username);
                urlBuilder.AddArgument("p", _auth.Password);
            }

            var url = urlBuilder.ToUri();
            Log.Debug($"Writing: URL: \"{url}\"");

            var req = new HttpRequestMessage(HttpMethod.Post, url);

            var content = string.Join("\n", lines);
            req.Content = new StringContent(content);

            using (var res = await _httpClient.SendAsync(req, _cancellationTokenSource.Token).ConfigureAwait(false))
            {
                if (res.IsSuccessStatusCode)
                {
                    Log.Debug("Finished writing");

                    if (Log.IsTraceEnabled)
                    {
                        Log.Trace("\n" + content);
                    }

                    return;
                }

                var msgBuilder = new StringBuilder();
                msgBuilder.AppendLine($"Failed to write ({res.StatusCode});");
                msgBuilder.AppendLine($"Host URL: {_auth.HostUrl}");
                msgBuilder.AppendLine($"Bucket: {_auth.Bucket}");
                msgBuilder.AppendLine($"Username: {_auth.Username ?? "<empty>"}");
                msgBuilder.AppendLine($"Password: {_auth.Password ?? "<empty>"}");
                msgBuilder.AppendLine($"Content ({lines.Count} lines): ");

                foreach (var line in lines)
                {
                    msgBuilder.AppendLine($"Line: {line}");
                }

                throw new Exception(msgBuilder.ToString());
            }
        }
    }
}