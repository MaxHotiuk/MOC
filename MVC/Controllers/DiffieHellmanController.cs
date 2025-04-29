using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;
using System.Security.Claims;
using System.Text;

namespace MVC.Controllers
{
    public class DiffieHellmanController : Controller
    {
        private readonly IDiffieHellmanService _diffieHellmanService;
        private readonly IFileService _fileService;

        public DiffieHellmanController(IDiffieHellmanService diffieHellmanService, IFileService fileService)
        {
            _diffieHellmanService = diffieHellmanService;
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
        public async Task<IActionResult> GenerateParameters(int bitLength = 32)
        {
            try
            {
                // Генеруємо велике просте число p
                var p = await _diffieHellmanService.GeneratePrimeAsync(bitLength);
                
                // Знаходимо первісний корінь g за модулем p
                var g = await _diffieHellmanService.FindPrimitiveRootAsync(p);
                
                // Генеруємо приватний ключ
                var privateKey = await _diffieHellmanService.GeneratePrivateKeyAsync();
                
                // Обчислюємо публічний ключ
                var publicKey = await _diffieHellmanService.ComputePublicKeyAsync(g, privateKey, p);
                
                // Зберігаємо параметри в TempData для використання
                TempData["p"] = p.ToString();
                TempData["g"] = g.ToString();
                TempData["privateKey"] = privateKey.ToString();
                TempData["publicKey"] = publicKey.ToString();
                
                TempData["SuccessMessage"] = "Diffie-Hellman parameters generated successfully";
                
                return RedirectToAction("Index", new { fileId = TempData["FileId"] });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating parameters: {ex.Message}";
                return RedirectToAction("Index", new { fileId = TempData["FileId"] });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ComputeSharedSecret(int fileId, string otherPublicKey, string privateKey, string modulus)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var file = await _fileService.GetFileByIdAsync(fileId, userId!);
                
                if (file == null)
                    return NotFound("File not found");
                
                // Конвертуємо параметри зі строки в BigInteger
                BigInteger B = BigInteger.Parse(otherPublicKey);
                BigInteger a = BigInteger.Parse(privateKey);
                BigInteger p = BigInteger.Parse(modulus);
                
                // Обчислюємо спільний секретний ключ
                BigInteger sharedSecret = await _diffieHellmanService.ComputeSharedSecretAsync(B, a, p);
                
                // Отримуємо ключовий матеріал у вигляді хешу
                string keyMaterial = _diffieHellmanService.DeriveKeyMaterial(sharedSecret);
                
                await _fileService.UpdateFileAsync(file);
                
                TempData["SuccessMessage"] = "Shared secret computed successfully";
                TempData["FileId"] = fileId;
                TempData["sharedSecret"] = sharedSecret.ToString();
                TempData["keyMaterial"] = keyMaterial;
                
                return RedirectToAction("Index", new { fileId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error computing shared secret: {ex.Message}";
                return RedirectToAction("Index", new { fileId });
            }
        }

        [HttpPost]
        public IActionResult DownloadParameters()
        {
            try
            {
                string p = TempData["p"]?.ToString() ?? string.Empty;
                string g = TempData["g"]?.ToString() ?? string.Empty;
                string publicKey = TempData["publicKey"]?.ToString() ?? string.Empty;
                string privateKey = TempData["privateKey"]?.ToString() ?? string.Empty;
                
                // Зберігаємо значення назад у TempData, щоб не втратити їх при перенаправленні
                TempData["p"] = p;
                TempData["g"] = g;
                TempData["publicKey"] = publicKey;
                TempData["privateKey"] = privateKey;
                
                string content = 
                    $"Diffie-Hellman Parameters\n\n" +
                    $"Prime Modulus (p):\n{p}\n\n" +
                    $"Generator (g):\n{g}\n\n" +
                    $"Public Key (A):\n{publicKey}\n\n" +
                    $"Private Key (a) - KEEP SECURE:\n{privateKey}";
                
                return File(Encoding.UTF8.GetBytes(content), "text/plain", "diffie_hellman_parameters.txt");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading parameters: {ex.Message}";
                return RedirectToAction("Index", new { fileId = TempData["FileId"] });
            }
        }

        [HttpPost]
        public IActionResult DownloadSharedSecret()
        {
            try
            {
                string sharedSecret = TempData["sharedSecret"]?.ToString() ?? string.Empty;
                string keyMaterial = TempData["keyMaterial"]?.ToString() ?? string.Empty;
                
                // Зберігаємо значення назад у TempData, щоб не втратити їх при перенаправленні
                TempData["sharedSecret"] = sharedSecret;
                TempData["keyMaterial"] = keyMaterial;
                
                string content = 
                    $"Diffie-Hellman Shared Secret\n\n" +
                    $"Raw Shared Secret:\n{sharedSecret}\n\n" +
                    $"Derived Key Material (hex):\n{keyMaterial}";
                
                return File(Encoding.UTF8.GetBytes(content), "text/plain", "diffie_hellman_shared_secret.txt");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading shared secret: {ex.Message}";
                return RedirectToAction("Index", new { fileId = TempData["FileId"] });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateWithExistingParameters(int fileId, string existingP, string existingG)
        {
            try
            {
                // Перевіряємо чи не пусті параметри
                if (string.IsNullOrWhiteSpace(existingP) || string.IsNullOrWhiteSpace(existingG))
                {
                    TempData["ErrorMessage"] = "Both modulus (p) and generator (g) are required";
                    return RedirectToAction("Index", new { fileId });
                }

                // Конвертуємо параметри зі строки в BigInteger
                BigInteger p = BigInteger.Parse(existingP);
                BigInteger g = BigInteger.Parse(existingG);

                // Генеруємо приватний ключ
                var privateKey = await _diffieHellmanService.GeneratePrivateKeyAsync();
                
                // Обчислюємо публічний ключ
                var publicKey = await _diffieHellmanService.ComputePublicKeyAsync(g, privateKey, p);
                
                // Зберігаємо параметри в TempData для використання
                TempData["p"] = p.ToString();
                TempData["g"] = g.ToString();
                TempData["privateKey"] = privateKey.ToString();
                TempData["publicKey"] = publicKey.ToString();
                TempData["FileId"] = fileId;
                
                TempData["SuccessMessage"] = "Keys generated successfully using existing parameters";
                
                return RedirectToAction("Index", new { fileId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating keys: {ex.Message}";
                return RedirectToAction("Index", new { fileId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EncryptText(int fileId, string plainText, string keyMaterial)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(plainText))
                {
                    TempData["ErrorMessage"] = "Text to encrypt cannot be empty";
                    return RedirectToAction("Index", new { fileId });
                }

                if (string.IsNullOrWhiteSpace(keyMaterial))
                {
                    TempData["ErrorMessage"] = "Key material cannot be empty";
                    return RedirectToAction("Index", new { fileId });
                }

                // Шифруємо текст
                string encryptedText = _diffieHellmanService.EncryptText(plainText, keyMaterial);

                // Оновлюємо файл з зашифрованим текстом
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var file = await _fileService.GetFileByIdAsync(fileId, userId!);
                
                if (file == null)
                    return NotFound("File not found");
                
                await _fileService.UpdateFileAsync(file);
                
                // Зберігаємо зашифрований текст у TempData для подальшого використання
                TempData["encryptedText"] = encryptedText;
                TempData["keyMaterial"] = keyMaterial;
                TempData["SuccessMessage"] = "Text encrypted successfully";
                
                return RedirectToAction("Index", new { fileId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Encryption error: {ex.Message}";
                return RedirectToAction("Index", new { fileId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DecryptText(int fileId, string cipherText, string keyMaterial)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cipherText))
                {
                    TempData["ErrorMessage"] = "Cipher text cannot be empty";
                    return RedirectToAction("Index", new { fileId });
                }

                if (string.IsNullOrWhiteSpace(keyMaterial))
                {
                    TempData["ErrorMessage"] = "Key material cannot be empty";
                    return RedirectToAction("Index", new { fileId });
                }

                // Розшифровуємо текст
                string decryptedText = _diffieHellmanService.DecryptText(cipherText, keyMaterial);

                // Оновлюємо файл з розшифрованим текстом
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var file = await _fileService.GetFileByIdAsync(fileId, userId!);
                
                if (file == null)
                    return NotFound("File not found");
                
                await _fileService.UpdateFileAsync(file);
                
                // Зберігаємо розшифрований текст у TempData
                TempData["decryptedText"] = decryptedText;
                TempData["keyMaterial"] = keyMaterial;
                TempData["SuccessMessage"] = "Text decrypted successfully";
                
                return RedirectToAction("Index", new { fileId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Decryption error: {ex.Message}";
                return RedirectToAction("Index", new { fileId });
            }
        }
    }
}