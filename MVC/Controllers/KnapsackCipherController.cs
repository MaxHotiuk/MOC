using Microsoft.AspNetCore.Mvc;
using Core.Interfaces;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MVC.Models;
using Infrastructure.Services;
using System.Text.Json.Serialization;

namespace MVC.Controllers
{
    public class KnapsackCipherController : Controller
    {
        private readonly IKnapsackCipherService _knapsackCipherService;
        private readonly IFileService _fileService;

        public KnapsackCipherController(IKnapsackCipherService knapsackCipherService, IFileService fileService)
        {
            _knapsackCipherService = knapsackCipherService;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int fileId)
        {
            var model = await PrepareViewModel(fileId);
            return View(model);
        }

        private async Task<KnapsackCipherViewModel> PrepareViewModel(int fileId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var file = await _fileService.GetFileByIdAsync(fileId, userId!);
            
            var model = new KnapsackCipherViewModel
            {
                FileId = fileId,
                FileContent = file?.FileContentText ?? "No content"
            };

            // Load keys from database if available
            if (file != null && !string.IsNullOrEmpty(file.PublicKeyJson))
            {
                var publicKey = JsonSerializer.Deserialize<List<int>>(file.PublicKeyJson);
                model.PublicKey = string.Join(", ", publicKey!);
            }

            if (file != null && !string.IsNullOrEmpty(file.PrivateKeyJson))
            {
                var privateKey = JsonSerializer.Deserialize<List<int>>(file.PrivateKeyJson);
                model.PrivateKey = string.Join(", ", privateKey!);
            }

            if (file != null && !string.IsNullOrEmpty(file.Modulus))
                model.Modulus = file.Modulus;

            if (file != null && !string.IsNullOrEmpty(file.Multiplier))
                model.Multiplier = file.Multiplier;

            if (file != null && file.KeyBitLength.HasValue)
                model.BitLength = file.KeyBitLength.Value;

            // Retrieve any TempData messages
            if (TempData["SuccessMessage"] != null)
                model.SuccessMessage = TempData["SuccessMessage"]!.ToString();

            if (TempData["ErrorMessage"] != null)
                model.ErrorMessage = TempData["ErrorMessage"]!.ToString();

            if (TempData["EncryptionProcess"] != null)
                model.EncryptionProcess = TempData["EncryptionProcess"]!.ToString();

            if (TempData["DecryptionProcess"] != null)
                model.DecryptionProcess = TempData["DecryptionProcess"]!.ToString();

            return model;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateKeys(int fileId, int bitLength = 8)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var file = await _fileService.GetFileByIdAsync(fileId, userId!);
                
                if (file == null)
                    return NotFound("File not found");

                var (privateKey, publicKey, m, n) = _knapsackCipherService.GenerateKeyPair(bitLength);
                
                // Store keys in database
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false
                };
                
                file.PublicKeyJson = JsonSerializer.Serialize(publicKey, options);
                file.PrivateKeyJson = JsonSerializer.Serialize(privateKey, options);
                file.Modulus = n.ToString();
                file.Multiplier = m.ToString();
                file.KeyBitLength = bitLength;
                
                await _fileService.UpdateFileAsync(file);
                
                TempData["SuccessMessage"] = "Keys generated and stored successfully";
                
