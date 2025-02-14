namespace Core.Models
{
    public class FileUploadModel
    {
        public IFormFile? File { get; set; }
        public string? FileContentText { get; set; }
        public string? FileName { get; set; } = null!;
        public string? FileExtension { get; set; } = null!;
        public bool IsByte { get; set; } = false;
    }
}