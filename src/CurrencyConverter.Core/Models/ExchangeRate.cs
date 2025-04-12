namespace CurrencyConverter.Core.Models;

public class ExchangeRate
{
    public required string Base { get; set; }
    public DateTime Date { get; set; }
    public Dictionary<string, decimal> Rates { get; set; } = new();
}