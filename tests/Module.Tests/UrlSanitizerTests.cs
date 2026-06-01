using System;
using NUnit.Framework;
using UrlSanitizer.Interfaces;
using UrlSanitizer.Implementations.GenCode1;

using SanitizerClass = UrlSanitizer.Implementations.GenCode1.UrlSanitizer;

namespace UrlSanitizer.Tests
{
    [TestFixture]
    public class UrlSanitizerBlackBoxTests
    {
        private IUrlSanitizer _sanitizer;

        [SetUp]
        public void Setup()
        {
            _sanitizer = new SanitizerClass();
        }

        // ========== ПОЗИТИВНЫЕ ТЕСТЫ ==========

        [Test]
        public void Sanitize_ValidUrlWithHttps_ReturnsNormalizedUrl()
        {
            // Arrange
            string url = "HTTPS://Example.com/Path?query=1#fragment";

            // Act
            string result = _sanitizer.Sanitize(url);

            // Assert
            Assert.That(result, Is.EqualTo("https://example.com/Path?query=1"));
        }

        [Test]
        public void Sanitize_UrlWithoutProtocol_AddsHttps()
        {
            string url = "example.com/page";
            string result = _sanitizer.Sanitize(url);
            Assert.That(result, Is.EqualTo("https://example.com/page"));
        }

        [Test]
        public void Sanitize_UrlWithWhitespace_TrimsSpaces()
        {
            string url = "  https://example.com  ";
            string result = _sanitizer.Sanitize(url);
            Assert.That(result, Is.EqualTo("https://example.com"));
        }

        [Test]
        public void Sanitize_RemovesUtmParameters()
        {
            string url = "https://example.com/page?utm_source=google&utm_medium=cpc&id=123";
            string result = _sanitizer.Sanitize(url);
            Assert.That(result, Does.Not.Contain("utm_source"));
            Assert.That(result, Does.Contain("id=123"));
        }

        [Test]
        public void Sanitize_RemovesFragment()
        {
            string url = "https://example.com/page#section";
            string result = _sanitizer.Sanitize(url);
            Assert.That(result, Does.Not.Contain("#"));
        }

        [Test]
        public void IsValidUrl_ValidHttpsUrl_ReturnsTrue()
        {
            bool result = _sanitizer.IsValidUrl("https://example.com");
            Assert.That(result, Is.True);
        }

        [Test]
        public void ExtractDomain_SimpleUrl_ReturnsDomain()
        {
            string result = _sanitizer.ExtractDomain("https://example.com/page");
            Assert.That(result, Is.EqualTo("example.com"));
        }

        [Test]
        public void ExtractDomain_UrlWithSubdomain_ReturnsFullDomain()
        {
            string result = _sanitizer.ExtractDomain("https://sub.example.com/page");
            Assert.That(result, Is.EqualTo("sub.example.com"));
        }

        // ========== НЕГАТИВНЫЕ ТЕСТЫ ==========

