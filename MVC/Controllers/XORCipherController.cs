using Microsoft.AspNetCore.Mvc;
using Core.Interfaces;
using System.Security.Claims;

namespace MVC.Controllers
{
    public class XORCipherController : Controller
    {
        private readonly IXORCipherService _xorCipherService;
        private readonly IFileService _fileService;

        public XORCipherController(IXORCipherService xorCipherService, IFileService fileService)
        {
            _xorCipherService = xorCipherService;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int fileId)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            ViewBag.FileId = fileId;
            ViewBag.FileContent = file?.FileContentText ?? "No content";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Encrypt(int fileId, string gamma, string language, string encryptionMethod)
        {
            bool useOneTimePad = encryptionMethod == "oneTimePad";
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var file = await _fileService.GetFileByIdAsync(fileId, userId!);
                
                if (file == null)
                    return NotFound("File not found");

                string encryptionKey = gamma;
                
                // If using one-time pad, generate a random key
                if (useOneTimePad)
                {
                    string oneTimePad = _xorCipherService.GenerateOneTimePad(file.FileContentText!.Length, language);
                    encryptionKey = "OTP:" + oneTimePad;
                    
                    // Store the one-time pad for user to save
                    TempData["OneTimePad"] = oneTimePad;
                }

                string encryptedText = await _xorCipherService.EncryptAsync(file.FileContentText!, encryptionKey, language);
                
                // Update file with encrypted text
                file.FileContentText = encryptedText;
                await _fileService.UpdateFileAsync(file);
                
                TempData["SuccessMessage"] = useOneTimePad ? 
                    "Text encrypted successfully using one-time pad. Make sure to save your key!" : 
                    "Text encrypted successfully";
                
                TempData["Gamma"] = gamma;
                
                return RedirectToAction("Index", new { fileId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Encryption error: {ex.Message}";
                return RedirectToAction("Index", new { fileId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Decrypt(int fileId, string gamma, string language)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var file = await _fileService.GetFileByIdAsync(fileId, userId!);
                
                if (file == null)
                    return NotFound("File not found");
                
                // Automatically detect if this is a one-time pad based on input
                string decryptionKey = gamma.StartsWith("OTP:") ? gamma : 
                                      (gamma.Length > 20) ? "OTP:" + gamma : gamma;
                
                string decryptedText = await _xorCipherService.DecryptAsync(file.FileContentText!, decryptionKey, language);
                
                // Update file with decrypted text
                file.FileContentText = decryptedText;
                await _fileService.UpdateFileAsync(file);
                
                TempData["SuccessMessage"] = "Text decrypted successfully";
                TempData["Gamma"] = gamma;
                
                return RedirectToAction("Index", new { fileId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Decryption error: {ex.Message}";
                return RedirectToAction("Index", new { fileId });
            }
        }
        
        [HttpGet]
        public IActionResult DownloadOneTimePad(string pad)
        {
            if (string.IsNullOrEmpty(pad))
            {
                return BadRequest("No pad provided");
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(pad);
            return File(bytes, "text/plain", "one_time_pad.txt");
        }
    }
}