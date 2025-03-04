namespace Core.Entities;

public class FrequencyAttackResult
{
    public int Offset { get; set; }
    public int Direction { get; set; }
    public int Step { get; set; }
    public double ChiSquaredScore { get; set; }
    public string? DecipheredText { get; set; }
}