        [Test]
        public void Sanitize_NullUrl_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => _sanitizer.Sanitize(null));
            Assert.That(ex.Message, Does.Contain("URL"));
        }

        [Test]
        public void Sanitize_EmptyString_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => _sanitizer.Sanitize(""));
            Assert.That(ex.Message, Does.Contain("URL"));
        }

        [Test]
        public void Sanitize_WhitespaceOnly_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => _sanitizer.Sanitize("   "));
            Assert.That(ex.Message, Does.Contain("URL"));
        }

        [Test]
        public void Sanitize_InvalidUrlFormat_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => _sanitizer.Sanitize("not a url"));
            Assert.That(ex.Message, Does.Contain("формат") | Does.Contain("корректным"));
        }

        [Test]
        public void IsValidUrl_NullInput_ReturnsFalse()
        {
            bool result = _sanitizer.IsValidUrl(null);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ExtractDomain_InvalidUrl_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => _sanitizer.ExtractDomain("invalid"));
            Assert.That(ex.Message, Does.Contain("URL"));
        }

        // ========== ГРАНИЧНЫЕ СЛУЧАИ ==========

        [Test]
        public void Sanitize_MultipleUtmParameters_RemovesAll()
        {
            string url = "https://example.com?utm_source=a&utm_medium=b&utm_campaign=c&utm_term=d&utm_content=e&keep=value";
            string result = _sanitizer.Sanitize(url);
            Assert.That(result, Does.Not.Contain("utm_"));
            Assert.That(result, Does.Contain("keep=value"));
        }

        [Test]
        public void Sanitize_UrlWithPort_KeepsPort()
        {
            string url = "https://example.com:8080/page";
            string result = _sanitizer.Sanitize(url);
            Assert.That(result, Is.EqualTo("https://example.com:8080/page"));
        }
    }
    [TestFixture]
    public class UrlSanitizerWhiteBoxTests
    {
        private IUrlSanitizer _sanitizer;

        [SetUp]
        public void Setup()
        {
            _sanitizer = new SanitizerClass();
        }

        // ========== ТЕСТЫ БЕЛОГО ЯЩИКА ==========

        [Test]
        public void Sanitize_EmptyQueryAfterUtmRemoval_RemovesQuestionMark()
        {
            // Проверка внутренней логики: если после удаления utm-параметров
            // строка запроса становится пустой, вопросительный знак должен удаляться
            string url = "https://example.com?utm_source=test";
            string result = _sanitizer.Sanitize(url);

            // Ожидаем, что реализация удалит пустой query string
            Assert.That(result, Does.Not.Contain("?"));
            Assert.That(result, Is.EqualTo("https://example.com"));
        }

        [Test]
        public void Sanitize_PathCase_PreservesOriginalCase()
        {
            // Проверка, что реализация сохраняет регистр в пути
            // (это внутреннее решение, не оговоренное в спецификации)
            string url = "https://example.com/MyPage/UserProfile";
            string result = _sanitizer.Sanitize(url);

            Assert.That(result, Does.Contain("MyPage/UserProfile"));
            Assert.That(result, Is.EqualTo("https://example.com/MyPage/UserProfile"));
        }

        [Test]
        public void ExtractDomain_UrlWithPort_ExcludesPort()
        {
            // Проверка, что порт не включается в домен
            // (спецификация явно не оговаривает обработку порта)
            string result = _sanitizer.ExtractDomain("https://example.com:8080/page");

            Assert.That(result, Is.EqualTo("example.com"));
            Assert.That(result, Does.Not.Contain("8080"));
        }

        [Test]
        public void Sanitize_UrlWithMultipleSlashes_NormalizesPath()
        {
            // Проверка внутренней нормализации пути
            // (может быть реализована или нет — это деталь реализации)
            string url = "https://example.com//path//to//page";
            string result = _sanitizer.Sanitize(url);

            // Если реализация нормализует слеши, тест пройден
            // Если нет — это не ошибка, просто особенность реализации
            Assert.That(result, Does.Not.Contain("//"));
        }

        [Test]
        public void ExtractDomain_UrlWithSubdomain_ReturnsExactSubdomain()
        {
            // Проверка, что реализация правильно определяет иерархию доменов
            string result = _sanitizer.ExtractDomain("https://blog.sub.example.com/page");

            Assert.That(result, Is.EqualTo("blog.sub.example.com"));

            // Дополнительная проверка: результат не должен содержать путь
            Assert.That(result, Does.Not.Contain("/"));
            Assert.That(result, Does.Not.Contain("page"));

            // Проверяем, что все части домена сохранились
            Assert.That(result, Does.Contain("blog"));
            Assert.That(result, Does.Contain("sub"));
            Assert.That(result, Does.Contain("example"));
            Assert.That(result, Does.Contain("com"));
        }

        [Test]
        public void Sanitize_KeepsOnlyValidQueryParameters()
        {
            // Проверка внутренней логики фильтрации параметров
            string url = "https://example.com?valid=1&utm_source=test&another=2&utm_medium=test";
            string result = _sanitizer.Sanitize(url);

            // Проверяем, что сохранились только НЕ utm-параметры
            Assert.That(result, Does.Contain("valid=1"));
            Assert.That(result, Does.Contain("another=2"));

            // Проверяем порядок параметров (если это важно для реализации)
            int validIndex = result.IndexOf("valid=1");
            int anotherIndex = result.IndexOf("another=2");
            Assert.That(validIndex, Is.LessThan(anotherIndex));
        }

        [Test]
        public void IsValidUrl_EdgeCases_InternalValidationLogic()
        {
            // Проверка граничных случаев, которые может обрабатывать реализация
            Assert.That(_sanitizer.IsValidUrl("http://localhost"), Is.True);
            Assert.That(_sanitizer.IsValidUrl("http://127.0.0.1"), Is.True);
            Assert.That(_sanitizer.IsValidUrl("ftp://files.example.com"), Is.True);
        }

        [Test]
        public void Sanitize_UrlWithEncodedCharacters_DoesNotDoubleEncode()
        {
            // Проверка, что реализация не кодирует уже закодированные символы
            string url = "https://example.com/search?q=%D0%BF%D1%80%D0%B8%D0%B2%D0%B5%D1%82";
            string result = _sanitizer.Sanitize(url);

            // Ожидаем, что реализация сохранит существующее кодирование
            Assert.That(result, Does.Contain("%D0%BF%D1%80%D0%B8%D0%B2%D0%B5%D1%82"));
        }
    }
}
