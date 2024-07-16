using asec.Configuration;
using asec.Extensions;
using asec.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add additional configuration files
//builder.Configuration.AddJsonFile("emulators.json", false);

// Add services to the container.
builder.Services.AddDbContext<AsecDBContext>(b => {
    var section = builder.Configuration.GetSection("Database");
    var useSqlite = section.GetValue<bool>("UseSqlite");
    var connectionString = section.GetValue<string>("ConnectionString");
    if (useSqlite)
    {
        b.UseSqlite(connectionString);
    }
    else
    {
        b.UseSqlServer(connectionString);
    }
});
builder.Services.AddCors();
builder.Services.AddAsecServices();
builder.Services.AddControllers()
    .AddJsonOptions(opt => opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<DigitalizationToolsOptionsSetup>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(config => {
    config
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin();
});
//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
