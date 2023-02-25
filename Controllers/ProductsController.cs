using System.Security.Claims;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

[ApiController]
[Route("/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private IConfiguration _config;
    private readonly NpgsqlConnection _connection;

    public ProductsController(IConfiguration configuration)
    {
        _config = configuration;
        _connection = new NpgsqlConnection(_config["Database:ConnectionString"]);
    }

    /// <summary>
    /// Get all products in database
    /// </summary>
    /// <returns> Returns all products in database </returns>
    /// <response code="200">Ok</response>
    /// <response code="401">You're not authenticated</response>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        var products = new List<ProductInfo> { };
        await _connection.OpenAsync();
        var query = new NpgsqlCommand(
            @"
        SELECT Products.name, Products.description, Products.price, Products.id, Users.username 
        FROM Products 
        INNER JOIN Users ON Products.user_id=Users.id",
            _connection
        );

        var reader = await query.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            string productName = reader.GetString(0);
            string productDescription = reader.GetString(1);
            double productPrice = reader.GetDouble(2);
            long productId = reader.GetInt64(3);
            string authorUsername = reader.GetString(4);

            var product = new ProductInfo()
            {
                id = productId,
                authorUsername = authorUsername,
                description = productDescription,
                name = productName,
                price = productPrice
            };
            products.Add(product);
        }

        await _connection.CloseAsync();
        await query.DisposeAsync();
        await reader.CloseAsync();

        return Ok(products);
    }

    /// <summary>
    /// Create a product
    /// </summary>
    /// <returns> Returns the created product </returns>
    /// <response code="201"> The product was created </response>
    /// <response code="401"> You're not authenticated </response>
    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> CreateProduct(ProductRegistration product)
    {
        string? userEmail = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        await _connection.OpenAsync();
        var queryGetId = new NpgsqlCommand("SELECT id FROM users WHERE email = @email", _connection)
        {
            Parameters = { new("email", userEmail) }
        };

        var reader = await queryGetId.ExecuteReaderAsync();

        Console.WriteLine(await reader.ReadAsync());

        long id = reader.GetInt64(0);

        await reader.CloseAsync();
        await queryGetId.DisposeAsync();

        var productForDb = new Product()
        {
            authorId = id,
            description = product.description,
            name = product.name,
            price = product.price
        };

        var queryInsertProduct = new NpgsqlCommand(
            "INSERT INTO products (user_id, name, description, image_url, price) VALUES (@id, @name, @description, @image_url, @price)",
            _connection
        )
        {
            Parameters =
            {
                new("id", id),
                new("name", productForDb.name),
                new("description", productForDb.description),
                new("image_url", new List<string> { }),
                new("price", productForDb.price)
            }
        };

        await queryInsertProduct.ExecuteNonQueryAsync();
        await queryInsertProduct.DisposeAsync();
        await _connection.CloseAsync();

        return Created("", productForDb);
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    /// <returns> Without return </returns>
    /// <response code="200"> The product was deleted </response>
    /// <response code="401"> You're not authenticated </response>
    [HttpPost("delete")]
    [Authorize]
    public async Task<IActionResult> DeleteProduct(ProductInfo product)
    {
        string? userEmail = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        await _connection.OpenAsync();
        var queryGetUserId = new NpgsqlCommand(
            "SELECT id FROM users WHERE email = @email",
            _connection
        )
        {
            Parameters = { new("email", userEmail) },
        };

        var reader = await queryGetUserId.ExecuteReaderAsync();
        if (!(await reader.ReadAsync()))
            return Unauthorized();

        long userId = reader.GetInt64(0);

        await queryGetUserId.DisposeAsync();
        await reader.CloseAsync();

        var queryGetUserIdOfProduct = new NpgsqlCommand(
            "SELECT user_id FROM products WHERE id = @id",
            _connection
        )
        {
            Parameters = { new("id", product.id) },
        };

        reader = await queryGetUserIdOfProduct.ExecuteReaderAsync();

        if (!(await reader.ReadAsync()))
            return Unauthorized();

        long productUserId = reader.GetInt16(0);

        await queryGetUserIdOfProduct.DisposeAsync();
        await reader.CloseAsync();

        if (!(userId == productUserId))
            return Unauthorized();

        var queryDeleteProduct = new NpgsqlCommand(
            "DELETE FROM products WHERE id = @id",
            _connection
        )
        {
            Parameters = { new("id", product.id) }
        };

        await queryDeleteProduct.ExecuteNonQueryAsync();
        await queryDeleteProduct.DisposeAsync();

        await _connection.CloseAsync();

        return Ok();
    }
}
