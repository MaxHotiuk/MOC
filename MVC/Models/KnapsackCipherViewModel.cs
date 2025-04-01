using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MVC.Models
{
    public class KnapsackCipherViewModel
    {
        // File information
        public int FileId { get; set; }
        public string FileContent { get; set; } = "No content";
        
        // Keys information
        public string? PublicKey { get; set; }
        public string? PrivateKey { get; set; }
        public string? Modulus { get; set; }
        public string? Multiplier { get; set; }
        public int BitLength { get; set; } = 8;
        
        // Process details
        public string? EncryptionProcess { get; set; }
        public string? DecryptionProcess { get; set; }
        
        // UI messages
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        
        // Language selection
        [Display(Name = "Language")]
        public string Language { get; set; } = "eng";
        
        // Key import form
        public KeyImportModel KeyImport { get; set; } = new KeyImportModel();
        
        public bool HasKeys => !string.IsNullOrEmpty(PublicKey) && !string.IsNullOrEmpty(PrivateKey);
        public bool HasPublicKey => !string.IsNullOrEmpty(PublicKey);
        public bool HasPrivateKey => !string.IsNullOrEmpty(PrivateKey) && !string.IsNullOrEmpty(Modulus) && !string.IsNullOrEmpty(Multiplier);
    }
    
    public class KeyImportModel
    {
        [Display(Name = "Public Key (comma separated)")]
        public string? PublicKey { get; set; }

        [Display(Name = "Private Key (comma separated)")]
        public string? PrivateKey { get; set; }

        [Display(Name = "Modulus (n)")]
        public string? Modulus { get; set; }

        [Display(Name = "Multiplier (m)")]
        public string? Multiplier { get; set; }
    }
}