using Core.Interfaces;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Entities;

namespace Infrastructure.Services
{
    public partial class TritemiusCipherService : ITritemiusCipherService
    {
        private readonly Dictionary<string, Dictionary<char, double>>? _frequencyData;

        public TritemiusCipherService()
        {
            // Load frequency data from the JSON file
            string filePath = "/Users/maxhotiuk/Desktop/6sem/MOC/MVC/wwwroot/data/frequency_data.json";
            string jsonData = File.ReadAllText(filePath);
            _frequencyData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<char, double>>>(jsonData);
        }

        private Dictionary<char, double> GetExpectedFrequencies(string language)
        {
            if (_frequencyData!.ContainsKey(language))
            {
                return _frequencyData[language];
            }
            throw new ArgumentException($"Frequency data for language '{language}' not found.");
        }
        private static char[] Alphabet(string language)
        {
            char[] englishAlphabet = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'];//, 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', ' ', ',', '.', '!', '?', ':', ';', '(', ')', '-', '"'];
            char[] ukrainianAlphabet = ['а', 'б', 'в', 'г', 'ґ', 'д', 'е', 'є', 'ж', 'з', 'и', 'і', 'ї', 'й', 'к', 'л', 'м', 'н', 'о', 'п', 'р', 'с', 'т', 'у', 'ф', 'х', 'ц', 'ч', 'ш', 'щ', 'ь', 'ю', 'я', 'А', 'Б', 'В', 'Г', 'Ґ', 'Д', 'Е', 'Є', 'Ж', 'З', 'И', 'І', 'Ї', 'Й', 'К', 'Л', 'М', 'Н', 'О', 'П', 'Р', 'С', 'Т', 'У', 'Ф', 'Х', 'Ц', 'Ч', 'Ш', 'Щ', 'Ь', 'Ю', 'Я', ' ', ',', '.', '!', '?', ':', ';', '(', ')', '-', '"'];
            return language == "English" ? englishAlphabet : ukrainianAlphabet;
        }

        public Task<string> EncryptAsync(string text, int a, int b, string language)
        {
            char[] alphabet = Alphabet(language);
            var cipher = new TrithemiusCipher(alphabet);
            var key = new TrithemiusCipher.LinKey { A = a, B = b, keyType = TrithemiusCipher.Key.KeyType.LinearAquestion };
            return Task.FromResult(cipher.Trithemius(text, TrithemiusCipher.Operation.Encrypt, key));
        }

        public Task<string> DecryptAsync(string text, int a, int b, string language)
        {
            char[] alphabet = Alphabet(language);
            var cipher = new TrithemiusCipher(alphabet);
            var key = new TrithemiusCipher.LinKey { A = a, B = b, keyType = TrithemiusCipher.Key.KeyType.LinearAquestion };
            return Task.FromResult(cipher.Trithemius(text, TrithemiusCipher.Operation.Decrypt, key));
        }

        public Task<string> EncryptAsync(string text, int a, int b, int c, string language)
        {
            char[] alphabet = Alphabet(language);
            var cipher = new TrithemiusCipher(alphabet);
            var key = new TrithemiusCipher.NonLinKey { A = a, B = b, C = c, keyType = TrithemiusCipher.Key.KeyType.NonLinearAquestion };
            return Task.FromResult(cipher.Trithemius(text, TrithemiusCipher.Operation.Encrypt, key));
        }

        public Task<string> DecryptAsync(string text, int a, int b, int c, string language)
        {
            char[] alphabet = Alphabet(language);
            var cipher = new TrithemiusCipher(alphabet);
            var key = new TrithemiusCipher.NonLinKey { A = a, B = b, C = c, keyType = TrithemiusCipher.Key.KeyType.NonLinearAquestion };
            return Task.FromResult(cipher.Trithemius(text, TrithemiusCipher.Operation.Decrypt, key));
        }

        public Task<string> EncryptAsync(string text, string gaslo, string language)
        {
            char[] alphabet = Alphabet(language);
            var cipher = new TrithemiusCipher(alphabet);
            var key = new TrithemiusCipher.Gaslo { gaslo = gaslo, keyType = TrithemiusCipher.Key.KeyType.Gaslo };
            return Task.FromResult(cipher.Trithemius(text, TrithemiusCipher.Operation.Encrypt, key));
        }

