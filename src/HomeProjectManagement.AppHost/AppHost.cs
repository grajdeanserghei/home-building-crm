var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL: a persistent data volume keeps data across restarts; pgAdmin gives a UI.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var projectsDb = postgres.AddDatabase("projectsdb");

// .NET Core Web API backend.
var apiService = builder.AddProject<Projects.HomeProjectManagement_ApiService>("apiservice")
    .WithReference(projectsDb)
    .WaitFor(projectsDb)
    // Add a clickable link to the Scalar OpenAPI UI (served at /scalar in development)
    // alongside the default endpoint link in the Aspire dashboard.
    .WithUrlForEndpoint("http", endpoint => new ResourceUrlAnnotation
    {
        Url = $"{endpoint.Url}/scalar",
        DisplayText = "Scalar API Reference",
    });

// Remote MCP server: a second driving adapter that lets connected AI agents (Claude / ChatGPT)
// drive contractor/bid/BoQ data entry. It shares the projectsdb and is reachable from outside the
// Aspire network because remote agent clients connect to it directly.
builder.AddProject<Projects.HomeProjectManagement_McpServer>("mcpserver")
    .WithReference(projectsDb)
    .WaitFor(projectsDb)
    .WithExternalHttpEndpoints();

// Next.js frontend. AddNextJsApp handles the dev-server port binding and runs `npm run dev`.
builder.AddNextJsApp("web", "../web")
    .WithNpm()
    .WithReference(apiService)
    .WaitFor(apiService)
    // Server-side code in Next.js reads the backend URL from this env var.
    .WithEnvironment("API_BASE_URL", apiService.GetEndpoint("http"));

builder.Build().Run();
