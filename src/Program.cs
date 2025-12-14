using Microsoft.EntityFrameworkCore;
using taskedin_be.src.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured. Please check your appsettings.json file.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
);

builder.Services.AddScoped<taskedin_be.src.Modules.Users.Services.UserService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
Console.WriteLine($"ENV = {builder.Environment.EnvironmentName}");
Console.WriteLine($"CS  = {builder.Configuration.GetConnectionString("DefaultConnection")}");

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
