using Core.Interfaces;
using Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class XORCipherServiceTests
    {
        private readonly XORCipherService _xorCipherService;

        public XORCipherServiceTests()
        {
            _xorCipherService = new XORCipherService();
        }

        [Fact]
        public void EncryptAsync_WithOneTimePad_ReturnsCorrectEncryptedText()
        {
            // Arrange
            var text = "hello";
            var gamma = "OTP:abcde";
            var language = "English";

            // Act
            var result = _xorCipherService.EncryptAsync(text, gamma, language).Result;

            // Assert
            Assert.Equal("hfjik", result);
        }

        [Fact]
        public void EncryptAsync_WithGamma_ReturnsCorrectEncryptedText()
        {
            // Arrange
            var text = "hello";
            var gamma = "abc";
            var language = "English";

            // Act
            var result = _xorCipherService.EncryptAsync(text, gamma, language).Result;

            // Assert
            Assert.Equal("hfjlp", result);
        }

        [Fact]
        public async Task EncryptAsync_ThrowsException_WhenLanguageNotSupported()
        {
            // Arrange
            var text = "hello";
            var gamma = "abc";
            var language = "UnsupportedLanguage";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _xorCipherService.EncryptAsync(text, gamma, language));
        }

        [Fact]
        public void DecryptAsync_WithOneTimePad_ReturnsCorrectDecryptedText()
        {
            // Arrange
            var text = "hfjik";
            var gamma = "OTP:abcde";
            var language = "English";

            // Act
            var result = _xorCipherService.DecryptAsync(text, gamma, language).Result;

            // Assert
            Assert.Equal("hello", result);
        }

        [Fact]
        public void DecryptAsync_WithGamma_ReturnsCorrectDecryptedText()
        {
            // Arrange
            var text = "hfjlp";
            var gamma = "abc";
            var language = "English";

            // Act
            var result = _xorCipherService.DecryptAsync(text, gamma, language).Result;

            // Assert
            Assert.Equal("hello", result);
        }

        [Fact]
        public async Task DecryptAsync_ThrowsException_WhenLanguageNotSupported()
        {
            // Arrange
            var text = "hfjik";
            var gamma = "abc";
            var language = "UnsupportedLanguage";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _xorCipherService.DecryptAsync(text, gamma, language));
        }

        [Fact]
        public void GenerateOneTimePad_ReturnsCorrectLength()
        {
            // Arrange
            var length = 10;
            var language = "English";

            // Act
            var result = _xorCipherService.GenerateOneTimePad(length, language);

            // Assert
            Assert.Equal(length, result.Length);
        }

        [Fact]
        public void GenerateOneTimePad_ThrowsException_WhenLanguageNotSupported()
        {
            // Arrange
            var length = 10;
            var language = "UnsupportedLanguage";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _xorCipherService.GenerateOneTimePad(length, language));
        }

        [Fact]
        public void GetBinaryIndices_ReturnsCorrectBinaryRepresentation()
        {
            // Arrange
            var language = "English";

            // Act
            var result = _xorCipherService.GetBinaryIndices(language);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }

        [Fact]
        public void GetBinaryIndices_ThrowsException_WhenLanguageNotSupported()
        {
            // Arrange
            var language = "UnsupportedLanguage";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _xorCipherService.GetBinaryIndices(language));
        }

        [Fact]
        public void GetBinaryTextRepresentation_ReturnsCorrectBinaryRepresentation()
        {
            // Arrange
            var text = "hello";
            var language = "English";

            // Act
            var result = _xorCipherService.GetBinaryTextRepresentation(text, language);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("000111", result); // Adjusted binary representation
        }

        [Fact]
        public void GetBinaryTextRepresentation_ThrowsException_WhenLanguageNotSupported()
        {
            // Arrange
            var text = "hello";
            var language = "UnsupportedLanguage";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _xorCipherService.GetBinaryTextRepresentation(text, language));
        }

        [Fact]
        public void GetEncryptionProcess_ReturnsCorrectProcessDescription()
        {
            // Arrange
            var text = "hello";
            var gamma = "abc";
            var language = "English";

            // Act
            var result = _xorCipherService.GetEncryptionProcess(text, gamma, language);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Text char: 'h'", result);
        }

        [Fact]
        public void GetEncryptionProcess_ThrowsException_WhenLanguageNotSupported()
        {
            // Arrange
            var text = "hello";
            var gamma = "abc";
            var language = "UnsupportedLanguage";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _xorCipherService.GetEncryptionProcess(text, gamma, language));
        }
    }
}