using System.Numerics;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IDiffieHellmanService
    {
        // Генерація великого простого числа для використання як модуль
        Task<BigInteger> GeneratePrimeAsync(int bitLength = 32);
        
        // Знаходження первісного кореня за модулем p
        Task<BigInteger> FindPrimitiveRootAsync(BigInteger p);
        
        // Генерація випадкового приватного ключа
        Task<BigInteger> GeneratePrivateKeyAsync(int bitLength = 16);
        
        // Обчислення публічного ключа
        Task<BigInteger> ComputePublicKeyAsync(BigInteger g, BigInteger privateKey, BigInteger p);
        
        // Обчислення спільного секретного ключа
        Task<BigInteger> ComputeSharedSecretAsync(BigInteger otherPublicKey, BigInteger privateKey, BigInteger p);
        
        // Виведення обчисленого секретного ключа у вигляді хешу
        string DeriveKeyMaterial(BigInteger sharedSecret, int keySize = 32);

        // Шифрування тексту з використанням спільного ключа
        string EncryptText(string plainText, string keyMaterial);

        // Розшифрування тексту з використанням спільного ключа
        string DecryptText(string cipherText, string keyMaterial);
    }
}