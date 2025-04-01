using Core.Entities;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MVC.Models;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MVC.Controllers
{
    [Authorize]
    public class EncryptDecryptController : Controller
    {
        private readonly IFileService _fileService;
        private readonly IFrequencyService _frequencyService;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, TempFileInfoModel> _tempFiles = new();
        private readonly string _tempFilePath;

        public EncryptDecryptController(IFileService fileService, IConfiguration configuration, IFrequencyService frequencyService)
        {
            _fileService = fileService;
            _frequencyService = frequencyService;
            _configuration = configuration;
            _tempFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TempFiles");
            Directory.CreateDirectory(_tempFilePath);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadOrCreate(FileUploadModel model)
        {
            if (ModelState.IsValid)
            {
                var dbFile = new DbFile
                {
                    UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                    FileName = model.FileName ?? Path.GetFileNameWithoutExtension(model.File!.FileName),
                    FileExtension = model.File != null ? Path.GetExtension(model.File.FileName) : model.FileExtension ?? ".txt",
                    IsByte = model.IsByte
                };

                if (model.File != null)
                {
                    using var memoryStream = new MemoryStream();
                    await model.File.CopyToAsync(memoryStream);
                    if (model.IsByte)
                    {
                        dbFile.FileContentByte = memoryStream.ToArray();
                        dbFile.FileContentText = "File content is in byte format.";
                    }
                    else
                    {
                        dbFile.FileContentText = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                    }
                }
                else if (!string.IsNullOrEmpty(model.FileContentText))
                {
                    dbFile.FileContentText = model.FileContentText;
                }

                var fileId = await _fileService.UploadOrCreateFileAsync(dbFile);
                var filePath = Url.Action("DownloadFile", "EncryptDecrypt", new { fileId = fileId }, Request.Scheme);
                ViewBag.FileLink = filePath;
                return RedirectToAction("Files");
            }

            return View("Index", model);
        }

        public async Task<IActionResult> Files()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var files = await _fileService.GetUserFilesAsync(userId);
            return View(files);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFile(int fileId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var file = await _fileService.GetFileByIdAsync(fileId, userId);
            if (file == null)
            {
                return NotFound();
            }

            await _fileService.DeleteFileAsync(fileId);
            return RedirectToAction("Files");
        }

        public IActionResult ChooseCipher(int fileId)
        {
            ViewBag.FileId = fileId;
            return View();
        }

        public async Task<IActionResult> DownloadFile(int fileId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var file = await _fileService.GetFileByIdAsync(fileId, userId);
            if (file == null || file.UserId != userId)
            {
                return NotFound();
            }

            byte[] fileBytes;
            string fileName;

            if (file.IsByte && file.FileContentByte != null)
            {
                fileBytes = file.FileContentByte;
                fileName = $"{file.FileName}{file.FileExtension}";
            }
            else if (!string.IsNullOrEmpty(file.FileContentText))
            {
                fileBytes = Encoding.UTF8.GetBytes(file.FileContentText);
                fileName = $"{file.FileName}{file.FileExtension}";
            }
            else
            {
                return NotFound();
            }

            var mimeType = "application/octet-stream";
            if (file.FileExtension == ".pdf")
                mimeType = "application/pdf";
            else if (file.FileExtension == ".jpg" || file.FileExtension == ".jpeg")
                mimeType = "image/jpeg";
            else if (file.FileExtension == ".png")
                mimeType = "image/png";
            else if (file.FileExtension == ".txt")
                mimeType = "text/plain";
            else if (file.FileExtension == ".exe")
                mimeType = "application/octet-stream";

            return File(fileBytes, mimeType, fileName);
        }

        public async Task<IActionResult> ReturnFileContent(int fileId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var file = await _fileService.GetFileByIdAsync(fileId, userId);
            if (file == null || file.UserId != userId)
            {
                return NotFound();
            }

            return Content(file.FileContentText!);
        }

        [HttpPost]
        public IActionResult ChooseCipher(int fileId, string cipher)
        {
            switch (cipher)
            {
                case "Caesar":
                    return RedirectToAction("Index", "CaesarCipher", new { fileId });
                case "Tritemius":
                    return RedirectToAction("Index", "TritemiusCipher", new { fileId });
                case "XOR":
                    return RedirectToAction("Index", "XORCipher", new { fileId });
                case "Knapsack":
                    return RedirectToAction("Index", "KnapsackCipher", new { fileId });
                default:
                    return RedirectToAction("Files");
            }
        }

        [HttpGet]
        public async Task<IActionResult> FrequencyTable(int fileId)
        {
            var file = await _fileService.GetFileByIdAsync(fileId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("User not found"));
            if (file == null)
            {
                return NotFound();
            }

            var content = file.FileContentText ?? throw new Exception("No content in file");

            var frequencyTable = _frequencyService.GetFrequency(content);

            ViewBag.FileId = fileId;
            ViewBag.FrequencyTable = frequencyTable;
            return View();
        }

        [HttpGet]
        public IActionResult FrequencyTableDic(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return BadRequest("Language not specified.");
            }
            else if (language != "English" && language != "Ukrainian")
            {
                return BadRequest("Language not supported.");
            }

            var jsonPath = "/Users/maxhotiuk/Desktop/6sem/MOC/MVC/wwwroot/data/frequency_data.json";

            if (!System.IO.File.Exists(jsonPath))
            {
                return StatusCode(500, "Frequency data file not found.");
            }

            var jsonData = System.IO.File.ReadAllText(jsonPath);
            var frequencyData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(jsonData);

            if (frequencyData == null || !frequencyData.ContainsKey(language))
            {
                return StatusCode(500, "Frequency data not available.");
            }

            ViewBag.FrequencyTable = frequencyData[language];
            return View();
        }
    }
}