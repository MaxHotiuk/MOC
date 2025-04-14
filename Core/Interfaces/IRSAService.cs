using System.Numerics;

namespace Core.Interfaces;

public interface IRSAService
{
    // Генерація ключів
    Task<(BigInteger p, BigInteger q, BigInteger n, BigInteger e, BigInteger d)> GenerateKeysAsync(int bitLength = 1024);
    
    // Шифрування з відкритим ключем
    Task<string> EncryptAsync(string plainText, BigInteger e, BigInteger n);
    
    // Розшифрування з закритим ключем
    Task<string> DecryptAsync(string cipherText, BigInteger d, BigInteger n);
    
    // Перевірка чи число просте
    bool IsPrime(BigInteger n, int k = 10);
    
    // Бінарний алгоритм піднесення до степеня за модулем
    BigInteger ModPow(BigInteger b, BigInteger e, BigInteger m);
}
