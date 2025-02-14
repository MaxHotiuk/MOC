using Core.Entities;
using Core.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public EncryptDecryptController(IFileService fileService)
        {
            _fileService = fileService;
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
                    FileExtension = model.FileExtension ?? Path.GetExtension(model.File!.FileName),
                    IsByte = model.IsByte
                };

                if (model.File != null)
                {
                    using var memoryStream = new MemoryStream();
                    await model.File.CopyToAsync(memoryStream);
                    if (model.IsByte)
                    {
                        dbFile.FileContentByte = memoryStream.ToArray();
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
            if (file == null || file.UserId != userId) // Ensure the file belongs to the current user
            {
                return NotFound();
            }

            byte[] fileBytes;
            string contentType;
            string fileName;

            if (file.IsByte && file.FileContentByte != null)
            {
                fileBytes = file.FileContentByte;
                contentType = "application/octet-stream";
                fileName = $"{file.FileName}{file.FileExtension}";
            }
            else if (!string.IsNullOrEmpty(file.FileContentText))
            {
                fileBytes = Encoding.UTF8.GetBytes(file.FileContentText);
                contentType = "text/plain";
                fileName = $"{file.FileName}{file.FileExtension}";
            }
            else
            {
                return NotFound();
            }

            return File(fileBytes, contentType, fileName);
        }

        public async Task<IActionResult> PrintFile(int fileId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var file = await _fileService.GetFileByIdAsync(fileId, userId);
            if (file == null || file.UserId != userId) // Ensure the file belongs to the current user
            {
                return NotFound();
            }

            if (file.IsByte && file.FileContentByte != null)
            {
                // For binary files, display a message that printing is not supported
                TempData["Message"] = "Printing binary files is not supported.";
                return RedirectToAction("Files");
            }

            if (!string.IsNullOrEmpty(file.FileContentText))
            {
                // For text files, render a printable view
                ViewBag.FileName = $"{file.FileName}{file.FileExtension}";
                ViewBag.FileContent = file.FileContentText;
                return View("Print");
            }

            return NotFound();
        }
    }
}