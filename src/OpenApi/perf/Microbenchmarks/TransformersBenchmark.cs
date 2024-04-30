// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi.Microbenchmarks;

/// <summary>
/// The following benchmarks are used to assess the memory and performance
/// impact of different types of transformers. In particular, we want to
/// measure the impact of (a) context-object creation and caching and (b)
/// enumerator usage when processing operations in a given document.
/// </summary>
public class TransformersBenchmark : OpenApiDocumentServiceTestBase
{
    [Params(10, 100, 1000)]
    public int TransformerCount { get; set; }

    private readonly IEndpointRouteBuilder _builder = CreateBuilder();
    private readonly OpenApiOptions _options = new OpenApiOptions();
    private OpenApiDocumentService _documentService;

    [GlobalSetup(Target = nameof(OperationTransformerAsDelegate))]
    public void OperationTransformerAsDelegate_Setup()
    {
        _builder.MapGet("/", () => { });
        for (var i = 0; i <= TransformerCount; i++)
        {
            _options.UseOperationTransformer((operation, context, token) =>
            {
                operation.Description = "New Description";
                return Task.CompletedTask;
            });
        }
        _documentService = CreateDocumentService(_builder, _options);
    }

    [GlobalSetup(Target = nameof(ActivatedDocumentTransformer))]
    public void ActivatedDocumentTransformer_Setup()
    {
        _builder.MapGet("/", () => { });
        for (var i = 0; i <= TransformerCount; i++)
        {
            _options.UseTransformer<ActivatedTransformer>();
        }
        _documentService = CreateDocumentService(_builder, _options);
    }

    [GlobalSetup(Target = nameof(DocumentTransformerAsDelegate))]
    public void DocumentTransformerAsDelegate_Delegate()
    {
        _builder.MapGet("/", () => { });
        for (var i = 0; i <= TransformerCount; i++)
        {
            _options.UseTransformer((document, context, token) =>
            {
                document.Info.Description = "New Description";
                return Task.CompletedTask;
            });
        }
        _documentService = CreateDocumentService(_builder, _options);
    }

    [Benchmark]
    public async Task OperationTransformerAsDelegate()
    {
        await _documentService.GetOpenApiDocumentAsync();
    }

    [Benchmark]
    public async Task ActivatedDocumentTransformer()
    {
        await _documentService.GetOpenApiDocumentAsync();
    }

    [Benchmark]
    public async Task DocumentTransformerAsDelegate()
    {
        await _documentService.GetOpenApiDocumentAsync();
    }

    private class ActivatedTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info.Description = "Info Description";
            return Task.CompletedTask;
        }
    }
}
