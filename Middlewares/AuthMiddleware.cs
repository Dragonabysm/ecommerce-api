using System.Security.Claims;

namespace App.Middlewares;

public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;

    public AuthorizationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _config = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            Claim? jti = context.User.Claims.FirstOrDefault(x => x.Type == "jti");
            Claim? email = context.User.Claims.FirstOrDefault(
                x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
            );

            if (jti != null && email != null)
            {
                var connection = new Npgsql.NpgsqlConnection(_config["Database:ConnectionString"]);
                await connection.OpenAsync();

                using (
                    var query = new Npgsql.NpgsqlCommand(
                        "SELECT id FROM users WHERE email = @email AND latest_jti = @jti",
                        connection
                    )
                    {
                        Parameters = { new("email", email.Value), new("jti", jti.Value) }
                    }
                )
                {
                    var queryResult = await query.ExecuteReaderAsync();
                    if (!(await queryResult.ReadAsync()))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.CompleteAsync();
                    }
                    await queryResult.CloseAsync();
                }
                await connection.CloseAsync();
            }
        }
        await _next(context);
    }
}
