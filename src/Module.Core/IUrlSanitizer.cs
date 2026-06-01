using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UrlSanitizer.Interfaces
{
    public interface IUrlSanitizer
    {
        /// <summary>
        /// Очищает и нормализует URL
        /// </summary>
        /// <param name="url">Входная строка URL</param>
        /// <returns>Очищенный и нормализованный URL</returns>
        /// <exception cref="ArgumentException">Если URL некорректен или пуст</exception>
        string Sanitize(string url);

        /// <summary>
        /// Проверяет, является ли строка корректным URL
        /// </summary>
        bool IsValidUrl(string url);

        /// <summary>
        /// Извлекает домен из URL
        /// </summary>
        string ExtractDomain(string url);
    }
}
