using Core.Entities;

namespace Core.Interfaces
{
    public interface IXORCipherService
    {
        Task<string> EncryptAsync(string text, string gamma, string language);
        Task<string> DecryptAsync(string text, string gamma, string language);
        string GenerateOneTimePad(int length, string language);
    }
}