        public Task<string> DecryptAsync(string text, string gaslo, string language)
        {
            char[] alphabet = Alphabet(language);
            var cipher = new TrithemiusCipher(alphabet);
            var key = new TrithemiusCipher.Gaslo { gaslo = gaslo, keyType = TrithemiusCipher.Key.KeyType.Gaslo };
            return Task.FromResult(cipher.Trithemius(text, TrithemiusCipher.Operation.Decrypt, key));
        }

        public (int? A, int B, int C)? FindKey(string plainText, string encryptedText, string language)
        {
            char[] alphabet = Alphabet(language);
            if (plainText.Length < 3 || encryptedText.Length < 3 || plainText.Length != encryptedText.Length)
                return null;

            // Try to find a linear key first (more computationally efficient)
            var linearKey = FindLinearKey(plainText, encryptedText, alphabet);
            if (linearKey.HasValue)
            {
                var cipher = new TrithemiusCipher(alphabet);
                var key = new TrithemiusCipher.LinKey 
                { 
                    A = linearKey.Value.A, 
                    B = linearKey.Value.B, 
                    keyType = TrithemiusCipher.Key.KeyType.LinearAquestion 
                };
                
                var testEncryptedText = cipher.Trithemius(plainText, TrithemiusCipher.Operation.Encrypt, key);
                if (testEncryptedText == encryptedText)
                    return (null, linearKey.Value.A, linearKey.Value.B);
            }

            // If linear key fails, try non-linear key
            var nonLinearKey = FindNonLinearKey(plainText, encryptedText, alphabet);
            if (nonLinearKey.HasValue)
            {
                var cipher = new TrithemiusCipher(alphabet);
                var key = new TrithemiusCipher.NonLinKey 
                { 
                    A = nonLinearKey.Value.A, 
                    B = nonLinearKey.Value.B, 
                    C = nonLinearKey.Value.C, 
                    keyType = TrithemiusCipher.Key.KeyType.NonLinearAquestion 
                };
                
                var testEncryptedText = cipher.Trithemius(plainText, TrithemiusCipher.Operation.Encrypt, key);
                if (testEncryptedText == encryptedText)
                    return (nonLinearKey.Value.A, nonLinearKey.Value.B, nonLinearKey.Value.C);
            }

            // Exhaustive search if initial methods fail
            return ExhaustiveKeySearch(plainText, encryptedText, alphabet);
        }

        public List<FrequencyAttackResult> FrequencyAttack(string ciphertext, string language)
        {
            char[] alphabet = Alphabet(language);
            var cipher = new TrithemiusCipher(alphabet);
            var results = new List<FrequencyAttackResult>();

            for (int testOffset = 0; testOffset < alphabet.Length; testOffset++)
            {
                for (int testDirection = -1; testDirection <= 1; testDirection += 2)
                {
                    for (int testStep = 0; testStep < alphabet.Length; testStep++)
                    {
                        var key = new TrithemiusCipher.LinKey
                        {
                            A = testStep,
                            B = testOffset,
                            keyType = TrithemiusCipher.Key.KeyType.LinearAquestion
                        };

                        string decipheredText = cipher.Trithemius(ciphertext, TrithemiusCipher.Operation.Decrypt, key);
                        double chiSquaredScore = ChiSquaredScore(decipheredText, language);

                        results.Add(new FrequencyAttackResult
                        {
                            Offset = testOffset,
                            Direction = testDirection,
                            Step = testStep,
                            ChiSquaredScore = chiSquaredScore,
                            DecipheredText = decipheredText
                        });
                    }
                }
            }

            return results.OrderBy(r => r.ChiSquaredScore).ToList();
        }

        private Dictionary<char, int> GetObservedFrequencies(string text, string language)
        {
            var frequencies = new Dictionary<char, int>();
            var expectedFrequencies = GetExpectedFrequencies(language);

            foreach (char c in text.ToLower())
            {
                if (expectedFrequencies.ContainsKey(c))
                {
                    if (frequencies.ContainsKey(c))
                        frequencies[c]++;
                    else
                        frequencies[c] = 1;
                }
            }

            return frequencies;
        }

