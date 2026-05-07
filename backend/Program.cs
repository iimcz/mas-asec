using asec.Configuration;
using asec.Emulation;
using asec.Extensions;
using asec.Models;
using Microsoft.EntityFrameworkCore;
using Minio.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add additional configuration files
//builder.Configuration.AddJsonFile("emulators.json", false);

// Add services to the container.
builder.Services.AddDbContext<AsecDBContext>(b =>
{
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
builder.Services.AddKeyedMinio("LocalObjectStorage", options =>
{
    var section = builder.Configuration.GetSection("ObjectStorage");
    options.Endpoint = section.GetValue<string>("Endpoint") ?? "";
    options.AccessKey = section.GetValue<string>("AccessKey") ?? "";
    options.SecretKey = section.GetValue<string>("SecretKey") ?? "";
    options.Region = section.GetValue<string>("Region") ?? "";
    options.SessionToken = section.GetValue<string>("SessionToken") ?? "";
});
builder.Services.AddKeyedMinio("ArchiveObjectStorage", options =>
{
    var section = builder.Configuration.GetSection("");
    options.Endpoint = section.GetValue<string>("Endpoint") ?? "";
    options.AccessKey = section.GetValue<string>("AccessKey") ?? "";
    options.SecretKey = section.GetValue<string>("SecretKey") ?? "";
    options.Region = section.GetValue<string>("Region") ?? "";
    options.SessionToken = section.GetValue<string>("SessionToken") ?? "";
});
builder.Services.AddHttpClient();
builder.Services.AddAsecServices();
builder.Services.AddControllers()
    .AddJsonOptions(opt => opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<DigitalizationToolsOptionsSetup>();
builder.Services.ConfigureOptions<EmulatorOptionsSetup>();

var app = builder.Build();

// First do migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AsecDBContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(config =>
{
    config
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin();
});
//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.LoadPlatforms();
app.Run();
