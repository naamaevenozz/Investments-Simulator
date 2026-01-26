using InvestmentsServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<InvestmentService>();
builder.Services.AddHostedService<InvestmentBackgroundService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("-------------------------------------------");
Console.WriteLine("   Backend is UP and RUNNING!");
Console.WriteLine("   URL: http://localhost:5000");
Console.WriteLine("-------------------------------------------");

app.Run();