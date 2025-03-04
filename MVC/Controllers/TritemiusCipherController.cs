using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MVC.Controllers
{
    public class TritemiusCipherController : Controller
    {
        private readonly ITritemiusCipherService _tritemiusCipherService;
        private readonly IFileService _fileService;

        public TritemiusCipherController(ITritemiusCipherService tritemiusCipherService, IFileService fileService)
        {
            _tritemiusCipherService = tritemiusCipherService;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int fileId)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new System.Exception("User not found"));
            if (file == null)
            {
                return NotFound();
            }

            ViewBag.FileId = fileId;
            ViewBag.FileContent = file.FileContentText;
            return View();
        }

        // Linear Encryption
        [HttpPost]
        public async Task<IActionResult> EncryptLinear(int fileId, string language, int a, int b)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new System.Exception("User not found"));
            if (file == null)
            {
                return NotFound();
            }

            if (file.IsByte)
            {
                return BadRequest("Byte encryption is not supported for Tritemius cipher.");
            }
            else
            {
                string encryptedText = await _tritemiusCipherService.EncryptAsync(
                    file.FileContentText ?? throw new Exception("No content in file"), 
                    a, b, language);

                file.FileContentText = encryptedText;
                await _fileService.UpdateFileAsync(file);
                ViewBag.FileId = fileId;
                ViewBag.FileContent = encryptedText;
                ViewBag.EncryptionMethod = "Linear";
                ViewBag.A = a;
                ViewBag.B = b;
            }
            return View("Index");
        }

        // Quadratic Encryption
        [HttpPost]
        public async Task<IActionResult> EncryptQuadratic(int fileId, string language, int a, int b, int c)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new System.Exception("User not found"));
            if (file == null)
            {
                return NotFound();
            }

            if (file.IsByte)
            {
                return BadRequest("Byte encryption is not supported for Tritemius cipher.");
            }
            else
            {
                string encryptedText = await _tritemiusCipherService.EncryptAsync(
                    file.FileContentText ?? throw new Exception("No content in file"), 
                    a, b, c, language);

                file.FileContentText = encryptedText;
                await _fileService.UpdateFileAsync(file);
                ViewBag.FileId = fileId;
                ViewBag.FileContent = encryptedText;
                ViewBag.EncryptionMethod = "Quadratic";
                ViewBag.A = a;
                ViewBag.B = b;
                ViewBag.C = c;
            }
            return View("Index");
        }

        // Keyword Encryption
        [HttpPost]
        public async Task<IActionResult> EncryptKeyword(int fileId, string language, string keyword)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new System.Exception("User not found"));
            if (file == null)
            {
                return NotFound();
            }

            if (file.IsByte)
            {
                return BadRequest("Byte encryption is not supported for Tritemius cipher.");
            }
            else
            {
                string encryptedText = await _tritemiusCipherService.EncryptAsync(
                    file.FileContentText ?? throw new Exception("No content in file"), 
                    keyword, language);

                file.FileContentText = encryptedText;
                await _fileService.UpdateFileAsync(file);
                ViewBag.FileId = fileId;
                ViewBag.FileContent = encryptedText;
                ViewBag.EncryptionMethod = "Keyword";
                ViewBag.Keyword = keyword;
            }
            return View("Index");
        }

        // Linear Decryption
        [HttpPost]
        public async Task<IActionResult> DecryptLinear(int fileId, int a, int b, string language)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new System.Exception("User not found"));
            if (file == null)
            {
                return NotFound();
            }

            if (file.IsByte)
            {
                return BadRequest("Byte decryption is not supported for Tritemius cipher.");
            }
            else
            {
                string decryptedText = await _tritemiusCipherService.DecryptAsync(
                    file.FileContentText ?? throw new Exception("No content in file"), 
                    a, b, language);

                file.FileContentText = decryptedText;
                await _fileService.UpdateFileAsync(file);
                ViewBag.FileId = fileId;
                ViewBag.FileContent = decryptedText;
                ViewBag.DecryptionMethod = "Linear";
                ViewBag.A = a;
                ViewBag.B = b;
            }
            return View("Index");
        }

        // Quadratic Decryption
        [HttpPost]
        public async Task<IActionResult> DecryptQuadratic(int fileId, int a, int b, int c, string language)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new System.Exception("User not found"));
            if (file == null)
            {
                return NotFound();
            }

            if (file.IsByte)
            {
                return BadRequest("Byte decryption is not supported for Tritemius cipher.");
            }
            else
            {
                string decryptedText = await _tritemiusCipherService.DecryptAsync(
                    file.FileContentText ?? throw new Exception("No content in file"), 
                    a, b, c, language);

                file.FileContentText = decryptedText;
                await _fileService.UpdateFileAsync(file);
                ViewBag.FileId = fileId;
                ViewBag.FileContent = decryptedText;
                ViewBag.DecryptionMethod = "Quadratic";
                ViewBag.A = a;
                ViewBag.B = b;
                ViewBag.C = c;
            }
            return View("Index");
        }

        // Keyword Decryption
        [HttpPost]
        public async Task<IActionResult> DecryptKeyword(int fileId, string keyword, string language)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new System.Exception("User not found"));
            if (file == null)
            {
                return NotFound();
            }

            if (file.IsByte)
            {
                return BadRequest("Byte decryption is not supported for Tritemius cipher.");
            }
            else
            {
                string decryptedText = await _tritemiusCipherService.DecryptAsync(
                    file.FileContentText ?? throw new Exception("No content in file"), 
                    keyword, language);

                file.FileContentText = decryptedText;
                await _fileService.UpdateFileAsync(file);
                ViewBag.FileId = fileId;
                ViewBag.FileContent = decryptedText;
                ViewBag.DecryptionMethod = "Keyword";
                ViewBag.Keyword = keyword;
            }
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> FrequencyAttack(string language, int fileId)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new System.Exception("User not found"));
            if (file == null)
            {
                return NotFound();
            }

            var results = _tritemiusCipherService.FrequencyAttack(file.FileContentText!, language);

            ViewBag.FileId = fileId;
            ViewBag.FileContent = file.FileContentText ?? "No content in file";
            ViewBag.FrequencyAttackResults = results;

            return View("Index");
        }

        // Brute Force Attack
        [HttpPost]
        public async Task<IActionResult> Attack(string plainText, string encryptedText, string language, int fileId)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new System.Exception("User not found"));
            if (file == null)
            {
                return NotFound();
            }
            // Try numeric key attack first
            var numericKeyTuple = _tritemiusCipherService.FindKey(plainText, encryptedText, language);
            if (numericKeyTuple.HasValue)
            {
                var (A, B, C) = numericKeyTuple.Value;
                if (A == null)
                {
                    ViewBag.Key = $"Numeric Key - A: {B}, B: {C}";
                }
                else
                {
                    ViewBag.Key = $"Numeric Key - A: {A}, B: {B}, C: {C}";
                }
                ViewBag.FileId = fileId;
                ViewBag.FileContent = file.FileContentText ?? "No content in file";
                return View("Index");
            }
            ViewBag.FileId = fileId;
            ViewBag.FileContent = file.FileContentText ?? "No content in file";
            ViewBag.Error = "Unable to find a key.";
            return View("Index");
        }
    }
}