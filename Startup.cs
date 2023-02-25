using System.Reflection;
using System.Security.Cryptography;
using App.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

public class Startup
{
    public IConfiguration configRoot { get; }

    public Startup(IConfiguration configuration)
    {
        configRoot = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);

            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Version = "v1",
                    Contact = new OpenApiContact
                    {
                        Email = "dinishigor@gmail.com",
                        Name = "Higor Dinis",
                        Url = new Uri("https://github.com/dragonabysm")
                    },
                    Description = "A ecommerce basic API with authentication.",
                    Title = "Ecommerce Api"
                }
            );

            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Get a JWT by Login or Register endpoints and paste this here"
                }
            );

            options.IgnoreObsoleteActions();
            
            options.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                }
            );
        });

        services.AddSingleton<RsaSecurityKey>(provider =>
        {
            RSA rsa = RSA.Create();

            rsa.ImportRSAPrivateKey(
                source: Convert.FromBase64String(configRoot["Jwt:PrivateKey"]),
                bytesRead: out int _
            );

            return new RsaSecurityKey(rsa);
        });

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    SecurityKey rsa = services
                        .BuildServiceProvider()
                        .GetRequiredService<RsaSecurityKey>();
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
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
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
    }
}
