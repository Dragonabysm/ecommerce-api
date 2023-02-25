using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Security.Claims;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private IConfiguration _config;
    private RSA rsa;

    private readonly NpgsqlConnection _connection;

    public AuthController(IConfiguration configuration)
    {
        _config = configuration;
        _connection = new NpgsqlConnection(_config["Database:ConnectionString"]);

        rsa = RSA.Create();

        rsa.ImportRSAPrivateKey(
            source: Convert.FromBase64String(_config["Jwt:PrivateKey"]),
            bytesRead: out int _
        );
    }

    /// <summary>
    /// Login a user and returns he JWT if the login is Ok
    /// </summary>
    /// <returns> Returns the user JWT </returns>
    /// <response code="200">You're authenticated, take the JWT</response>
    /// <response code="401">Your credentials isn't valid</response>
    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login(RegistrationRequest user)
    {
        string jti = Guid.NewGuid().ToString("N");
        string salt;
        string password;

        var userForDb = new User
        {
            Email = user.Email,
            Password = user.Password,
            Username = user.Username
        };

        await _connection.OpenAsync();

        var queryForUserData = new NpgsqlCommand(
            "SELECT salt, password FROM users WHERE email = @email AND username = @username",
            _connection
        )
        {
            Parameters = { new("email", userForDb.Email), new("username", userForDb.Username), }
        };
        var reader = await queryForUserData.ExecuteReaderAsync();
        if (!(await reader.ReadAsync()))
            return Unauthorized();

        salt = reader.GetString(0);
        password = reader.GetString(1);

        await queryForUserData.DisposeAsync();
        await reader.CloseAsync();

        await userForDb.HashPassword(salt);

        if (userForDb.Password == password)
        {
            var queryForUpdate = new NpgsqlCommand(
                "UPDATE users SET latest_jti = @jti WHERE email = @email",
                _connection
            )
            {
                Parameters = { new("email", userForDb.Email), new("jti", jti), }
            };

            await queryForUpdate.ExecuteNonQueryAsync();
            await queryForUpdate.DisposeAsync();
            await _connection.CloseAsync();

            var credentials = new SigningCredentials(
                new RsaSecurityKey(rsa),
                SecurityAlgorithms.RsaSha256
            )
            {
                CryptoProviderFactory = new CryptoProviderFactory
                {
                    CacheSignatureProviders = false
                }
            };

            var claims = new[] { new Claim("email", user.Email), new Claim("jti", jti), };

            var jwt = new JwtSecurityToken(
                "http://localhost:5117",
                "http://localhost:5117",
                claims,
                expires: DateTime.Now.AddHours(10),
                signingCredentials: credentials
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return Ok(token);
        }
        return Unauthorized();
    }

    /// <summary>
    /// Register a user and returns he JWT
    /// </summary>
    /// <returns> Returns the user JWT </returns>
    /// <response code="200">You're registered</response>
    /// <response code="409">Already have this email registered in a user</response>
    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register(RegistrationRequest user)
    {
        string jti = Guid.NewGuid().ToString("N");
        var userForDb = new User
        {
            Email = user.Email,
            LatestJWTId = jti,
            Password = user.Password,
            Username = user.Username
        };
        await userForDb.HashPassword();

        await _connection.OpenAsync();

        try
        {
            var createUser = new NpgsqlCommand(
                "INSERT INTO users (username, email, password, salt, latest_jti) VALUES (@username, @email, @password, @salt, @latestjwtid)",
                _connection
            )
            {
                Parameters =
                {
                    new("username", userForDb.Username),
                    new("email", userForDb.Email),
                    new("password", userForDb.Password),
                    new("salt", userForDb.Salt),
                    new("latestjwtid", userForDb.LatestJWTId),
                }
            };

            await createUser.ExecuteNonQueryAsync();
        }
        catch (NpgsqlException)
        {
            // Account already exists
            return Conflict("Already exists a registered user with your email.");
        }

        await _connection.CloseAsync();

        var credentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256
        )
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };

        var claims = new[] { new Claim("email", user.Email), new Claim("jti", jti) };

        var jwt = new JwtSecurityToken(
            "http://localhost:5117",
            "http://localhost:5117",
            claims,
            expires: DateTime.Now.AddHours(10),
            signingCredentials: credentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return Ok(token);
    }
}
