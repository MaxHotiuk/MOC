using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class XORCipherService : IXORCipherService
    {
        // Словник з алфавітами
        private readonly Dictionary<string, char[]> _alphabets = new()
        {
            { "English", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ,.!?:;()-'\"".ToCharArray() },
            { "Ukrainian", "абвгґдеєжзиіїйклмнопрстуфхцчшщьюяАБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ ,.!?:;()-'\"".ToCharArray() }
        };

        // Словники для індексів символів
        private readonly Dictionary<string, Dictionary<char, int>> _charToIndex = new();
        private readonly Dictionary<string, Dictionary<int, char>> _indexToChar = new();

        public XORCipherService()
        {
            // Ініціалізуємо словники індексів
            foreach (var languagePair in _alphabets)
            {
                string language = languagePair.Key;
                char[] alphabet = languagePair.Value;
                
                _charToIndex[language] = new Dictionary<char, int>();
                _indexToChar[language] = new Dictionary<int, char>();
                
                for (int i = 0; i < alphabet.Length; i++)
                {
                    _charToIndex[language][alphabet[i]] = i;
                    _indexToChar[language][i] = alphabet[i];
                }
            }
        }

        public Task<string> EncryptAsync(string text, string gamma, string language)
        {
            // Перевірка на використання одноразового шифроблокноту
            if (gamma.StartsWith("OTP:"))
            {
                return Task.FromResult(ProcessXOR(text, gamma.Substring(4), language, true));
            }
            
            // Стандартний XOR з повторюваною гамою
            return Task.FromResult(ProcessXOR(text, gamma, language, false));
        }

        public Task<string> DecryptAsync(string text, string gamma, string language)
        {
            // Дешифрування ідентичне шифруванню для XOR
            return EncryptAsync(text, gamma, language);
        }

        private string ProcessXOR(string text, string gamma, string language, bool isOneTimePad)
        {
            // Перевірка підтримки мови
            if (!_charToIndex.ContainsKey(language))
                throw new ArgumentException($"Unsupported language: {language}");

            var charToIndex = _charToIndex[language];
            var indexToChar = _indexToChar[language];
            int alphabetSize = _alphabets[language].Length;
            StringBuilder result = new StringBuilder();
            
            // Перевірка для одноразового шифроблокноту
            if (isOneTimePad && gamma.Length < text.Length)
                throw new ArgumentException("One-time pad must be at least as long as the message");

            // Розширення гами до довжини тексту, якщо не одноразовий шифроблокнот
            string effectiveGamma = isOneTimePad ? gamma : ExtendGamma(gamma, text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                char textChar = text[i];

                // Якщо символ відсутній в алфавіті, залишаємо його без змін
                if (!charToIndex.ContainsKey(textChar))
                {
                    result.Append(textChar);
                    continue;
                }

                char gammaChar = effectiveGamma[i % effectiveGamma.Length];
                
                // Перевірка символу гами
                if (!charToIndex.ContainsKey(gammaChar))
                    throw new ArgumentException($"Gamma contains invalid character '{gammaChar}' for the selected language.");
                
                // Отримуємо індекси символів
                int textIndex = charToIndex[textChar];
                int gammaIndex = charToIndex[gammaChar];
                
                // XOR операція над індексами, з урахуванням модуля
                int resultIndex = (textIndex ^ gammaIndex) % alphabetSize;
                
                // Додаємо відповідний символ до результату
                result.Append(indexToChar[resultIndex]);
            }

            return result.ToString();
        }

        // Метод для розширення гами до потрібної довжини
        private string ExtendGamma(string gamma, int length)
        {
            if (string.IsNullOrEmpty(gamma))
                throw new ArgumentException("Gamma cannot be empty");

            if (gamma.Length >= length)
                return gamma.Substring(0, length);

            StringBuilder extendedGamma = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                extendedGamma.Append(gamma[i % gamma.Length]);
            }

            return extendedGamma.ToString();
        }

        // Метод для генерації випадкового одноразового блокноту
        public string GenerateOneTimePad(int length, string language)
        {
            if (!_alphabets.ContainsKey(language))
                throw new ArgumentException($"Unsupported language: {language}");

            var alphabet = _alphabets[language];
            var result = new StringBuilder(length);
            
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[length * 2]; // Використовуємо більше байтів для кращої випадковості
                rng.GetBytes(bytes);
                
                for (int i = 0; i < length; i++)
                {
                    int index = BitConverter.ToUInt16(bytes, i * 2) % alphabet.Length;
                    result.Append(alphabet[index]);
                }
            }
            
            return result.ToString();
        }

        // Допоміжний метод для відображення двійкових представлень індексів у алфавіті
        public Dictionary<char, string> GetBinaryIndices(string language)
        {
            if (!_alphabets.ContainsKey(language))
                throw new ArgumentException($"Unsupported language: {language}");
                
            var result = new Dictionary<char, string>();
            int bitsRequired = CalculateBitsRequired(_alphabets[language].Length);
            
            foreach (var pair in _charToIndex[language])
            {
                string binary = Convert.ToString(pair.Value, 2).PadLeft(bitsRequired, '0');
                result[pair.Key] = binary;
            }
            
            return result;
        }
        
        // Метод для визначення кількості бітів для представлення алфавіту
        private int CalculateBitsRequired(int alphabetSize)
        {
            return (int)Math.Ceiling(Math.Log2(alphabetSize));
        }

        // Метод для отримання бінарного представлення тексту
        public string GetBinaryTextRepresentation(string text, string language)
        {
            var binaryIndices = GetBinaryIndices(language);
            var result = new StringBuilder();
            
            foreach (char c in text)
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                if (binaryIndices.TryGetValue(c, out string binary))
                {
                    result.Append(binary).Append(" ");
                }
                else
                {
                    result.Append("[not in alphabet] ");
                }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            }
            
            return result.ToString().Trim();
        }

        // Метод для представлення процесу шифрування у двійковій формі
        public string GetEncryptionProcess(string text, string gamma, string language)
        {
            if (!_charToIndex.ContainsKey(language))
                throw new ArgumentException($"Unsupported language: {language}");
                
            var charToIndex = _charToIndex[language];
            var binaryIndices = GetBinaryIndices(language);
            var alphabetSize = _alphabets[language].Length;
            var bitsRequired = CalculateBitsRequired(alphabetSize);
            
            string extendedGamma = ExtendGamma(gamma, text.Length);
            var result = new StringBuilder();
            
            result.AppendLine($"Alphabet size: {alphabetSize}, Bits required: {bitsRequired}");
            result.AppendLine();
            
            for (int i = 0; i < text.Length; i++)
            {
                char textChar = text[i];
                char gammaChar = extendedGamma[i % extendedGamma.Length];
                
                if (!charToIndex.ContainsKey(textChar))
                {
                    result.AppendLine($"Character '{textChar}' not in alphabet, keeping as is");
                    continue;
                }
                
                if (!charToIndex.ContainsKey(gammaChar))
                {
                    result.AppendLine($"Gamma character '{gammaChar}' not in alphabet");
                    continue;
                }
                
                int textIndex = charToIndex[textChar];
                int gammaIndex = charToIndex[gammaChar];
                int xorResult = textIndex ^ gammaIndex;
                int modResult = xorResult % alphabetSize;
                
                string textBinary = binaryIndices[textChar];
                string gammaBinary = binaryIndices[gammaChar];
                string xorBinary = Convert.ToString(xorResult, 2).PadLeft(bitsRequired * 2, '0');
                string modBinary = Convert.ToString(modResult, 2).PadLeft(bitsRequired, '0');
                
                result.AppendLine($"Text char: '{textChar}', index: {textIndex}, binary: {textBinary}");
                result.AppendLine($"Gamma char: '{gammaChar}', index: {gammaIndex}, binary: {gammaBinary}");
                result.AppendLine($"XOR result: {xorResult}, binary: {xorBinary}");
                result.AppendLine($"Mod result: {modResult}, binary: {modBinary}, char: '{_indexToChar[language][modResult]}'");
                result.AppendLine();
            }
            
            return result.ToString();
        }
    }
}