        private double ChiSquaredScore(string text, string language)
        {
            var expectedFrequencies = GetExpectedFrequencies(language);
            var observedFrequencies = GetObservedFrequencies(text, language);

            double chiSquared = 0;
            foreach (var key in expectedFrequencies.Keys)
            {
                double expected = expectedFrequencies[key];
                double observed = observedFrequencies.ContainsKey(key) ? observedFrequencies[key] : 0;
                chiSquared += Math.Pow(observed - expected, 2) / expected;
            }

            return chiSquared;
        }

        private (int? A, int B, int C)? ExhaustiveKeySearch(string plainText, string encryptedText, char[] alphabet)
        {
            int mod = alphabet.Length;
            
            // Try linear keys
            for (int a = 0; a < mod; a++)
            {
                for (int b = 0; b < mod; b++)
                {
                    var cipher = new TrithemiusCipher(alphabet);
                    var key = new TrithemiusCipher.LinKey 
                    { 
                        A = a, 
                        B = b, 
                        keyType = TrithemiusCipher.Key.KeyType.LinearAquestion 
                    };
                    
                    var testEncryptedText = cipher.Trithemius(plainText, TrithemiusCipher.Operation.Encrypt, key);
                    if (testEncryptedText == encryptedText)
                        return (null, a, b);
                }
            }

            // Try non-linear keys
            for (int a = 0; a < mod; a++)
            {
                for (int b = 0; b < mod; b++)
                {
                    for (int c = 0; c < mod; c++)
                    {
                        var cipher = new TrithemiusCipher(alphabet);
                        var key = new TrithemiusCipher.NonLinKey 
                        { 
                            A = a, 
                            B = b, 
                            C = c, 
                            keyType = TrithemiusCipher.Key.KeyType.NonLinearAquestion 
                        };
                        
                        var testEncryptedText = cipher.Trithemius(plainText, TrithemiusCipher.Operation.Encrypt, key);
                        if (testEncryptedText == encryptedText)
                            return (a, b, c);
                    }
                }
            }

            return null;
        }

        private (int A, int B)? FindLinearKey(string plainText, string encryptedText, char[] alphabet)
        {
            int x1 = 0, x2 = 1;
            int y1 = GetCharIndex(encryptedText[0], alphabet);
            int y2 = GetCharIndex(encryptedText[1], alphabet);
            int p1 = GetCharIndex(plainText[0], alphabet);
            int p2 = GetCharIndex(plainText[1], alphabet);

            if (p1 == -1 || p2 == -1 || y1 == -1 || y2 == -1)
                return null;

            int mod = alphabet.Length;
            int deltaX = (x2 - x1) % mod;
            if (deltaX == 0) return null;

            int A = (y2 - y1) * ModInverse(deltaX, mod) % mod;
            if (A < 0) A += mod;

            int B = (y1 - (p1 + A * x1) % mod) % mod;
            if (B < 0) B += mod;

            return (A, B);
        }

