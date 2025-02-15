namespace MVC.Models;
public class TempFileInfoModel
{
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public DateTime ExpirationTime { get; set; }
}