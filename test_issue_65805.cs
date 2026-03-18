using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Microsoft.AspNetCore.OpenApi.Tests;

public class RequestBodyCommentTests
{
    [Fact]
    public async Task RequestBodyDescriptionUsesCorrectParameterComment()
    {
        // Regression test for issue #65805
        // When a method has multiple parameters including [FromBody] and [FromServices],
        // the request body description should use the [FromBody] parameter's XML comment,
        // not the last parameter's comment.
        
        var app = CreateMinimalApp();
        var openApiDocument = await GetOpenApiDocument(app);
        
        var postFooOperation = openApiDocument.Paths["/foo"].Operations[OperationType.Post];
        var requestBody = postFooOperation.RequestBody;
        
        // The request body description should be from the [FromBody] SomeData parameter
        Assert.NotNull(requestBody);
        Assert.Equal("Sample data provided by the user.", requestBody.Description);
        // Should NOT be "Injected cancellation token." (from the CancellationToken parameter)
    }
    
    private WebApplication CreateMinimalApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddOpenApi();
        
        var app = builder.Build();
        app.MapOpenApi();
        
        // Map the endpoint from issue #65805
        app.MapPost("/foo", PostSampleData)
            .WithName("PostSampleData")
            .WithOpenApi();
        
        return app;
    }
    
    // This mirrors the reproduction case from issue #65805
    /// <summary>
    /// Process some sample input.
    /// </summary>
    /// <param name="data">Sample data provided by the user.</param>
    /// <param name="logger">Logger for diagnostics and tracing.</param>
    /// <param name="cancellation">Injected cancellation token.</param>
    /// <returns>The number the user supplied.</returns>
    public record SomeData(int Number, string Text);
    
    public static IResult PostSampleData(
        [FromBody] SomeData data,
        [FromServices] ILogger<SomeData> logger,
        CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Results.Ok(data.Number);
    }
    
    private async Task<OpenApiDocument> GetOpenApiDocument(WebApplication app)
    {
        var openApiDocumentProvider = app.Services.GetRequiredService<IOpenApiDocumentProvider>();
        return await openApiDocumentProvider.GetOpenApiDocumentAsync(app.Services);
    }
}
