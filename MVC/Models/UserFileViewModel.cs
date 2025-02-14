namespace MVC.Models;

public class UserFileViewModel
{
    public int Id { get; set; }
    public string FileName { get; set; } = null!;
    public string FileExtension { get; set; } = null!;
    public bool IsByte { get; set; }
}
