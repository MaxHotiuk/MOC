namespace Core.Interfaces
{
    public interface ICaesarCipherService
    {
        Task<string> EncryptAsync(string text, int key);
        Task<string> DecryptAsync(string text, int key);
        Task<byte[]> EncryptAsync(byte[] data, int key);
        Task<byte[]> DecryptAsync(byte[] data, int key);
        public Dictionary<string, int> BruteForceAttack(string text);
    }
}