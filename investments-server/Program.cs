using InvestmentsServer.Services;
using InvestmentsServer.Data;
using InvestmentsServer.Hubs; // NEW: SignalR Hub
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// NEW: Add SignalR
builder.Services.AddSignalR();

// Configure Database
// Using SQLite for simplicity
builder.Services.AddDbContextFactory<InvestmentDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("SqliteConnection") 
        ?? "Data Source=investments.db"
    )
);

// NEW: Register the Investment Queue as Singleton
// Singleton ensures the same queue instance is used across the app
builder.Services.AddSingleton<InvestmentQueue>();

// Register Services
builder.Services.AddScoped<InvestmentService>();

// NEW: Register BOTH background services
builder.Services.AddHostedService<InvestmentQueueWorker>();      // Processes queue
builder.Services.AddHostedService<InvestmentBackgroundService>(); // Checks completions

// Configure CORS - UPDATED to allow SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<InvestmentDbContext>>();
    await using var context = await contextFactory.CreateDbContextAsync();
    await context.Database.MigrateAsync();
    Console.WriteLine("✔ Database migrations applied successfully");
}

// IMPORTANT: CORS must be before MapHub
app.UseCors("AllowFrontend");

app.UseAuthorization();

// Map REST API Controllers
app.MapControllers();

// NEW: Map SignalR Hub
app.MapHub<InvestmentHub>("/investmentHub");

Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("   Backend is UP and RUNNING!");
Console.WriteLine("   API URL: http://localhost:5000");
Console.WriteLine("   SignalR Hub: http://localhost:5000/investmentHub");
Console.WriteLine("   Database: SQLite (investments.db)");
Console.WriteLine("   Event-Driven: ENABLED");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

app.Run();