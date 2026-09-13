using AuroraPet.Infrastructure.Data;
using AuroraPet.Payments.Application.Interfaces;
using AuroraPet.Payments.Application.Options;
using AuroraPet.Payments.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Banco de dados Oracle (FIAP) via Entity Framework Core
builder.Services.AddDbContext<AuroraDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));

builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection(StripeOptions.SectionName));

builder.Services.AddScoped<ISubscriptionService, StripeSubscriptionService>();

// Health Check (requisito da Sprint 3/4)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AuroraDbContext>("oracle")
    .AddCheck("stripe_config", () =>
    {
        var stripeKey = builder.Configuration["Stripe:SecretKey"];
        return string.IsNullOrEmpty(stripeKey)
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Stripe não configurado")
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
    });

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
