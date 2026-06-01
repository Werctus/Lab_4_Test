using UrlSanitizer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlSanitizer.Implementations.GenCode1
{
    public class UrlSanitizer : IUrlSanitizer
    {
        private static readonly HashSet<string> TrackingParams = new HashSet<string>
        {
            "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content"
        };

        public string Sanitize(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null, empty, or whitespace.");

            url = url.Trim();

            if (!IsValidUrl(url))
                throw new ArgumentException("URL has invalid format.");

            // Приведение схемы к нижнему регистру
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var scheme = uri.Scheme.ToLower();
                var builder = new UriBuilder(uri)
                {
                    Scheme = scheme,
                    Host = uri.Host.ToLower()
                };

                // Удаление фрагмента
                builder.Fragment = string.Empty;

                // Удаление параметров отслеживания
                if (!string.IsNullOrEmpty(builder.Query))
                {
                    var query = builder.Query.TrimStart('?');
                    var pairs = query.Split('&');
                    var filtered = new List<string>();
                    foreach (var pair in pairs)
                    {
                        var key = pair.Split('=')[0].ToLower();
                        if (!TrackingParams.Contains(key))
                            filtered.Add(pair);
                    }
                    builder.Query = string.Join("&", filtered);
                }

                url = builder.ToString();
            }
            else if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("ftp://"))
            {
                url = "https://" + url;
            }

            return url;
        }

        public bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            url = url.Trim();
            return Uri.TryCreate(url, UriKind.Absolute, out _) ||
                   (url.Contains(".") && !url.StartsWith(" ") && !url.EndsWith(" "));
        }

        public string ExtractDomain(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !IsValidUrl(url))
                throw new ArgumentException("URL is invalid.");

            url = url.Trim();
            if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("ftp://"))
                url = "https://" + url;

            var uri = new Uri(url);
            return uri.Host.ToLower();
        }
    }
}
