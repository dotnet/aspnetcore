// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
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
    private readonly OpenApiOptions _options = new();
    private OpenApiDocumentService _documentService;
    private IServiceProvider _serviceProvider;

    [GlobalSetup(Target = nameof(ActivatedOperationTransformer))]
    public void ActivatedOperationTransformer_Setup()
    {
        _builder.MapGet("/", () => { });
        for (var i = 0; i <= TransformerCount; i++)
        {
            _options.AddOperationTransformer<OperationTransformer>();
        }
        _documentService = CreateDocumentService(_builder, _options);
        _serviceProvider = _builder.ServiceProvider.CreateScope().ServiceProvider;
    }

    [GlobalSetup(Target = nameof(OperationTransformerAsDelegate))]
    public void OperationTransformerAsDelegate_Setup()
    {
        _builder.MapGet("/", () => { });
        for (var i = 0; i <= TransformerCount; i++)
        {
            _options.AddOperationTransformer((operation, context, token) =>
            {
                operation.Description = "New Description";
                return Task.CompletedTask;
            });
        }
        _documentService = CreateDocumentService(_builder, _options);
        _serviceProvider = _builder.ServiceProvider.CreateScope().ServiceProvider;
    }

    [GlobalSetup(Target = nameof(ActivatedDocumentTransformer))]
    public void ActivatedDocumentTransformer_Setup()
    {
        _builder.MapGet("/", () => { });
        for (var i = 0; i <= TransformerCount; i++)
        {
            _options.AddDocumentTransformer<DocumentTransformer>();
        }
        _documentService = CreateDocumentService(_builder, _options);
        _serviceProvider = _builder.ServiceProvider.CreateScope().ServiceProvider;
    }

    [GlobalSetup(Target = nameof(DocumentTransformerAsDelegate))]
    public void DocumentTransformerAsDelegate_Delegate()
    {
        _builder.MapGet("/", () => { });
        for (var i = 0; i <= TransformerCount; i++)
        {
            _options.AddDocumentTransformer((document, context, token) =>
            {
                document.Info.Description = "New Description";
                return Task.CompletedTask;
            });
        }
        _documentService = CreateDocumentService(_builder, _options);
        _serviceProvider = _builder.ServiceProvider.CreateScope().ServiceProvider;
    }

    [GlobalSetup(Target = nameof(ActivatedSchemaTransformer))]
    public void ActivatedSchemaTransformer_Setup()
    {
        _builder.MapPost("/", (Todo todo) => todo);
        for (var i = 0; i <= TransformerCount; i++)
        {
            _options.AddSchemaTransformer<SchemaTransformer>();
        }
        _documentService = CreateDocumentService(_builder, _options);
        _serviceProvider = _builder.ServiceProvider.CreateScope().ServiceProvider;
    }

    [GlobalSetup(Target = nameof(SchemaTransformerAsDelegate))]
    public void SchemaTransformer_Setup()
    {
        _builder.MapPost("/", (Todo todo) => todo);
        for (var i = 0; i <= TransformerCount; i++)
        {
            _options.AddSchemaTransformer((schema, context, token) =>
            {
                if (context.JsonTypeInfo.Type == typeof(Todo) && context.ParameterDescription != null)
                {
                    schema.Extensions["x-my-extension"] = new OpenApiAny(context.ParameterDescription.Name);
                }
                else
                {
                    schema.Extensions["x-my-extension"] = new OpenApiAny("response");
                }
                return Task.CompletedTask;
            });
        }
        _documentService = CreateDocumentService(_builder, _options);
        _serviceProvider = _builder.ServiceProvider.CreateScope().ServiceProvider;
    }

    [Benchmark]
    public async Task ActivatedOperationTransformer()
    {
        await _documentService.GetOpenApiDocumentAsync(_serviceProvider);
    }

    [Benchmark]
    public async Task OperationTransformerAsDelegate()
    {
        await _documentService.GetOpenApiDocumentAsync(_serviceProvider);
    }

    [Benchmark]
    public async Task ActivatedDocumentTransformer()
    {
        await _documentService.GetOpenApiDocumentAsync(_serviceProvider);
    }

    [Benchmark]
    public async Task DocumentTransformerAsDelegate()
    {
        await _documentService.GetOpenApiDocumentAsync(_serviceProvider);
    }

    [Benchmark]
    public async Task ActivatedSchemaTransformer()
    {
        await _documentService.GetOpenApiDocumentAsync(_serviceProvider);
    }

    [Benchmark]
    public async Task SchemaTransformerAsDelegate()
    {
        await _documentService.GetOpenApiDocumentAsync(_serviceProvider);
    }

    private class DocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info.Description = "Info Description";
            return Task.CompletedTask;
        }
    }

    private class OperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            operation.Description = "Operation Description";
            return Task.CompletedTask;
        }
    }

    private class SchemaTransformer : IOpenApiSchemaTransformer
    {
        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            if (context.JsonTypeInfo.Type == typeof(Todo) && context.ParameterDescription != null)
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny(context.ParameterDescription.Name);
            }
            else
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny("response");
            }
            return Task.CompletedTask;
        }
    }
}
