using asec.Configuration;
using asec.Extensions;
using asec.Models;
using Microsoft.EntityFrameworkCore;
using Minio;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

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
    var section = builder.Configuration.GetSection("LocalObjectStorage");
    options.Endpoint = section.GetValue<string>("Endpoint") ?? "";
    options.AccessKey = section.GetValue<string>("AccessKey") ?? "";
    options.SecretKey = section.GetValue<string>("SecretKey") ?? "";
    options.Region = section.GetValue<string>("Region") ?? "";
    options.SessionToken = section.GetValue<string>("SessionToken") ?? "";
});
builder.Services.AddKeyedMinio("ArchiveObjectStorage", options =>
{
    var section = builder.Configuration.GetSection("ArchiveObjectStorage");
    options.Endpoint = section.GetValue<string>("Endpoint") ?? "";
    options.AccessKey = section.GetValue<string>("AccessKey") ?? "";
    options.SecretKey = section.GetValue<string>("SecretKey") ?? "";
    options.Region = section.GetValue<string>("Region") ?? "";
    options.SessionToken = section.GetValue<string>("SessionToken") ?? "";
    options.ConfigureClient(client =>
    {
        client.WithSSL();
    });
});
builder.Services.AddHttpClient();
builder.Services.AddAsecServices();
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(new PascalCaseNamingPolicy()));
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureOptions<DigitalizationToolsOptionsSetup>();
builder.Services.ConfigureOptions<EmulatorOptionsSetup>();
builder.Services.AddOptionsWithValidateOnStart<LocalObjectStorageConfiguration>()
    .BindConfiguration("LocalObjectStorage")
    .ValidateDataAnnotations();

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
    .AllowAnyOrigin()
    .WithExposedHeaders(tusdotnet.Helpers.CorsHelper.GetExposedHeaders());
});
//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapTusUpload();
app.LoadPlatforms();
app.Run();
