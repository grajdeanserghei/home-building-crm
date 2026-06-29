using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using HomeProjectManagement.Application;
using HomeProjectManagement.Infrastructure;
using HomeProjectManagement.Infrastructure.Persistence;
using HomeProjectManagement.McpServer.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (OpenTelemetry, health checks, service discovery).
builder.AddServiceDefaults();

// Same projectsdb the ApiService uses; the schema is owned by the ApiService's startup migration,
// so this host only reads/writes — it does not migrate.
builder.AddNpgsqlDbContext<AppDbContext>("projectsdb");

// Composition root: identical wiring to the ApiService. Tools call these app-service ports in-process.
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// Serialize enums as their string names in tool I/O so the MCP schema matches the REST contract
// and the frontend's TypeScript enums (e.g. BidStatus, NoteType, Currency).
var toolSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    // Reflection-based (de)serialization for the tool argument/result types. Without an explicit
    // resolver, MakeReadOnly() throws when the MCP library finalizes these options at startup.
    TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
};
toolSerializerOptions.Converters.Add(new JsonStringEnumConverter());

builder.Services
    .AddMcpServer()
    .WithHttpTransport()                                  // Streamable HTTP — what remote clients use.
    .WithToolsFromAssembly(serializerOptions: toolSerializerOptions); // discovers [McpServerTool] methods.

// Cloudflare Access token validation (gated by CloudflareAccess:Enabled). When disabled, the host
// runs network-restricted with the StubCurrentUser; when enabled, it validates the Cloudflare Access
// assertion forwarded by the tunnel, re-checks the stakeholder allow-list, and attributes writes to
// the authenticated principal. The OAuth flow + Google login + edge allow-list live in Cloudflare
// Access (Managed OAuth) — see docs/specifications/cloudflare-access-authentication.md.
var authEnabled = builder.AddCloudflareAccessAuthentication();

var app = builder.Build();

app.MapDefaultEndpoints();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

var mcp = app.MapMcp();
if (authEnabled)
{
    mcp.RequireAuthorization(CloudflareAccessAuthExtensions.StakeholderPolicy);
}

app.Run();
