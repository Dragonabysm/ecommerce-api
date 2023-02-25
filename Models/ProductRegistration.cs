using System.ComponentModel.DataAnnotations;

namespace App.Models;

public class ProductRegistration
{
  [Required]
    public string name { get; set; } = null!;

    [Required]
    public double price { get; set; }

    [Required]
    public string description { get; set; } = "";
}