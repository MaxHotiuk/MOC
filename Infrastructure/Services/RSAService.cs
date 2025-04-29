using Core.Interfaces;
using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class RSAService : IRSAService
    {
        // Реалізація алгоритму бінарного піднесення до степеня за модулем
        public BigInteger ModPow(BigInteger b, BigInteger e, BigInteger m)
        {
            // Перевірка граничних випадків
            if (m == 1) return 0;
            if (e == 0) return 1;
            
            BigInteger result = 1;
            b = b % m;
            
            while (e > 0)
            {
                // Якщо поточний біт експоненти дорівнює 1
                if (e % 2 == 1)
                {
                    result = (result * b) % m;
                }
                
                // Підготовка до наступного біту експоненти
                e = e >> 1; // Бітовий зсув вправо - ділення на 2
                b = (b * b) % m; // Квадрат основи за модулем
            }
            
            return result;
        }
        
        // Перевірка числа на простоту за допомогою тесту Міллера-Рабіна
        public bool IsPrime(BigInteger n, int k = 10)
        {
            // Перевірка особливих випадків
            if (n <= 1) return false;
            if (n <= 3) return true;
            if (n % 2 == 0) return false;
            
            // Представимо n - 1 як 2^r * d, де d непарне
            BigInteger d = n - 1;
            int r = 0;
            
            while (d % 2 == 0)
            {
                d /= 2;
                r++;
            }
            
            // Проведемо k раундів тесту Міллера-Рабіна
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[n.ToByteArray().Length];
            
            for (int i = 0; i < k; i++)
            {
                BigInteger a;
                do
                {
                    rng.GetBytes(bytes);
                    a = new BigInteger(bytes);
                    if (a < 0) a = -a; // Гарантуємо, що a позитивне
                } while (a < 2 || a >= n - 2);
                
                BigInteger x = ModPow(a, d, n);
                
                if (x == 1 || x == n - 1)
                    continue;
                
                bool isProbablePrime = false;
                for (int j = 0; j < r - 1; j++)
                {
                    x = ModPow(x, 2, n);
                    if (x == n - 1)
                    {
                        isProbablePrime = true;
                        break;
                    }
                }
                
                if (!isProbablePrime)
                    return false;
            }
            
            return true;
        }
        
        // Генерація випадкового простого числа потрібної довжини
        private BigInteger GeneratePrime(int bitLength)
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[bitLength / 8];
            BigInteger number;
            
            do
            {
                rng.GetBytes(bytes);
                number = new BigInteger(bytes);
                if (number < 0) number = -number; // Забезпечуємо позитивність
                
                // Встановлюємо найбільший та найменший біти
                number |= BigInteger.One << (bitLength - 1); // Встановлюємо старший біт
                number |= BigInteger.One; // Забезпечуємо непарність
                
            } while (!IsPrime(number, 20)); // Більша кількість ітерацій для надійності
            
            return number;
        }
        
        // Алгоритм Евкліда для знаходження НСД
        private BigInteger Gcd(BigInteger a, BigInteger b)
        {
            while (b != 0)
            {
                BigInteger temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }
        
        // Розширений алгоритм Евкліда для знаходження мультиплікативного оберненого елемента
        private BigInteger ModInverse(BigInteger a, BigInteger m)
        {
            BigInteger m0 = m;
            BigInteger y = 0, x = 1;
            
            if (m == 1)
                return 0;
            
            while (a > 1)
            {
                BigInteger q = a / m;
                BigInteger t = m;
                
                m = a % m;
                a = t;
                t = y;
                
                y = x - q * y;
                x = t;
            }
            
            // Переконуємось, що x додатнє
            if (x < 0)
                x += m0;
            
            return x;
        }
        
        // Генерація ключів RSA
        public async Task<(BigInteger p, BigInteger q, BigInteger n, BigInteger e, BigInteger d)> GenerateKeysAsync(int bitLength = 1024)
        {
            // Генеруємо два великих простих числа p і q
            BigInteger p, q;
            
            // Генеруємо p і q як окремі завдання
            var pTask = Task.Run(() => GeneratePrime(bitLength / 2));
            var qTask = Task.Run(() => GeneratePrime(bitLength / 2));
            
            await Task.WhenAll(pTask, qTask);
            
            p = pTask.Result;
            q = qTask.Result;
            
            // Обчислюємо n = p * q
            BigInteger n = p * q;
            
            // Обчислюємо функцію Ейлера φ(n) = (p-1) * (q-1)
            BigInteger phi = (p - 1) * (q - 1);
            
            // Вибираємо відкритий показник e, взаємно простий з φ(n)
            BigInteger e = 65537; // Часто використовуваний показник, який є простим числом Ферма
            
            // Перевіряємо, що e взаємно просте з phi
            if (Gcd(e, phi) != 1)
            {
                // Якщо не взаємно просте, шукаємо інше e
                for (e = 65537; e < phi; e += 2)
                {
                    if (Gcd(e, phi) == 1)
                        break;
                }
            }
            
            // Обчислюємо таємний ключ d такий, що (e * d) % phi = 1
            BigInteger d = ModInverse(e, phi);
            
            return (p, q, n, e, d);
        }
        
        // Шифрування повідомлення
        public Task<string> EncryptAsync(string plainText, BigInteger e, BigInteger n)
        {
            // Отримуємо числові значення символів Unicode
            var encrypted = new StringBuilder();
            
            foreach (char c in plainText)
            {
                // Перетворюємо символ на його числове значення Unicode
                BigInteger m = new BigInteger(c);
                
                // Шифруємо використовуючи c = m^e mod n
                BigInteger c_encrypted = ModPow(m, e, n);
                
                // Додаємо до зашифрованого рядка та відокремлюємо символом ","
                encrypted.Append(c_encrypted.ToString());
                encrypted.Append(",");
            }
            
            // Видаляємо останню кому
            if (encrypted.Length > 0)
                encrypted.Length--;
            
            return Task.FromResult(encrypted.ToString());
        }

        // Розшифрування повідомлення
        public Task<string> DecryptAsync(string cipherText, BigInteger d, BigInteger n)
        {
            string[] parts = cipherText.Split(',');
            var decrypted = new StringBuilder();
            
            foreach (string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;
                
                // Парсимо зашифроване значення
                if (!BigInteger.TryParse(part, out BigInteger c))
                    continue;
                
                // Розшифровуємо використовуючи m = c^d mod n
                BigInteger m = ModPow(c, d, n);
                
                // Конвертуємо назад у символ Unicode
                char character = (char)(int)m;
                decrypted.Append(character);
            }
            
            return Task.FromResult(decrypted.ToString());
        }
    }
}