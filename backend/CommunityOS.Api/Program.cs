using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Minimal services for now; we’ll add Auth/EF Core/Tenant middleware next.
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();


// Root endpoint for infra probes.
app.MapGet("/", () => Results.Text("CommunityOS API running", "text/plain"))
   .WithName("Root");

// Basic health check for supervisor + curl sanity.
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
   .WithName("Health");

app.Run();
