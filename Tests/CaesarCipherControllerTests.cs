using System.Security.Claims;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MVC.Controllers;

namespace Tests;

public class CaesarCipherControllerTests
{
    private readonly Mock<ICaesarCipherService> _mockCaesarCipherService;
    private readonly Mock<IFileService> _mockFileService;
    private readonly CaesarCipherController _controller;

    public CaesarCipherControllerTests()
    {
        _mockCaesarCipherService = new Mock<ICaesarCipherService>();
        _mockFileService = new Mock<IFileService>();
        _controller = new CaesarCipherController(_mockCaesarCipherService.Object, _mockFileService.Object);

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

        _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id")!)
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
    public async Task Encrypt_UpdatesFileContent_AndReturnsView()
    {
        var fileId = 1;
        var key = 3;
        var originalContent = "Hello";
        var encryptedContent = "Khoor";
        var file = new DbFile { Id = fileId, FileContentText = originalContent, IsByte = false };

        _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                       .ReturnsAsync(file);
        _mockCaesarCipherService.Setup(service => service.EncryptAsync(originalContent, key))
                               .ReturnsAsync(encryptedContent);

        var result = await _controller.Encrypt(fileId, key);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.Equal(encryptedContent, viewResult.ViewData["FileContent"]);
        Assert.Equal(fileId, viewResult.ViewData["FileId"]);

        _mockFileService.Verify(service => service.UpdateFileAsync(file), Times.Once);
    }

    [Fact]
    public async Task Decrypt_UpdatesFileContent_AndReturnsView()
    {
        var fileId = 1;
        var key = 3;
        var encryptedContent = "Khoor";
        var decryptedContent = "Hello";
        var file = new DbFile { Id = fileId, FileContentText = encryptedContent, IsByte = false };

        _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                       .ReturnsAsync(file);
        _mockCaesarCipherService.Setup(service => service.DecryptAsync(encryptedContent, key))
                               .ReturnsAsync(decryptedContent);

        var result = await _controller.Decrypt(fileId, key);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.Equal(decryptedContent, viewResult.ViewData["FileContent"]);
        Assert.Equal(fileId, viewResult.ViewData["FileId"]);

        _mockFileService.Verify(service => service.UpdateFileAsync(file), Times.Once);
    }

    [Fact]
    public async Task Attack_ReturnsViewWithDecryptedContent_WhenSuccessful()
    {
        var fileId = 1;
        var encryptedContent = "Khoor";
        var decryptedContent = "Hello";
        var key = 3;
        var file = new DbFile { Id = fileId, FileContentText = encryptedContent, IsByte = false };

        _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                       .ReturnsAsync(file);
        _mockCaesarCipherService.Setup(service => service.BruteForceAttack(encryptedContent))
                               .Returns(new Dictionary<string, int> { { decryptedContent, key } });

        var result = await _controller.Attack(fileId);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.Equal(decryptedContent, viewResult.ViewData["FileContent"]);
        Assert.Equal(key, viewResult.ViewData["Key"]);

        _mockFileService.Verify(service => service.UpdateFileAsync(file), Times.Once);
    }

    [Fact]
    public async Task Attack_ReturnsViewWithError_WhenDecryptionFails()
    {
        var fileId = 1;
        var encryptedContent = "Khoor";
        var file = new DbFile { Id = fileId, FileContentText = encryptedContent, IsByte = false };

        _mockFileService.Setup(service => service.GetFileByIdAsync(fileId, "test-user-id"))
                       .ReturnsAsync(file);
        _mockCaesarCipherService.Setup(service => service.BruteForceAttack(encryptedContent))
                               .Returns(new Dictionary<string, int>());

        var result = await _controller.Attack(fileId);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.Equal("Unable to decrypt the file using a brute-force attack.", viewResult.ViewData["Error"]);
    }
}