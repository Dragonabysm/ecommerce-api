using System.ComponentModel.DataAnnotations;
using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace App.Models;

public class User
{

    public async Task HashPassword()
    {
        var hasher = new Argon2id(Encoding.UTF8.GetBytes(this.Password));
        hasher.Salt = RandomNumberGenerator.GetBytes(60);
        hasher.DegreeOfParallelism = 12;
        hasher.Iterations = 3;
        hasher.MemorySize = 1024 * 280;
        this.Salt = Convert.ToHexString(hasher.Salt);
        this.Password = Convert.ToHexString(await hasher.GetBytesAsync(50));
    }

    public async Task HashPassword(string salt)
    {
        var hasher = new Argon2id(Encoding.UTF8.GetBytes(this.Password));
        hasher.Salt = Convert.FromHexString(salt);
        hasher.DegreeOfParallelism = 12;
        hasher.Iterations = 3;
        hasher.MemorySize = 1024 * 280;
        this.Salt = salt;
        this.Password = Convert.ToHexString(await hasher.GetBytesAsync(50));
    }

    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(80)]
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    [Required]
    public string Salt { get; set; } = null!;

    // c7d74acc9e45408f846414f046151235
    [RegularExpression("[a-z0-9]{32}")]
    public string LatestJWTId { get; set; } = null!;
}
