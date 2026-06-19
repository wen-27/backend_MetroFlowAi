using Application;
using Infrastructure;
using Infrastructure.Persistence;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DemoCors", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    // Future locked policy:
    // options.AddPolicy("Frontend", p => p.WithOrigins("http://localhost:5173", "https://metroflow-demo.vercel.app").AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("DemoCors");
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "MetroFlow AI Backend", utc = DateTime.UtcNow }));

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<MetroFlowDbContext>();
        await db.Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<MetroFlowSeeder>().SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database seed skipped. Start MySQL with docker compose to enable persistence.");
    }
}

app.Run();
