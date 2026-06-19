using System.Text.Json.Serialization;
using Application;
using Infrastructure;
using Infrastructure.Persistence;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MetroFlow AI Backend API",
        Version = "v1",
        Description = "Backend demo para estaciones, rutas, alertas, grafos, recomendaciones operativas y busqueda vectorial con ChromaDB."
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("DemoCors", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    // Future locked policy:
    // options.AddPolicy("Frontend", p => p.WithOrigins("http://localhost:5173", "https://metroflow-demo.vercel.app").AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MetroFlow AI Backend API v1");
});
app.UseCors("DemoCors");
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "MetroFlow AI Backend", utc = DateTime.UtcNow }));

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<MetroFlowDbContext>();
        await db.Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<MetroFlowSeeder>().SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database seed skipped. Check PostgreSQL connection settings to enable persistence.");
    }
}

app.Run();