        private (int A, int B, int C)? FindNonLinearKey(string plainText, string encryptedText, char[] alphabet)
        {
            int x1 = 0, x2 = 1, x3 = 2;
            int y1 = GetCharIndex(encryptedText[0], alphabet);
            int y2 = GetCharIndex(encryptedText[1], alphabet);
            int y3 = GetCharIndex(encryptedText[2], alphabet);
            int p1 = GetCharIndex(plainText[0], alphabet);
            int p2 = GetCharIndex(plainText[1], alphabet);
            int p3 = GetCharIndex(plainText[2], alphabet);

            if (p1 == -1 || p2 == -1 || p3 == -1 || y1 == -1 || y2 == -1 || y3 == -1)
                return null;

            int mod = alphabet.Length;

            // Розв'язок системи трьох рівнянь для A, B, C
            int a1 = x1 * x1, b1 = x1, c1 = 1;
            int a2 = x2 * x2, b2 = x2, c2 = 1;
            int a3 = x3 * x3, b3 = x3, c3 = 1;

            int det = a1 * (b2 * c3 - b3 * c2) - b1 * (a2 * c3 - a3 * c2) + c1 * (a2 * b3 - a3 * b2);
            if (det == 0) return null;

            int detA = y1 * (b2 * c3 - b3 * c2) - b1 * (y2 * c3 - y3 * c2) + c1 * (y2 * b3 - y3 * b2);
            int detB = a1 * (y2 * c3 - y3 * c2) - y1 * (a2 * c3 - a3 * c2) + c1 * (a2 * y3 - a3 * y2);
            int detC = a1 * (b2 * y3 - b3 * y2) - b1 * (a2 * y3 - a3 * y2) + y1 * (a2 * b3 - a3 * b2);

            int A = detA * ModInverse(det, mod) % mod;
            int B = detB * ModInverse(det, mod) % mod;
            int C = detC * ModInverse(det, mod) % mod;

            if (A < 0) A += mod;
            if (B < 0) B += mod;
            if (C < 0) C += mod;

            return (A, B, C);
        }

        private int GetCharIndex(char c, char[] alphabet)
        {
            int index = Array.IndexOf(alphabet, c);
            return index >= 0 ? index : -1;
        }

        private int ModInverse(int a, int m)
        {
            a = (a % m + m) % m;
            for (int x = 1; x < m; x++)
                if ((a * x) % m == 1) return x;
            return -1;
        }
    }

    public class TrithemiusCipher
    {
        private Dictionary<char, int> dict;
        private Dictionary<int, char> reverseDict;

        public TrithemiusCipher(char[] alphabet)
        {
            dict = new Dictionary<char, int>();
            reverseDict = new Dictionary<int, char>();
            for (int i = 0; i < alphabet.Length; i++)
            {
                dict[alphabet[i]] = i;
                reverseDict[i] = alphabet[i];
            }
        }

        public enum Operation { Encrypt, Decrypt }

        public class Key
        {
            public enum KeyType { LinearAquestion, NonLinearAquestion, Gaslo }
            public KeyType keyType { get; set; }
        }

        public class LinKey : Key
        {
            public int A { get; set; }
            public int B { get; set; }
        }

        public class NonLinKey : LinKey
        {
            public int C { get; set; }
        }

        public class Gaslo : Key
        {
            public required string gaslo { get; set; }
        }

        public string Trithemius(string input, Operation op, object operationArgs)
        {
            string result = string.Empty;

            int posLetterInMsg = 0;
            foreach (char letter in input)
            {
                // Skip characters not in the dictionary
                if (!dict.ContainsKey(letter))
                {
                    result += letter; // Append the character as-is
                    posLetterInMsg++; // Increment position for key calculation
                    continue;
                }

                int num, key = 0;

                switch (((Key)operationArgs).keyType)
                {
                    case Key.KeyType.LinearAquestion:
                        key = ((LinKey)operationArgs).A * posLetterInMsg + ((LinKey)operationArgs).B;
                        break;
                    case Key.KeyType.NonLinearAquestion:
                        key = ((NonLinKey)operationArgs).A * posLetterInMsg * posLetterInMsg + ((NonLinKey)operationArgs).B * posLetterInMsg + ((NonLinKey)operationArgs).C;
                        break;
                    case Key.KeyType.Gaslo:
                        key = dict[((Gaslo)operationArgs).gaslo[posLetterInMsg]];
                        if (posLetterInMsg % (((Gaslo)operationArgs).gaslo.Length + 1) == 0)
                            posLetterInMsg = -1;
                        break;
                }

                if (op == Operation.Encrypt)
                {
                    num = (dict[letter] + key) % dict.Count;
                }
                else
                {
                    num = (dict[letter] + dict.Count - (key % dict.Count)) % dict.Count;
                }

                // Ensure num is non-negative
                if (num < 0)
                {
                    num += dict.Count;
                }

                // Finding in dict letter num
                result += reverseDict[num];
                posLetterInMsg++;
            }

            return result;
        }
    }
}