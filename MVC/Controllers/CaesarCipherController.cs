using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MVC.Controllers
{
    public class CaesarCipherController : Controller
    {
        private readonly ICaesarCipherService _caesarCipherService;
        private readonly IFileService _fileService;

        public CaesarCipherController(ICaesarCipherService caesarCipherService, IFileService fileService)
        {
            _caesarCipherService = caesarCipherService;
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

        [HttpPost]
        public async Task<IActionResult> Encrypt(int fileId, int key, string language)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new System.Exception("User not found"));
            if (file == null)
            {
                return NotFound();
            }

            if (file.IsByte)
            {
                byte[]? encryptedByteText = await _caesarCipherService.EncryptAsync(file.FileContentByte ?? throw new Exception("No content in file"), key);
                file.FileContentByte = encryptedByteText;
                await _fileService.UpdateFileAsync(file);
            }
            else
            {
                string encryptedText = await _caesarCipherService.EncryptAsync(file.FileContentText ?? throw new Exception("No content in file"), key, language);
                file.FileContentText = encryptedText;
                await _fileService.UpdateFileAsync(file);
                ViewBag.FileId = fileId;
                ViewBag.FileContent = encryptedText;
            }
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Decrypt(int fileId, int key, string language)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new System.Exception("User not found"));
            if (file == null)
            {
            return NotFound();
            }

            if (file.IsByte)
            {
                byte[]? decryptedByteText = await _caesarCipherService.DecryptAsync(file.FileContentByte ?? throw new Exception("No content in file"), key);
                file.FileContentByte = decryptedByteText;
                await _fileService.UpdateFileAsync(file);
            }
            else
            {
                string decryptedText = await _caesarCipherService.DecryptAsync(file.FileContentText ?? throw new Exception("No content in file"), key, language);
                file.FileContentText = decryptedText;
                await _fileService.UpdateFileAsync(file);
                ViewBag.FileId = fileId;
                ViewBag.FileContent = decryptedText;
            }
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Attack(int fileId, string language)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new System.Exception("User not found"));
            if (file == null)
            {
                return NotFound();
            }

            var (decryptedText, key) = _caesarCipherService.BruteForceAttack(file.FileContentText ?? throw new Exception("No content in file"), language).FirstOrDefault();
            if (decryptedText != null)
            {
                file.FileContentText = decryptedText;
                await _fileService.UpdateFileAsync(file);

                ViewBag.FileId = fileId;
                ViewBag.FileContent = decryptedText;
                ViewBag.Key = key;
                return View("Index");
            }

            ViewBag.Error = "Unable to decrypt the file using a brute-force attack.";
            return View("Index");
        }
    }
}