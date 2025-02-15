using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Services
{
    public partial class FrequencyService : IFrequencyService
    {
        public Dictionary<char, Dictionary<string, double>> GetFrequency(string text)
        {
            // Filter out whitespace and punctuation
            var filteredContent = text
                .Where(c => !char.IsWhiteSpace(c) && !char.IsPunctuation(c))
                .ToList();

            int totalCharacters = filteredContent.Count;
            if (totalCharacters == 0)
            {
                throw new Exception("No valid characters found in text.");
            }

            // Calculate frequency
            var frequencyTable = filteredContent
                .GroupBy(c => c)
                .Select(g => new
                {
                    Character = g.Key,
                    Count = g.Count(),
                    Percentage = Math.Round(g.Count() / (double)totalCharacters * 100, 6)
                })
                .OrderByDescending(g => g.Count)
                .ToDictionary(
                    g => g.Character,
                    g => new Dictionary<string, double>
                    {
                        { "Count", g.Count },
                        { "Percentage", g.Percentage }
                    }
                );

            return frequencyTable;
        }
    }
}