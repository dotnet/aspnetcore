// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OpenApi.Microbenchmarks;

/// <summary>
/// The following benchmarks are used to assess the performance of the
/// core OpenAPI document generation logic. The parameter under test here
/// is the number of endpoints/operations that are defined in the application.
/// </summary>
[MemoryDiagnoser]
public class GenerationBenchmarks : OpenApiDocumentServiceTestBase
{
    [Params(10, 100, 1000)]
    public int EndpointCount { get; set; }

    private readonly IEndpointRouteBuilder _builder = CreateBuilder();
    private readonly OpenApiOptions _options = new OpenApiOptions();
    private OpenApiDocumentService _documentService;
    private IServiceProvider _serviceProvider;

    [GlobalSetup(Target = nameof(GenerateDocument))]
    public void OperationTransformerAsDelegate_Setup()
    {
        _builder.MapGet("/", () => { });
        for (var i = 0; i <= EndpointCount; i++)
        {
            _builder.MapGet($"/{i}", (int i) => new Todo(1, "Write benchmarks", false, DateTime.Now));
            _builder.MapPost($"/{i}", (Todo todo) => Results.Ok());
            _builder.MapDelete($"/{i}", (string id) => Results.NoContent());
        }
        _documentService = CreateDocumentService(_builder, _options);
        _serviceProvider = _builder.ServiceProvider.CreateScope().ServiceProvider;
    }

    [Benchmark]
    public async Task GenerateDocument()
    {
        await _documentService.GetOpenApiDocumentAsync(_serviceProvider);
    }
}
