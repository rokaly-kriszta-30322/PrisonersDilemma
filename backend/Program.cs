using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddDbContext<MyDbContext>(options =>
{
    var provider = builder.Configuration["DatabaseProvider"];

    if (string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
    {
        var cs = builder.Configuration.GetConnectionString("Postgres");

        if (string.IsNullOrWhiteSpace(cs))
            throw new Exception("ConnectionStrings:Postgres is missing");

        // Render gives a URL like: postgresql://user:pass@host/db
        // Npgsql expects: Host=...;Port=...;Database=...;Username=...;Password=...
        if (cs.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(cs);
            var userInfo = uri.UserInfo.Split(':', 2);

            var host = uri.Host;
            var port = uri.IsDefaultPort ? 5432 : uri.Port;
            var database = uri.AbsolutePath.TrimStart('/');

            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";

            cs =
                $"Host={host};" +
                $"Port={port};" +
                $"Database={database};" +
                $"Username={username};" +
                $"Password={password};" +
                $"SSL Mode=Require;" +
                $"Trust Server Certificate=true";
        }

        options.UseNpgsql(cs);
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

builder.Services.AddScoped<AccessTokenGenerator>();
builder.Services.AddScoped<IPasswordHasher, BCryptHasher>();
builder.Services.AddSingleton<ActiveUsers>();
builder.Services.AddScoped<GameLogic>();
builder.Services.AddScoped<GameOver>();
builder.Services.AddScoped<MatrixHandler>();
builder.Services.AddSingleton<IBotStrategyManager, BotStrategyManager>();
builder.Services.AddHostedService<BotInitiationService>();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Authentification:Issuer"],
        ValidAudience = builder.Configuration["Authentification:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Authentification:AccessTokenSecretKey"]!)
        )
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/gamehub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // Only run migrations when explicitly enabled (Render)
    if (string.Equals(cfg["RUN_MIGRATIONS"], "true", StringComparison.OrdinalIgnoreCase))
    {
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        db.Database.Migrate();
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/gamehub");

app.Run();
