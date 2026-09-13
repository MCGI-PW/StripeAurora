using AuroraPet.Payments.Application.Interfaces;
using AuroraPet.Payments.Application.Options;
using AuroraPet.Payments.Infrastructure.Payments;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection(StripeOptions.SectionName));

builder.Services.AddScoped<ISubscriptionService, StripeSubscriptionService>();

// Health Check (requisito da Sprint 3/4)
builder.Services.AddHealthChecks()
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

app.Run();
