using System.Net.Http;
using System.Text;
using Utils.General;

namespace InfluxDb.Client
{
    public sealed class InfluxDbAuth
    {
        public string HostUrl { get; set; }
        public string Bucket { get; set; }
        public string Organization { get; set; }
        public string AuthenticationToken { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public void ValidateOrThrow()
        {
            HostUrl.ThrowIfNullOrEmpty(nameof(HostUrl));
            Organization.ThrowIfNullOrEmpty(nameof(Organization));
            Bucket.ThrowIfNullOrEmpty(nameof(Bucket));
        }

        public HttpUrlBuilder MakeHttpUrlBuilder()
        {
            var builder = new HttpUrlBuilder(HostUrl);
            builder.AddArgument("org", Organization);
            builder.AddArgument("bucket", Bucket);
            return builder;
        }

        public void AuthenticateHttpRequest(HttpRequestMessage req)
        {
            // auth is not needed if disabled by the instance
            if (string.IsNullOrEmpty(AuthenticationToken)) return;

            req.Headers.TryAddWithoutValidation("Authorization", $"Token {AuthenticationToken}");
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Host URL: {HostUrl}");
            sb.AppendLine($"Bucket: {Bucket}");
            sb.AppendLine($"Organization: {Organization}");

            if (AuthenticationToken != null)
            {
                sb.AppendLine($"Authentication Token: {AuthenticationToken.HideCredential(4)}");
            }

            return sb.ToString();
        }
    }
}