using System.ComponentModel.DataAnnotations;

namespace App.Models;

public class RegistrationRequest
{

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
