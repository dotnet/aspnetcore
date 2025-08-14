# HTTP QUERY Method Support in ASP.NET Core

This document provides examples of how to use the new HTTP QUERY method support added to ASP.NET Core.

## HttpMethods Class

The `HttpMethods` class now includes support for the QUERY method:

```csharp
using Microsoft.AspNetCore.Http;

// New QUERY method constant
string queryMethod = HttpMethods.Query; // "QUERY"

// New IsQuery method
bool isQuery = HttpMethods.IsQuery("QUERY"); // true
bool isQueryLowercase = HttpMethods.IsQuery("query"); // true

// GetCanonicalizedValue now supports QUERY
string canonicalized = HttpMethods.GetCanonicalizedValue("query"); // Returns HttpMethods.Query
```

## MVC Controller Support

Use the new `HttpQueryAttribute` in MVC controllers:

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    [HttpQuery]
    public IActionResult Search([FromQuery] string q, [FromQuery] int limit = 10)
    {
        // Handle QUERY request with query parameters in the body
        return Ok(new { query = q, limit = limit });
    }
    
    [HttpQuery("search/{category}")]
    public IActionResult SearchByCategory(string category, [FromQuery] string q)
    {
        // Handle QUERY request with both route and query parameters
        return Ok(new { category = category, query = q });
    }
}
```

## Minimal API Support

Use the new `MapQuery` extension methods with minimal APIs:

```csharp
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// MapQuery with RequestDelegate
app.MapQuery("/search", async (HttpContext context) =>
{
    var query = context.Request.Query["q"].ToString();
    return Results.Ok(new { query = query });
});

// MapQuery with typed delegate
app.MapQuery("/search/{category}", (string category, string q) =>
{
    return Results.Ok(new { category = category, query = q });
});

// MapQuery with async delegate
app.MapQuery("/async-search", async (string q, ILogger<Program> logger) =>
{
    logger.LogInformation("Processing query: {Query}", q);
    await Task.Delay(100); // Simulate async work
    return Results.Ok(new { query = q, timestamp = DateTime.UtcNow });
});

app.Run();
```

## Usage with HTTP Clients

Example of making QUERY requests:

```csharp
using System.Net.Http;

var client = new HttpClient();

// Simple QUERY request
var request = new HttpRequestMessage(new HttpMethod("QUERY"), "https://api.example.com/search?q=test");
var response = await client.SendAsync(request);

// QUERY request with content body (if supported by the API)
var queryRequest = new HttpRequestMessage(new HttpMethod("QUERY"), "https://api.example.com/advanced-search")
{
    Content = JsonContent.Create(new { 
        query = "search terms", 
        filters = new { category = "books", minPrice = 10 } 
    })
};
var queryResponse = await client.SendAsync(queryRequest);
```

## Benefits of HTTP QUERY Method

The HTTP QUERY method is designed for safe queries that may include request bodies:

- **Safe**: Like GET, QUERY requests should not modify server state
- **Cacheable**: QUERY responses can be cached like GET responses  
- **Request Body**: Unlike GET, QUERY allows request bodies for complex query parameters
- **Semantic Clarity**: Clearly indicates query operations vs. data retrieval (GET)

For more information, see the HTTP QUERY method specification: https://datatracker.ietf.org/doc/draft-ietf-httpbis-safe-method-w-body/