using System.ComponentModel.DataAnnotations;

namespace ExchangeRateApi.Models;

public class ExchangeRate
{
    [Required]
    public string Country { get; set; } = string.Empty;

    [Required]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public int Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 characters")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Rate must be greater than 0")]
    public decimal Rate { get; set; }
}