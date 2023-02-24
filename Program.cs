using System.Security.Cryptography;
using App.Middlewares;
using App.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<RsaSecurityKey>(provider =>
{
    RSA rsa = RSA.Create();

    rsa.ImportRSAPrivateKey(
        source: Convert.FromBase64String(builder.Configuration["Jwt:PrivateKey"]),
        bytesRead: out int _
    );

    return new RsaSecurityKey(rsa);
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            SecurityKey rsa = builder.Services.BuildServiceProvider().GetRequiredService<RsaSecurityKey>();
            options.RequireHttpsMetadata = true;
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ClockSkew = TimeSpan.Zero,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "http://localhost:5117",
                ValidAudience = "http://localhost:5117",
                IssuerSigningKey = rsa,
            };
        }
    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<AuthorizationMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();

// get -h "Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6InN0bmciLCJqdGkiOiIwYjVlOWQyOTA5NGU0Yjc5OGY0NjI1ODNiNWM1YWU5ZCIsInJvbGUiOlsiQWRtaW4iLCJQYXllciJdLCJleHAiOjE2NzcyMjA1OTAsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTExNyIsImF1ZCI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTExNyJ9.BNdLGI-S_kSUC2WGro-33Vgau2o6wxD3yWyzruljRV1iETWXTgKkSIXyJWSOxRkI8ynp2_HZo2047ZKuJJWzcMzBS9Tr9RXT1FLZsu0WeAfMAyT2k74K1Z4iHuLBcs1VVQkt9d79aTfWxTgORhUbsolVfz7VwF6PjV7ixp55iVKgT66e9JZBpu49dTWDkwIXM_YZhx6cnkiXglA1WU76EB60J64n8UK49XcETMTMUWzXGp3jzI7-RGv0rT_AiSzSMeCpXZxr_t6dXR9vtvl0oDxABPEHd0PYa1UzZGpbbOaUZ8sANFvid2qgncS6I1MNfZtHD311Zd5Yl0tq0C2aqQ"