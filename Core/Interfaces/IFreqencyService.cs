namespace Core.Interfaces;

public interface IFrequencyService
{
    public Dictionary<char, Dictionary<string, double>> GetFrequency(string text);
}