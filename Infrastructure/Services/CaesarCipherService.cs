using Core.Entities;
using Core.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public partial class CaesarCipherService : ICaesarCipherService
    {
        private readonly HashSet<string> _englishDictionary;
        private readonly HashSet<string> _ukrainianDictionary;

        public CaesarCipherService()
        {
            _englishDictionary = LoadDictionary("/Users/maxhotiuk/Desktop/6sem/MOC/MVC/wwwroot/dictionaries/English.dic");
            _ukrainianDictionary = LoadDictionary("/Users/maxhotiuk/Desktop/6sem/MOC/MVC/wwwroot/dictionaries/Ukrainian.dic");
        }

        public Task<string> EncryptAsync(string text, int key, string language)
        {
            return Task.FromResult(CaesarCipher(text, key, language));
        }

        public Task<string> DecryptAsync(string text, int key, string language)
        {
            return Task.FromResult(CaesarCipher(text, -key, language));
        }

        public Task<byte[]> EncryptAsync(byte[] data, int key)
        {
            return Task.FromResult(CaesarCipher(data, key));
        }

        public Task<byte[]> DecryptAsync(byte[] data, int key)
        {
            return Task.FromResult(CaesarCipher(data, -key));
        }

        public Dictionary<string, int> BruteForceAttack(string text, string language)
        {
            var validResults = new Dictionary<string, int>();
            int max_key = language == "English" ? 36 : 43;
            for (int key = 0; key <= 33; key++)
            {
                string decryptedText = DecryptAsync(text, key, language).Result;
                if (IsValidText(decryptedText, _englishDictionary) || IsValidText(decryptedText, _ukrainianDictionary))
                {
                    validResults.Add(decryptedText, key);
                }
            }
            return validResults;
        }

        private static string CaesarCipher(string text, int key, string language)
        {
            char[] alphabet = Alphabet(language);
            var encryptedText = new StringBuilder();
            foreach (var symbol in text)
            {
                if (alphabet.Contains(symbol))
                {
                    int index = Array.IndexOf(alphabet, symbol);
                    int newIndex = (index + key) % alphabet.Length;
                    if (newIndex < 0)
                    {
                        newIndex += alphabet.Length;
                    }
                    _ = encryptedText.Append(alphabet[newIndex]);
                }
                else
                {
                    encryptedText.Append(symbol);
                }
            }
            return encryptedText.ToString();
        }

        private static byte[] CaesarCipher(byte[] data, int key)
        {
            return [.. data.Select(b => (byte)(b + key))];
        }

        private static char[] Alphabet(string language)
        {
            char[] englishAlphabet = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', ' ', ',', '.', '!', '?', ':', ';', '(', ')', '-', '"'];
            char[] ukrainianAlphabet = ['а', 'б', 'в', 'г', 'ґ', 'д', 'е', 'є', 'ж', 'з', 'и', 'і', 'ї', 'й', 'к', 'л', 'м', 'н', 'о', 'п', 'р', 'с', 'т', 'у', 'ф', 'х', 'ц', 'ч', 'ш', 'щ', 'ь', 'ю', 'я', 'А', 'Б', 'В', 'Г', 'Ґ', 'Д', 'Е', 'Є', 'Ж', 'З', 'И', 'І', 'Ї', 'Й', 'К', 'Л', 'М', 'Н', 'О', 'П', 'Р', 'С', 'Т', 'У', 'Ф', 'Х', 'Ц', 'Ч', 'Ш', 'Щ', 'Ь', 'Ю', 'Я', ' ', ',', '.', '!', '?', ':', ';', '(', ')', '-', '"'];
            return language == "English" ? englishAlphabet : ukrainianAlphabet;
        }

        public static bool IsValidText(string text, HashSet<string> dictionary)
        {
            var words = MyRegex().Split(text.ToLower());
            int validWordCount = words.Count(word => dictionary.Contains(word));
            return validWordCount > words.Length * 0.5;
        }

        public static HashSet<string> LoadDictionary(string filePath)
        {
            var dictionary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (System.IO.File.Exists(filePath))
            {
                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines.Skip(1))
                {
                    var word = line.Split('/')[0].Trim();
                    if (!string.IsNullOrEmpty(word))
                    {
                        dictionary.Add(word);
                    }
                }
            }

            return dictionary;
        }

        [GeneratedRegex(@"\W+")]
        private static partial Regex MyRegex();
    }
}