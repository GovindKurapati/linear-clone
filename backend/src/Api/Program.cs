using LinearClone.Api.Infrastructure;
using LinearClone.Application.Issues;
using LinearClone.Infrastructure.Issues;
using LinearClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Service registration (the DI container)
// ---------------------------------------------------------------------------

// Controllers (you chose controllers over Minimal API).
builder.Services.AddControllers();

// OpenAPI document generation (first-party, .NET 9+). Scalar renders this.
builder.Services.AddOpenApi();

// EF Core + SQL Server. Connection string "Default" is resolved from configuration,
// which in Development includes your User Secrets (and appsettings.Development.json).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Application services (controller -> service -> DbContext). Scoped = one per request.
builder.Services.AddScoped<IIssueService, IssueService>();

// Global exception handling: maps domain exceptions -> HTTP status codes,
// returns RFC 7807 ProblemDetails. Replaces per-controller try/catch.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// ---------------------------------------------------------------------------
// HTTP middleware pipeline (order matters — top runs first)
// ---------------------------------------------------------------------------

// Exception handler goes early so it can catch everything downstream.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();              // OpenAPI JSON at /openapi/v1.json
    app.MapScalarApiReference();   // Scalar UI at /scalar/v1
}

app.UseHttpsRedirection();

app.MapControllers();

// ---------------------------------------------------------------------------
// Seed dev data on startup (idempotent — only runs if the DB is empty).
// CreateScope() because AppDbContext is scoped and startup runs outside a request.
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();