                return RedirectToAction("Index", new { fileId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Key generation error: {ex.Message}";
                return RedirectToAction("Index", new { fileId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Encrypt(int fileId, string language = "eng")
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

                // Get public key from database
                if (string.IsNullOrEmpty(file.PublicKeyJson))
                {
                    TempData["ErrorMessage"] = "Public key not found. Please generate keys first.";
                    return RedirectToAction("Index", new { fileId });
                }
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };
                
                var publicKey = JsonSerializer.Deserialize<List<int>>(file.PublicKeyJson, options);
                
                if (publicKey == null)
                {
                    TempData["ErrorMessage"] = "Invalid public key format.";
                    return RedirectToAction("Index", new { fileId });
                }
                
                // Encrypt the message
                var encryptedNumbers = _knapsackCipherService.Encrypt(file.FileContentText, publicKey, language);
                string encryptedMessage = string.Join(" ", encryptedNumbers);
                
                // Save encryption process details for display
                var processDetails = new StringBuilder();
                processDetails.AppendLine("Original message: " + file.FileContentText);
                processDetails.AppendLine("Public key: " + string.Join(", ", publicKey));
                processDetails.AppendLine("Encrypted numbers: " + string.Join(", ", encryptedNumbers));
                TempData["EncryptionProcess"] = processDetails.ToString();
                
                // Update file content with encrypted text
                file.FileContentText = encryptedMessage;
                await _fileService.UpdateFileAsync(file);
                
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
        public async Task<IActionResult> Decrypt(int fileId, string language = "eng")
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var file = await _fileService.GetFileByIdAsync(fileId, userId!);
                
                if (file == null)
                    return NotFound("File not found");
                
                // Get keys from database
                if (string.IsNullOrEmpty(file.PrivateKeyJson) || string.IsNullOrEmpty(file.Modulus) || string.IsNullOrEmpty(file.Multiplier))
                {
                    TempData["ErrorMessage"] = "Private key, modulus or multiplier not found. Please generate keys first.";
                    return RedirectToAction("Index", new { fileId });
                }
                
                var privateKey = JsonSerializer.Deserialize<List<int>>(file.PrivateKeyJson);
                var n = int.Parse(file.Modulus);
                var m = int.Parse(file.Multiplier);
                
                // Parse encrypted numbers
                var encryptedNumbers = file.FileContentText!.Split(' ')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(int.Parse)
                    .ToList();
                
                // Decrypt the message
                string decryptedMessage = _knapsackCipherService.Decrypt(encryptedNumbers, privateKey!, m, n, language);
                
                // Save decryption process details for display
                var processDetails = new StringBuilder();
                processDetails.AppendLine("Encrypted numbers: " + file.FileContentText);
                processDetails.AppendLine("Private key: " + string.Join(", ", privateKey!));
                processDetails.AppendLine("Modulus (n): " + n);
                processDetails.AppendLine("Multiplier (m): " + m);
                processDetails.AppendLine("Decrypted message: " + decryptedMessage);
                TempData["DecryptionProcess"] = processDetails.ToString();
                
                // Update file content with decrypted text
                file.FileContentText = decryptedMessage;
                await _fileService.UpdateFileAsync(file);
                
                TempData["SuccessMessage"] = "Text decrypted successfully";
                
                return RedirectToAction("Index", new { fileId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Decryption error: {ex.Message}";
                return RedirectToAction("Index", new { fileId });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> DownloadKeys(int fileId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var file = await _fileService.GetFileByIdAsync(fileId, userId!);
            
            if (file == null || string.IsNullOrEmpty(file.PublicKeyJson) || string.IsNullOrEmpty(file.PrivateKeyJson) || 
                string.IsNullOrEmpty(file.Modulus) || string.IsNullOrEmpty(file.Multiplier))
            {
                TempData["ErrorMessage"] = "Keys not found. Please generate keys first.";
                return RedirectToAction("Index", new { fileId });
            }
            
            var publicKey = JsonSerializer.Deserialize<List<int>>(file.PublicKeyJson);
            var privateKey = JsonSerializer.Deserialize<List<int>>(file.PrivateKeyJson);
            
            var sb = new StringBuilder();
            sb.AppendLine("Knapsack Cipher Keys");
            sb.AppendLine("-------------------");
            sb.AppendLine();
            sb.AppendLine("Public Key (Open for sharing):");
            sb.AppendLine(string.Join(", ", publicKey!));
            sb.AppendLine();
            sb.AppendLine("======= PRIVATE KEY (KEEP SECURE) =======");
            sb.AppendLine($"Private sequence: {string.Join(", ", privateKey!)}");
            sb.AppendLine($"Modulus (n): {file.Modulus}");
            sb.AppendLine($"Multiplier (m): {file.Multiplier}");
            
            byte[] fileBytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(fileBytes, "text/plain", "knapsack_keys.txt");
        }

        [HttpPost]
        public async Task<IActionResult> ImportKeys(KnapsackCipherViewModel model, int fileId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var file = await _fileService.GetFileByIdAsync(fileId, userId!);
                
                if (file == null)
                    return NotFound("File not found");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false
                };

                // Case 1: Importing only public key
                if (!string.IsNullOrEmpty(model.KeyImport.PublicKey) && 
                    string.IsNullOrEmpty(model.KeyImport.PrivateKey) &&
                    string.IsNullOrEmpty(model.KeyImport.Modulus) &&
                    string.IsNullOrEmpty(model.KeyImport.Multiplier))
                {
                    var publicKeyList = model.KeyImport.PublicKey.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => int.Parse(k.Trim()))
                        .ToList();
                    
                    file.PublicKeyJson = JsonSerializer.Serialize(publicKeyList, options);
                    await _fileService.UpdateFileAsync(file);
                    
                    TempData["SuccessMessage"] = "Public key imported successfully.";
                    return RedirectToAction("Index", new { fileId });
                }
                
                // Case 2: Importing private key + n + m
                if (!string.IsNullOrEmpty(model.KeyImport.PrivateKey) && 
                    !string.IsNullOrEmpty(model.KeyImport.Modulus) &&
                    !string.IsNullOrEmpty(model.KeyImport.Multiplier))
                {
                    var privateKeyList = model.KeyImport.PrivateKey.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => int.Parse(k.Trim()))
                        .ToList();
                    
                    var n = int.Parse(model.KeyImport.Modulus.Trim());
                    var m = int.Parse(model.KeyImport.Multiplier.Trim());
                    
                    // Verify private key is a superincreasing sequence
                    bool isSuperIncreasing = true;
                    int sum = 0;
                    foreach (var num in privateKeyList)
                    {
                        if (num <= sum)
                        {
                            isSuperIncreasing = false;
                            break;
                        }
                        sum += num;
                    }
                    
                    if (!isSuperIncreasing)
                    {
                        TempData["ErrorMessage"] = "Invalid private key. The sequence is not superincreasing.";
                        return RedirectToAction("Index", new { fileId });
                    }
                    
                    // Generate public key from private key
                    var publicKeyList = privateKeyList.Select(x => (x * m) % n).ToList();
                    
                    file.PrivateKeyJson = JsonSerializer.Serialize(privateKeyList, options);
                    file.PublicKeyJson = JsonSerializer.Serialize(publicKeyList, options);
                    file.Modulus = n.ToString();
                    file.Multiplier = m.ToString();
                    file.KeyBitLength = privateKeyList.Count;
                    
                    await _fileService.UpdateFileAsync(file);
                    
                    TempData["SuccessMessage"] = "Private key with parameters imported successfully. Public key generated.";
                    return RedirectToAction("Index", new { fileId });
                }
                
                //Case 3: All keys provided
                if (!string.IsNullOrEmpty(model.KeyImport.PublicKey) && 
                    !string.IsNullOrEmpty(model.KeyImport.PrivateKey) &&
                    !string.IsNullOrEmpty(model.KeyImport.Modulus) &&
                    !string.IsNullOrEmpty(model.KeyImport.Multiplier))
                {
                    var publicKeyList = model.KeyImport.PublicKey.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => int.Parse(k.Trim()))
                        .ToList();
                    var privateKeyList = model.KeyImport.PrivateKey.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => int.Parse(k.Trim()))
                        .ToList();
                    var n = int.Parse(model.KeyImport.Modulus.Trim());
                    var m = int.Parse(model.KeyImport.Multiplier.Trim());
                    // Verify private key is a superincreasing sequence

                    bool isSuperIncreasing = true;
                    int sum = 0;
                    foreach (var num in privateKeyList)
                    {
                        if (num <= sum)
                        {
                            isSuperIncreasing = false;
                            break;
                        }
                        sum += num;
                    }
                    if (!isSuperIncreasing)
                    {
                        TempData["ErrorMessage"] = "Invalid private key. The sequence is not superincreasing.";
                        return RedirectToAction("Index", new { fileId });
                    }
                    // Generate public key from private key
                    var generatedPublicKeyList = privateKeyList.Select(x => (x * m) % n).ToList();
                    if (!publicKeyList.SequenceEqual(generatedPublicKeyList))
                    {
                        TempData["ErrorMessage"] = "Invalid public key. The provided public key does not match the private key.";
                        return RedirectToAction("Index", new { fileId });
                    }
                    file.PublicKeyJson = JsonSerializer.Serialize(publicKeyList, options);
                    file.PrivateKeyJson = JsonSerializer.Serialize(privateKeyList, options);
                    file.Modulus = n.ToString();
                    file.Multiplier = m.ToString();
                    file.KeyBitLength = privateKeyList.Count;
                    await _fileService.UpdateFileAsync(file);
                    TempData["SuccessMessage"] = "Keys imported successfully.";
                    return RedirectToAction("Index", new { fileId });
                }
                
                // Case 4: Invalid combination
                TempData["ErrorMessage"] = "You must provide either:\n1. Only public key\n2. Private key + modulus + multiplier";
                return RedirectToAction("Index", new { fileId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error importing keys: {ex.Message}";
                return RedirectToAction("Index", new { fileId });
            }
        }
    }
}