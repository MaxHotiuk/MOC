using Core.Interfaces;
using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class DiffieHellmanService : IDiffieHellmanService
    {
        private readonly IRSAService _rsaService;

        public DiffieHellmanService(IRSAService rsaService)
        {
            _rsaService = rsaService;
        }

        // Генерація великого простого числа для використання як модуль
        public async Task<BigInteger> GeneratePrimeAsync(int bitLength = 32)
        {
            return await Task.Run(() => {
                RandomNumberGenerator rng = RandomNumberGenerator.Create();
                byte[] bytes = new byte[bitLength / 8];
                BigInteger prime;
                
                do
                {
                    rng.GetBytes(bytes);
                    prime = new BigInteger(bytes);
                    if (prime < 0) prime = -prime; // Забезпечуємо позитивність
                    
                    // Встановлюємо найбільший та найменший біти
                    prime |= BigInteger.One << (bitLength - 1); // Встановлюємо старший біт
                    prime |= BigInteger.One; // Забезпечуємо непарність
                    
                } while (!_rsaService.IsPrime(prime, 20)); // Більша кількість ітерацій для надійності
                
                return prime;
            });
        }

        // Знаходження первісного кореня за модулем p
        public async Task<BigInteger> FindPrimitiveRootAsync(BigInteger p)
        {
            return await Task.Run(() => {
                // Знаходимо phi(p) = p-1
                BigInteger phi = p - 1;
                
                // Знаходимо прості множники phi(p)
                var factors = FindPrimeFactors(phi);
                
                // Перевіряємо кандидатів на примітивний корінь
                for (BigInteger g = 2; g < p; g++)
                {
                    bool isPrimitive = true;
                    
                    foreach (var factor in factors)
                    {
                        // Перевіряємо, чи g^(phi/factor) mod p = 1
                        BigInteger power = phi / factor;
                        if (_rsaService.ModPow(g, power, p) == 1)
                        {
                            isPrimitive = false;
                            break;
                        }
                    }
                    
                    if (isPrimitive)
                        return g;
                }
                
                // Якщо не знайдено, повертаємо значення за замовчуванням
                return new BigInteger(2);
            });
        }

        // Знаходження простих множників числа
        private HashSet<BigInteger> FindPrimeFactors(BigInteger n)
        {
            HashSet<BigInteger> factors = new HashSet<BigInteger>();
            
            // Перевіряємо ділення на 2
            while (n % 2 == 0)
            {
                factors.Add(2);
                n /= 2;
            }
            
            // Перевіряємо непарні числа
            for (BigInteger i = 3; i * i <= n; i += 2)
            {
                while (n % i == 0)
                {
                    factors.Add(i);
                    n /= i;
                }
            }
            
            // Якщо n > 1, то n також є простим числом
            if (n > 1)
                factors.Add(n);
                
            return factors;
        }

        // Генерація випадкового приватного ключа
        public async Task<BigInteger> GeneratePrivateKeyAsync(int bitLength = 16)
        {
            return await Task.Run(() => {
                RandomNumberGenerator rng = RandomNumberGenerator.Create();
                byte[] bytes = new byte[bitLength / 8];
                
                rng.GetBytes(bytes);
                BigInteger privateKey = new BigInteger(bytes);
                if (privateKey < 0) privateKey = -privateKey; // Забезпечуємо позитивність
                
                return privateKey;
            });
        }

        // Обчислення публічного ключа
        public async Task<BigInteger> ComputePublicKeyAsync(BigInteger g, BigInteger privateKey, BigInteger p)
        {
            return await Task.Run(() => _rsaService.ModPow(g, privateKey, p));
        }

        // Обчислення спільного секретного ключа
        public async Task<BigInteger> ComputeSharedSecretAsync(BigInteger otherPublicKey, BigInteger privateKey, BigInteger p)
        {
            return await Task.Run(() => _rsaService.ModPow(otherPublicKey, privateKey, p));
        }

        // Виведення обчисленого секретного ключа у вигляді хешу
        public string DeriveKeyMaterial(BigInteger sharedSecret, int keySize = 32)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] secretBytes = sharedSecret.ToByteArray();
                byte[] hashBytes = sha256.ComputeHash(secretBytes);
                
                // Обмежуємо розмір ключа
                byte[] keyBytes = new byte[keySize];
                Array.Copy(hashBytes, keyBytes, Math.Min(hashBytes.Length, keySize));
                
                return BitConverter.ToString(keyBytes).Replace("-", "");
            }
        }

        // Шифрування тексту з використанням спільного ключа
        public string EncryptText(string plainText, string keyMaterial)
        {
            if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(keyMaterial))
                throw new ArgumentException("Text and key material cannot be empty");
            
            try
            {
                // Конвертуємо ключовий матеріал з hex в байти
                byte[] keyBytes = ConvertHexToBytes(keyMaterial);
                
                // Генеруємо випадковий IV
                byte[] iv = new byte[16]; // 16 bytes for AES
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }
                
                // Підготовка алгоритму шифрування
                using (var aes = Aes.Create())
                {
                    aes.Key = keyBytes.Length == 32 ? keyBytes : ResizeKey(keyBytes, 32); // AES-256
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    // Шифруємо текст
                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new System.IO.MemoryStream())
                    {
                        // Спочатку записуємо IV у потік
                        ms.Write(iv, 0, iv.Length);
                        
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (var sw = new System.IO.StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        
                        // Конвертуємо зашифровані байти в base64 для зручного зберігання
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Encryption error: {ex.Message}", ex);
            }
        }

        // Розшифрування тексту з використанням спільного ключа
        public string DecryptText(string cipherText, string keyMaterial)
        {
            if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(keyMaterial))
                throw new ArgumentException("Cipher text and key material cannot be empty");
            
            try
            {
                // Конвертуємо ключовий матеріал з hex в байти
                byte[] keyBytes = ConvertHexToBytes(keyMaterial);
                
                // Конвертуємо зашифрований текст з base64 в байти
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                
                // Перевіряємо мінімальну довжину (повинна включати хоча б IV)
                if (cipherBytes.Length < 16)
                    throw new ArgumentException("Invalid cipher text format");
                
                // Отримуємо IV з перших 16 байтів
                byte[] iv = new byte[16];
                Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                
                // Підготовка алгоритму розшифрування
                using (var aes = Aes.Create())
                {
                    aes.Key = keyBytes.Length == 32 ? keyBytes : ResizeKey(keyBytes, 32); // AES-256
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    // Розшифровуємо текст
                    using (var ms = new System.IO.MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length))
                    using (var decryptor = aes.CreateDecryptor())
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new System.IO.StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Decryption error: {ex.Message}", ex);
            }
        }

        // Допоміжні методи

        // Конвертація hex рядка в масив байтів
        private byte[] ConvertHexToBytes(string hex)
        {
            // Видаляємо всі пробіли та переведемо в верхній регістр
            hex = hex.Replace(" ", "").ToUpper();
            
            // Перевіряємо чи це валідний hex рядок
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Invalid hex string");
            
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            
            return bytes;
        }

        // Змінюємо розмір ключа до необхідної довжини 
        private byte[] ResizeKey(byte[] originalKey, int targetSize)
        {
            byte[] resizedKey = new byte[targetSize];
            
            if (originalKey.Length >= targetSize)
            {
                // Якщо оригінальний ключ більший, беремо перші targetSize байтів
                Array.Copy(originalKey, resizedKey, targetSize);
            }
            else
            {
                // Якщо оригінальний ключ менший, копіюємо його повністю
                // і доповнюємо решту хешем від оригінального ключа
                Array.Copy(originalKey, resizedKey, originalKey.Length);
                
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(originalKey);
                    
                    for (int i = originalKey.Length; i < targetSize; i++)
                    {
                        resizedKey[i] = hashBytes[(i - originalKey.Length) % hashBytes.Length];
                    }
                }
            }
            
            return resizedKey;
        }
    }
}