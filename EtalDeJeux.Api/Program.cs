using EtalDeJeux.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// --- DB (Supabase via PG_CONN_STR ou appsettings) ---
var connStr =
    Environment.GetEnvironmentVariable("PG_CONN_STR")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new Exception("Missing Postgres connection string");

// ✅ Active la (dé)sérialisation JSON dynamique pour les colonnes json/jsonb (Npgsql 8+)
var dsBuilder = new NpgsqlDataSourceBuilder(connStr);
dsBuilder.EnableDynamicJson();
var dataSource = dsBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(dataSource));

// --- MVC / Controllers ---
builder.Services.AddControllers().AddJsonOptions(o =>
{
    // (optionnel) garde System.Text.Json par défaut
});

// --- CORS ---
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();

#if DEBUG
// Migrations + seed en Dev
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
