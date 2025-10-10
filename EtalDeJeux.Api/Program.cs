using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.NameTranslation;
using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// --- Connexion PostgreSQL ---
var connStr =
    Environment.GetEnvironmentVariable("PG_CONN_STR")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new Exception("Missing Postgres connection string");

var dsBuilder = new NpgsqlDataSourceBuilder(connStr);
dsBuilder.EnableDynamicJson();
var dataSource = dsBuilder.Build();

// --- DbContext EF Core ---
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(dataSource)
       .UseSnakeCaseNamingConvention()
       .ConfigureWarnings(w => w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning))
);

builder.Services.AddControllers();

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()
));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
