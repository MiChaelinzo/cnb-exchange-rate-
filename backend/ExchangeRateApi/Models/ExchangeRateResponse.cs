using System.ComponentModel.DataAnnotations;

namespace ExchangeRateApi.Models;

public class ExchangeRateResponse
{
    [Required]
    public DateTime Date { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Sequence number must be greater than 0")]
    public int SequenceNumber { get; set; }

    [Required]
    public List<ExchangeRate> Rates { get; set; } = new List<ExchangeRate>();
}