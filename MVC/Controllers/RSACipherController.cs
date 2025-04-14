using Core.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;
using System.Security.Claims;
using System.Text;

namespace MVC.Controllers
{
    public class RSACipherController : Controller
    {
        private readonly IRSAService _rsaService;
        private readonly IFileService _fileService;

        public RSACipherController(IRSAService rsaService, IFileService fileService)
        {
            _rsaService = rsaService;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int fileId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var file = await _fileService.GetFileByIdAsync(fileId, userId!);
                
                if (file == null)
                {
                    TempData["ErrorMessage"] = "File not found or you don't have permission to access it";
                    return RedirectToAction("Files", "EncryptDecrypt");
                }
                
                ViewBag.FileId = fileId;
                ViewBag.FileContent = file.FileContentText ?? "No content";
                
                return View();
            }
            catch
            {
                TempData["ErrorMessage"] = "File not found or you don't have permission to access it";
                return RedirectToAction("Files", "EncryptDecrypt");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateKeys(int bitLength = 1024)
        {
            try
            {
                var keys = await _rsaService.GenerateKeysAsync(bitLength);
                
                // Зберігаємо ключі в TempData для використання
                TempData["p"] = keys.p.ToString();
                TempData["q"] = keys.q.ToString();
                TempData["n"] = keys.n.ToString();
                TempData["e"] = keys.e.ToString();
                TempData["d"] = keys.d.ToString();
                
                TempData["SuccessMessage"] = "RSA keys generated successfully";
                
                return RedirectToAction("Index", new { fileId = TempData["FileId"] });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating keys: {ex.Message}";
                return RedirectToAction("Index", new { fileId = TempData["FileId"] });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Encrypt(int fileId, string publicKeyE, string publicKeyN)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var file = await _fileService.GetFileByIdAsync(fileId, userId!);
                
                if (file == null)
                    return NotFound("File not found");
                
                if (string.IsNullOrEmpty(file.FileContentText))
                {
                    TempData["ErrorMessage"] = "File content is empty or null.";
                    return RedirectToAction("Index", new { fileId });
                }
                
                // Конвертуємо публічний ключ зі строки в BigInteger
                BigInteger e = BigInteger.Parse(publicKeyE);
                BigInteger n = BigInteger.Parse(publicKeyN);
                
                // Шифруємо текст
                string encryptedText = await _rsaService.EncryptAsync(file.FileContentText, e, n);
                
                // Оновлюємо вміст файлу зашифрованим текстом
                file.FileContentText = encryptedText;
                await _fileService.UpdateFileAsync(file);
                
                TempData["SuccessMessage"] = "Text encrypted successfully";
                TempData["FileId"] = fileId;
                
                return RedirectToAction("Index", new { fileId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Encryption error: {ex.Message}";
                return RedirectToAction("Index", new { fileId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Decrypt(int fileId, string privateKeyD, string privateKeyN)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var file = await _fileService.GetFileByIdAsync(fileId, userId!);
                
                if (file == null)
                    return NotFound("File not found");
                
                if (string.IsNullOrEmpty(file.FileContentText))
                {
                    TempData["ErrorMessage"] = "File content is empty or null.";
                    return RedirectToAction("Index", new { fileId });
                }
                
                // Конвертуємо приватний ключ зі строки в BigInteger
                BigInteger d = BigInteger.Parse(privateKeyD);
                BigInteger n = BigInteger.Parse(privateKeyN);
                
                // Розшифровуємо текст
                string decryptedText = await _rsaService.DecryptAsync(file.FileContentText, d, n);
                
                // Оновлюємо вміст файлу розшифрованим текстом
                file.FileContentText = decryptedText;
                await _fileService.UpdateFileAsync(file);
                
                TempData["SuccessMessage"] = "Text decrypted successfully";
                TempData["FileId"] = fileId;
                
                return RedirectToAction("Index", new { fileId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Decryption error: {ex.Message}";
                return RedirectToAction("Index", new { fileId });
            }
        }

        [HttpPost]
        public IActionResult DownloadTxt(string fileId)
        {
            if (!int.TryParse(fileId, out int parsedFileId))
            {
                TempData["ErrorMessage"] = "Invalid file ID.";
                return RedirectToAction("Index");
            }

            var file = _fileService.GetFileByIdAsync(parsedFileId, User.FindFirstValue(ClaimTypes.NameIdentifier)).Result;
            return File(Encoding.UTF8.GetBytes(file.FileContentText), "text/plain", file.FileName);
        }

    }
}