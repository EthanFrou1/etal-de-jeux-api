using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using EtalDeJeux.Api.Options;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.NameTranslation;
using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Stripe;
using EtalDeJeux.Api.Services;

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

builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()
));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var stripeSecretKey =
    Environment.GetEnvironmentVariable("STRIPE_SECRET") ??
    builder.Configuration["Stripe:SecretKey"];

builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ICheckoutService, EtalDeJeux.Api.Services.CheckoutService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IOrderEmailService, OrderEmailService>();

if (!string.IsNullOrWhiteSpace(stripeSecretKey))
{
    StripeConfiguration.ApiKey = stripeSecretKey;
}

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
