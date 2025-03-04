namespace Core.Interfaces
{
    public interface ITritemiusCipherService
    {
        Task<string> EncryptAsync(string text, int a, int b, string language);
        Task<string> DecryptAsync(string text, int a, int b, string language);
        Task<string> EncryptAsync(string text, int a, int b, int c, string language);
        Task<string> DecryptAsync(string text, int a, int b, int c, string language);
        Task<string> EncryptAsync(string text, string gaslo, string language);
        Task<string> DecryptAsync(string text, string gaslo, string language);
        (int? A, int B, int C)? FindKey(string plainText, string encryptedText, string language);
    }
}