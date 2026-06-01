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
        // Набор параметров отслеживания, которые необходимо удалить
        private static readonly HashSet<string> TrackingParameters = new HashSet<string>
        {
            "utm_source",
            "utm_medium",
            "utm_campaign",
            "utm_term",
            "utm_content"
        };

        /// <summary>
        /// Очищает и нормализует URL.
        /// </summary>
        /// <param name="url">Входная строка URL.</param>
        /// <returns>Очищенный и нормализованный URL.</returns>
        /// <exception cref="ArgumentException">Если URL некорректен или пуст.</exception>
        public string Sanitize(string url)
        {
            // 1. Проверка на null или пустую строку
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("URL не может быть null, пустым или состоять из пробелов.", nameof(url));
            }

            // 2. Удаление лишних пробелов по краям
            url = url.Trim();

            // 3. Удаление фрагмента (всё после #)
            int fragmentIndex = url.IndexOf('#');
            if (fragmentIndex >= 0)
            {
                url = url.Substring(0, fragmentIndex);
            }

            // 4. Попытка разбора URL. Если не удалось, пробуем добавить схему https://
            Uri uriResult;
            bool isValidUri = Uri.TryCreate(url, UriKind.Absolute, out uriResult);

            if (!isValidUri)
            {
                isValidUri = Uri.TryCreate("https://" + url, UriKind.Absolute, out uriResult);
                if (!isValidUri)
                {
                    throw new ArgumentException("URL имеет недопустимый формат.", nameof(url));
                }
            }

            // 5. Проверка допустимой схемы (http, https, ftp)
            if (!uriResult.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                !uriResult.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) &&
                !uriResult.Scheme.Equals("ftp", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Недопустимая схема URL: {uriResult.Scheme}.", nameof(url));
            }

            // 6. Сборка нового URL с изменениями
            var builder = new UriBuilder(uriResult)
            {
                Scheme = uriResult.Scheme.ToLowerInvariant(), // Приведение схемы к нижнему регистру
                Host = uriResult.Host.ToLowerInvariant(),     // Приведение домена к нижнему регистру
                Fragment = string.Empty                       // Фрагмент уже удален, но для надежности
            };

            // 7. Обработка параметров запроса: удаление utm-параметров
            if (!string.IsNullOrEmpty(uriResult.Query))
            {
                var queryParams = ParseQueryString(uriResult.Query);
                var filteredParams = queryParams.Where(kvp => !TrackingParameters.Contains(kvp.Key.ToLowerInvariant()));

                builder.Query = string.Join("&", filteredParams.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

                // Если после фильтрации параметров не осталось, убираем знак вопроса из Query
                if (!filteredParams.Any())
                {
                    builder.Query = string.Empty;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Проверяет, является ли строка корректным URL.
        /// </summary>
        /// <param name="url">Строка для проверки.</param>
        /// <returns>True, если URL корректен.</returns>
        public bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            try
            {
                Sanitize(url); // Используем логику очистки для валидации
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Извлекает домен из URL.
        /// </summary>
        /// <param name="url">Исходный URL.</param>
        /// <returns>Домен (например, sub.example.com).</returns>
        /// <exception cref="ArgumentException">Если URL некорректен.</exception>
        public string ExtractDomain(string url)
        {
            if (!IsValidUrl(url))
            {
                throw new ArgumentException("URL имеет недопустимый формат.", nameof(url));
            }

            var sanitizedUrl = Sanitize(url);

            // Uri.Host возвращает только имя хоста без порта и схемы.
            return new Uri(sanitizedUrl).Host;
        }

        /// <summary>
        /// Вспомогательный метод для парсинга строки запроса в словарь.
        /// </summary>
        private static Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>();

            // Убираем ведущий '?'
            if (query.StartsWith("?"))
                query = query.Substring(1);

            if (string.IsNullOrEmpty(query))
                return result;

            foreach (var param in query.Split('&'))
            {
                if (string.IsNullOrWhiteSpace(param)) continue;

                var parts = param.Split(new[] { '=' }, 2);
                var key = Uri.UnescapeDataString(parts[0]);

                string value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

                result[key] = value;
            }

            return result;
        }
    }
}
