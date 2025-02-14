namespace Core.Entities;

public class DbFile
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public string? FileContentText { get; set; }
    public byte[]? FileContentByte { get; set; }
    public string FileName { get; set; } = null!;
    public string FileExtension { get; set; } = null!;
    public bool IsByte { get; set; }
}
