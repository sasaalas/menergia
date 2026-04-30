using Microsoft.Extensions.Configuration;
using menergia.api.Services;
using menergiabase.Services;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Get credentials from configuration
var username = builder.Configuration["MinunEnergia:Username"] 
    ?? throw new InvalidOperationException("Username not configured");
var password = builder.Configuration["MinunEnergia:Password"] 
    ?? throw new InvalidOperationException("Password not configured");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MinunEnergia services
builder.Services.AddSingleton(new MinunEnergiaLoginService(username, password));
builder.Services.AddSingleton<ConsumptionService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize consumption service
var consumptionService = app.Services.GetRequiredService<ConsumptionService>();
await consumptionService.InitializeAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();

