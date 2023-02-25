using System.ComponentModel.DataAnnotations;

namespace App.Models;

public class ProductInfo
{    
    [Required]
    public long id { get; set; }

    [Required]
    public string? authorUsername { get; set; }

    [Required]
    public string name { get; set; } = null!;

    [Required]
    public double price { get; set; }

    [Required]
    public string description { get; set; } = "";
}
