using InvestmentsServer.Services;
using InvestmentsServer.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// Configure Database
// Option 1: SQL Server (recommended for production)
/*
builder.Services.AddDbContextFactory<InvestmentDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);
*/

// Option 2: SQLite (simpler for development/testing)
builder.Services.AddDbContextFactory<InvestmentDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("SqliteConnection") 
        ?? "Data Source=investments.db"
    )
);

// Register Services
builder.Services.AddScoped<InvestmentService>();
builder.Services.AddHostedService<InvestmentBackgroundService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<InvestmentDbContext>>();
    await using var context = await contextFactory.CreateDbContextAsync();
    await context.Database.MigrateAsync();
    Console.WriteLine("✓ Database migrations applied successfully");
}

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("-------------------------------------------");
Console.WriteLine("   Backend is UP and RUNNING!");
Console.WriteLine("   URL: http://localhost:5000");
Console.WriteLine("   Database: SQLite (investments.db)");
Console.WriteLine("-------------------------------------------");

app.Run();