using Core.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services
{
    public class XORCipherService : IXORCipherService
    {
        private readonly Dictionary<string, char[]> _alphabets = new()
        {
            { "English", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ,.!?:;()-'\"".ToCharArray() },
            { "Ukrainian", "абвгґдеєжзиіїйклмнопрстуфхцчшщьюяАБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ ,.!?:;()-'\"".ToCharArray() }
        };

        public Task<string> EncryptAsync(string text, string gamma, string language)
        {
            // Check if using one-time pad mode
            if (gamma.StartsWith("OTP:"))
            {
                return Task.FromResult(ProcessOneTimePad(text, gamma.Substring(4), language));
            }
            
            // Standard XOR with repeated gamma
            return Task.FromResult(ProcessXOR(text, gamma, language));
        }

        public Task<string> DecryptAsync(string text, string gamma, string language)
        {
            // Check if using one-time pad mode
            if (gamma.StartsWith("OTP:"))
            {
                return Task.FromResult(ProcessOneTimePad(text, gamma.Substring(4), language));
            }
            
            // Standard XOR with repeated gamma
            return Task.FromResult(ProcessXOR(text, gamma, language));
        }

        private string ProcessXOR(string text, string gamma, string language)
        {
            if (!_alphabets.ContainsKey(language))
                throw new ArgumentException($"Unsupported language: {language}");

            var alphabet = _alphabets[language];
            var result = new StringBuilder();

            // Extending gamma to match text length
            string extendedGamma = ExtendGamma(gamma, text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                int textCharIndex = Array.IndexOf(alphabet, text[i]);
                int gammaCharIndex = Array.IndexOf(alphabet, extendedGamma[i]);
                
                // Skip characters not in the alphabet
                if (textCharIndex == -1)
                {
                    result.Append(text[i]);
                    continue;
                }

                if (gammaCharIndex == -1)
                    throw new ArgumentException($"Gamma contains invalid character '{extendedGamma[i]}' for the selected language.");

                // XOR operation
                int resultIndex = (textCharIndex ^ gammaCharIndex) % alphabet.Length;
                result.Append(alphabet[resultIndex]);
            }

            return result.ToString();
        }

        // New method for one-time pad (Vernam cipher)
        private string ProcessOneTimePad(string text, string pad, string language)
        {
            if (!_alphabets.ContainsKey(language))
                throw new ArgumentException($"Unsupported language: {language}");

            var alphabet = _alphabets[language];
            var result = new StringBuilder();

            // Verify pad length
            if (pad.Length < text.Length)
                throw new ArgumentException("One-time pad must be at least as long as the message");

            for (int i = 0; i < text.Length; i++)
            {
                int textCharIndex = Array.IndexOf(alphabet, text[i]);
                int padCharIndex = Array.IndexOf(alphabet, pad[i]);
                
                // Skip characters not in the alphabet
                if (textCharIndex == -1)
                {
                    result.Append(text[i]);
                    continue;
                }

                if (padCharIndex == -1)
                    throw new ArgumentException($"One-time pad contains invalid character '{pad[i]}' for the selected language.");

                // XOR operation
                int resultIndex = (textCharIndex ^ padCharIndex) % alphabet.Length;
                result.Append(alphabet[resultIndex]);
            }

            return result.ToString();
        }

        // Method to extend gamma to required length (for standard XOR)
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

        // New method to generate a random one-time pad
        public string GenerateOneTimePad(int length, string language)
        {
            if (!_alphabets.ContainsKey(language))
                throw new ArgumentException($"Unsupported language: {language}`");

            var alphabet = _alphabets[language];
            var result = new StringBuilder(length);
            
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[length];
                rng.GetBytes(bytes);
                
                for (int i = 0; i < length; i++)
                {
                    int index = bytes[i] % alphabet.Length;
                    result.Append(alphabet[index]);
                }
            }
            
            return result.ToString();
        }
    }
}