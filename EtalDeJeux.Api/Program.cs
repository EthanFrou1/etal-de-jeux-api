using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.NameTranslation;

var builder = WebApplication.CreateBuilder(args);

var connStr =
    Environment.GetEnvironmentVariable("PG_CONN_STR")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new Exception("Missing Postgres connection string");

var dsBuilder = new NpgsqlDataSourceBuilder(connStr);
var nameTranslator = new NpgsqlSnakeCaseNameTranslator();
dsBuilder.EnableDynamicJson();
dsBuilder.MapEnum<ProductType>("product_type", nameTranslator);
dsBuilder.MapEnum<FileKind>("file_kind", nameTranslator);
var dataSource = dsBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(dataSource));

builder.Services.AddControllers();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();

#if DEBUG
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await db.SeedAsync();
}
#endif

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
