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
builder.Services.AddAsecServices();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
