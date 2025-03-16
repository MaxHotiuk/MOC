using System.Security.Claims;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MVC.Controllers;
using Xunit;

namespace Tests
{
    public class TritemiusCipherControllerTests
    {
        private readonly Mock<ITritemiusCipherService> _mockTritemiusCipherService;
        private readonly Mock<IFileService> _mockFileService;
        private readonly TritemiusCipherController _controller;

        public TritemiusCipherControllerTests()
        {
            _mockTritemiusCipherService = new Mock<ITritemiusCipherService>();
            _mockFileService = new Mock<IFileService>();
            _controller = new TritemiusCipherController(_mockTritemiusCipherService.Object, _mockFileService.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "test-user-id")
            ]));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task Index_ReturnsViewWithFileContent()
        {
            var fileId = 1;
            var fileContent = "Sample file content";
            var file = new DbFile { Id = fileId, FileContentText = fileContent, IsByte = false };

            _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                           .ReturnsAsync(file);

            var result = await _controller.Index(fileId);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(fileContent, viewResult.ViewData["FileContent"]);
            Assert.Equal(fileId, viewResult.ViewData["FileId"]);
        }

        [Fact]
        public async Task Index_ReturnsNotFound_WhenFileDoesNotExist()
        {
            var fileId = 1;
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                           .ReturnsAsync(default(DbFile));
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

            var result = await _controller.Index(fileId);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EncryptLinear_UpdatesFileContent_AndReturnsView()
        {
            var fileId = 1;
            var a = 1;
            var b = 2;
            var language = "English";
            var originalContent = "text";
            var encryptedContent = "wkGF";
            var file = new DbFile { Id = fileId, FileContentText = originalContent, IsByte = false };

            _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                           .ReturnsAsync(file);
            _mockTritemiusCipherService.Setup(service => service.EncryptAsync(originalContent, a, b, language))
                                      .ReturnsAsync(encryptedContent);

            var result = await _controller.EncryptLinear(fileId, language, a, b);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal(encryptedContent, viewResult.ViewData["FileContent"]);
            Assert.Equal(fileId, viewResult.ViewData["FileId"]);
            Assert.Equal("Linear", viewResult.ViewData["EncryptionMethod"]);
            Assert.Equal(a, viewResult.ViewData["A"]);
            Assert.Equal(b, viewResult.ViewData["B"]);

            _mockFileService.Verify(service => service.UpdateFileAsync(file), Times.Once);
        }

        [Fact]
        public async Task EncryptQuadratic_UpdatesFileContent_AndReturnsView()
        {
            var fileId = 1;
            var a = 1;
            var b = 2;
            var c = 3;
            var language = "English";
            var originalContent = "text";
            var encryptedContent = "wkGF";
            var file = new DbFile { Id = fileId, FileContentText = originalContent, IsByte = false };

            _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                           .ReturnsAsync(file);
            _mockTritemiusCipherService.Setup(service => service.EncryptAsync(originalContent, a, b, c, language))
                                      .ReturnsAsync(encryptedContent);

            var result = await _controller.EncryptQuadratic(fileId, language, a, b, c);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal(encryptedContent, viewResult.ViewData["FileContent"]);
            Assert.Equal(fileId, viewResult.ViewData["FileId"]);
            Assert.Equal("Quadratic", viewResult.ViewData["EncryptionMethod"]);
            Assert.Equal(a, viewResult.ViewData["A"]);
            Assert.Equal(b, viewResult.ViewData["B"]);
            Assert.Equal(c, viewResult.ViewData["C"]);

            _mockFileService.Verify(service => service.UpdateFileAsync(file), Times.Once);
        }

        [Fact]
        public async Task EncryptKeyword_UpdatesFileContent_AndReturnsView()
        {
            var fileId = 1;
            var keyword = "key";
            var language = "English";
            var originalContent = "text";
            var encryptedContent = "wkGF";
            var file = new DbFile { Id = fileId, FileContentText = originalContent, IsByte = false };

            _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                           .ReturnsAsync(file);
            _mockTritemiusCipherService.Setup(service => service.EncryptAsync(originalContent, keyword, language))
                                      .ReturnsAsync(encryptedContent);

            var result = await _controller.EncryptKeyword(fileId, language, keyword);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal(encryptedContent, viewResult.ViewData["FileContent"]);
            Assert.Equal(fileId, viewResult.ViewData["FileId"]);
            Assert.Equal("Keyword", viewResult.ViewData["EncryptionMethod"]);
            Assert.Equal(keyword, viewResult.ViewData["Keyword"]);

            _mockFileService.Verify(service => service.UpdateFileAsync(file), Times.Once);
        }

        [Fact]
        public async Task DecryptLinear_UpdatesFileContent_AndReturnsView()
        {
            var fileId = 1;
            var a = 1;
            var b = 2;
            var language = "English";
            var encryptedContent = "wkGF";
            var decryptedContent = "text";
            var file = new DbFile { Id = fileId, FileContentText = encryptedContent, IsByte = false };

            _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                           .ReturnsAsync(file);
            _mockTritemiusCipherService.Setup(service => service.DecryptAsync(encryptedContent, a, b, language))
                                      .ReturnsAsync(decryptedContent);

            var result = await _controller.DecryptLinear(fileId, a, b, language);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal(decryptedContent, viewResult.ViewData["FileContent"]);
            Assert.Equal(fileId, viewResult.ViewData["FileId"]);
            Assert.Equal("Linear", viewResult.ViewData["DecryptionMethod"]);
            Assert.Equal(a, viewResult.ViewData["A"]);
            Assert.Equal(b, viewResult.ViewData["B"]);

            _mockFileService.Verify(service => service.UpdateFileAsync(file), Times.Once);
        }

        [Fact]
        public async Task DecryptQuadratic_UpdatesFileContent_AndReturnsView()
        {
            var fileId = 1;
            var a = 1;
            var b = 2;
            var c = 3;
            var language = "English";
            var encryptedContent = "wkGF";
            var decryptedContent = "text";
            var file = new DbFile { Id = fileId, FileContentText = encryptedContent, IsByte = false };

            _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                           .ReturnsAsync(file);
            _mockTritemiusCipherService.Setup(service => service.DecryptAsync(encryptedContent, a, b, c, language))
                                      .ReturnsAsync(decryptedContent);

            var result = await _controller.DecryptQuadratic(fileId, a, b, c, language);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal(decryptedContent, viewResult.ViewData["FileContent"]);
            Assert.Equal(fileId, viewResult.ViewData["FileId"]);
            Assert.Equal("Quadratic", viewResult.ViewData["DecryptionMethod"]);
            Assert.Equal(a, viewResult.ViewData["A"]);
            Assert.Equal(b, viewResult.ViewData["B"]);
            Assert.Equal(c, viewResult.ViewData["C"]);

            _mockFileService.Verify(service => service.UpdateFileAsync(file), Times.Once);
        }

        [Fact]
        public async Task DecryptKeyword_UpdatesFileContent_AndReturnsView()
        {
            var fileId = 1;
            var keyword = "key";
            var language = "English";
            var encryptedContent = "wkGF";
            var decryptedContent = "text";
            var file = new DbFile { Id = fileId, FileContentText = encryptedContent, IsByte = false };

            _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                           .ReturnsAsync(file);
            _mockTritemiusCipherService.Setup(service => service.DecryptAsync(encryptedContent, keyword, language))
                                      .ReturnsAsync(decryptedContent);

            var result = await _controller.DecryptKeyword(fileId, keyword, language);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal(decryptedContent, viewResult.ViewData["FileContent"]);
            Assert.Equal(fileId, viewResult.ViewData["FileId"]);
            Assert.Equal("Keyword", viewResult.ViewData["DecryptionMethod"]);
            Assert.Equal(keyword, viewResult.ViewData["Keyword"]);

            _mockFileService.Verify(service => service.UpdateFileAsync(file), Times.Once);
        }

        [Fact]
        public async Task Attack_ReturnsViewWithKey_WhenSuccessful()
        {
            var fileId = 1;
            var plainText = "text";
            var encryptedText = "wkGF";
            var language = "English";
            var file = new DbFile { Id = fileId, FileContentText = encryptedText, IsByte = false };

            _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                        .ReturnsAsync(file);
            _mockTritemiusCipherService.Setup(service => service.FindKey(plainText, encryptedText, language))
                                    .Returns(((int?)null, 3, 3));

            var result = await _controller.Attack(plainText, encryptedText, language, fileId);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal("Numeric Key - A: 3, B: 3", viewResult.ViewData["Key"]);
            Assert.Equal(fileId, viewResult.ViewData["FileId"]);
            Assert.Equal(encryptedText, viewResult.ViewData["FileContent"]);
        }

        [Fact]
        public async Task Attack_ReturnsViewWithError_WhenKeyNotFound()
        {
            var fileId = 1;
            var plainText = "text";
            var encryptedText = "wkGF";
            var language = "English";
            var file = new DbFile { Id = fileId, FileContentText = encryptedText, IsByte = false };

            _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                           .ReturnsAsync(file);
            _mockTritemiusCipherService.Setup(service => service.FindKey(plainText, encryptedText, language))
                                      .Returns((ValueTuple<int?, int, int>?)null);

            var result = await _controller.Attack(plainText, encryptedText, language, fileId);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal("Unable to find a key.", viewResult.ViewData["Error"]);
            Assert.Equal(fileId, viewResult.ViewData["FileId"]);
            Assert.Equal(encryptedText, viewResult.ViewData["FileContent"]);
        }
    }
}