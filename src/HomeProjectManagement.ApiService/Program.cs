using HomeProjectManagement.ApiService.Data;
using HomeProjectManagement.ApiService.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: telemetry, health checks, service discovery, resilience.
builder.AddServiceDefaults();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Aspire-wired Npgsql EF Core. "projectsdb" matches the resource name in the AppHost.
builder.AddNpgsqlDbContext<AppDbContext>("projectsdb");

// Allow the Next.js dev server to call the API from the browser.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Aspire health/liveness endpoints.
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Dev-only convenience: ensure the schema exists. Use EF Core migrations for production.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();

var projects = app.MapGroup("/api/projects");

projects.MapGet("/", async (AppDbContext db) =>
    await db.Projects.OrderByDescending(p => p.CreatedAt).ToListAsync());

projects.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
    await db.Projects.FindAsync(id) is { } project
        ? Results.Ok(project)
        : Results.NotFound());

projects.MapPost("/", async (Project project, AppDbContext db) =>
{
    project.Id = Guid.NewGuid();
    project.CreatedAt = DateTimeOffset.UtcNow;
    db.Projects.Add(project);
    await db.SaveChangesAsync();
    return Results.Created($"/api/projects/{project.Id}", project);
});

projects.MapPut("/{id:guid}", async (Guid id, Project input, AppDbContext db) =>
{
    var project = await db.Projects.FindAsync(id);
    if (project is null) return Results.NotFound();

    project.Name = input.Name;
    project.Description = input.Description;
    project.Status = input.Status;
    project.DueDate = input.DueDate;
    await db.SaveChangesAsync();
    return Results.Ok(project);
});

projects.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var project = await db.Projects.FindAsync(id);
    if (project is null) return Results.NotFound();

    db.Projects.Remove(project);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
