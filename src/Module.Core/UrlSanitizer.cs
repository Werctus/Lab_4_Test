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
        public string Sanitize(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL не может быть пустым или состоять из пробелов", nameof(url));

            url = url.Trim();

            // Добавляем https:// если нет протокола
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            // Разбираем URL
            Uri uri;
            try
            {
                uri = new Uri(url);
            }
            catch
            {
                throw new ArgumentException("Некорректный формат URL", nameof(url));
            }

            // Приводим схему и хост к нижнему регистру
            string scheme = uri.Scheme.ToLower();
            string host = uri.Host.ToLower();

            // Удаляем utm-параметры
            string query = RemoveUtmParameters(uri.Query);

            // Формируем результат без фрагмента
            string result = $"{scheme}://{host}";

            if (uri.Port != 80 && uri.Port != 443)
                result += $":{uri.Port}";

            result += uri.AbsolutePath;

            if (!string.IsNullOrEmpty(query))
                result += query;

            return result;
        }

        public bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            url = url.Trim();

            // Добавляем протокол для проверки, если его нет
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        public string ExtractDomain(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL не может быть пустым", nameof(url));

            url = url.Trim();

            // Добавляем протокол если нет
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                throw new ArgumentException("Некорректный формат URL", nameof(url));

            return uri.Host.ToLower();
        }

        private string RemoveUtmParameters(string query)
        {
            if (string.IsNullOrEmpty(query) || query == "?")
                return "";

            // Убираем вопросительный знак в начале
            query = query.TrimStart('?');

            var parameters = query.Split('&');
            var cleanParameters = new System.Collections.Generic.List<string>();

            foreach (var param in parameters)
            {
                if (string.IsNullOrEmpty(param))
                    continue;

                string lowerParam = param.ToLower();
                if (!lowerParam.StartsWith("utm_source=") &&
                    !lowerParam.StartsWith("utm_medium=") &&
                    !lowerParam.StartsWith("utm_campaign=") &&
                    !lowerParam.StartsWith("utm_term=") &&
                    !lowerParam.StartsWith("utm_content="))
                {
                    cleanParameters.Add(param);
                }
            }

            if (cleanParameters.Count == 0)
                return "";

            return "?" + string.Join("&", cleanParameters);
        }
    }
}
