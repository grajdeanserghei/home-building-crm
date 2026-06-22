using System.Text.Json.Serialization;
using HomeProjectManagement.ApiService.Endpoints;
using HomeProjectManagement.Application;
using HomeProjectManagement.Infrastructure;
using HomeProjectManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: telemetry, health checks, service discovery, resilience.
builder.AddServiceDefaults();

// Serialize/deserialize enums (e.g. ProjectStatus) as their string names so the
// JSON matches the frontend's TypeScript types and the values stored by EF Core.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Aspire-wired Npgsql EF Core. "projectsdb" matches the resource name in the AppHost.
// The DbContext type itself lives in Infrastructure.
builder.AddNpgsqlDbContext<AppDbContext>("projectsdb");

// Hexagonal composition root: application use cases + infrastructure adapters.
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// Allow the Next.js dev server to call the API from the browser.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Aspire health/liveness endpoints.
app.MapDefaultEndpoints();

// Apply any pending EF Core migrations on startup. Aspire's WaitFor(projectsDb)
// ensures the database is ready before this runs.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// Map the driving adapters (minimal-API endpoint groups).
app.MapProjectEndpoints();
app.MapWorkPackageEndpoints();
app.MapContractorEndpoints();
app.MapUnitOfMeasureEndpoints();

app.Run();
