// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

namespace Microsoft.AspNetCore.Http.Generators;

[Generator]
public sealed class RequestDelegateGenerator : IIncrementalGenerator
{
    private static readonly string[] _knownMethods =
    {
        "MapGet",
        "MapPost",
        "MapPut",
        "MapDelete",
        "MapPatch",
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var endpoints = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) => node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name: IdentifierNameSyntax
                    {
                        Identifier: { ValueText: var method }
                    }
                },
                ArgumentList: { Arguments: { Count: 2 } args }
            } && _knownMethods.Contains(method),
            transform: (context, token) =>
            {
                var operation = context.SemanticModel.GetOperation(context.Node, token) as IInvocationOperation;
                return StaticRouteHandlerModelParser.GetEndpointFromOperation(operation);
            })
            .Where(endpoint => endpoint.Response.ResponseType == "string")
            .WithTrackingName("EndpointModel");

        var thunks = endpoints.Select((endpoint, _) => $$"""
[{{StaticRouteHandlerModelEmitter.EmitSourceKey(endpoint)}}] = (
           (methodInfo, options) =>
            {
                if (options == null)
                {
                    return new RequestDelegateMetadataResult { EndpointMetadata = ReadOnlyCollection<object>.Empty };
                }
                options.EndpointBuilder.Metadata.Add(new SourceKey{{StaticRouteHandlerModelEmitter.EmitSourceKey(endpoint)}});
                return new RequestDelegateMetadataResult { EndpointMetadata = options.EndpointBuilder.Metadata.AsReadOnly() };
            },
            (del, options, inferredMetadataResult) =>
            {
                var handler = ({{StaticRouteHandlerModelEmitter.EmitHandlerDelegateType(endpoint)}})del;
                EndpointFilterDelegate? filteredInvocation = null;

                if (options.EndpointBuilder.FilterFactories.Count > 0)
                {
                    filteredInvocation = GeneratedRouteBuilderExtensionsCore.BuildFilterDelegate(ic =>
                    {
                        if (ic.HttpContext.Response.StatusCode == 400)
                        {
                            return ValueTask.FromResult<object?>(Results.Empty);
                        }
                        {{StaticRouteHandlerModelEmitter.EmitFilteredInvocation()}}
                    },
                    options.EndpointBuilder,
                    handler.Method);
                }

                {{StaticRouteHandlerModelEmitter.EmitRequestHandler()}}
                {{StaticRouteHandlerModelEmitter.EmitFilteredRequestHandler()}}

                RequestDelegate targetDelegate = filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
                var metadata = inferredMetadataResult?.EndpointMetadata ?? ReadOnlyCollection<object>.Empty;
                return new RequestDelegateResult(targetDelegate, metadata);
            }),
""");

        var stronglyTypedEndpointDefinitions = endpoints.Select((endpoint, _) => $$"""
{{RequestDelegateGeneratorSources.GeneratedCodeAttribute}}
    internal static global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder {{endpoint.HttpMethod}}(
        this global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints,
        [global::System.Diagnostics.CodeAnalysis.StringSyntax("Route")] string pattern,
        global::{{StaticRouteHandlerModelEmitter.EmitHandlerDelegateType(endpoint)}} handler,
        [global::System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [global::System.Runtime.CompilerServices.CallerLineNumber]int lineNumber = 0)
    {
        return global::Microsoft.AspNetCore.Http.Generated.GeneratedRouteBuilderExtensionsCore.MapCore(endpoints, pattern, handler, {{StaticRouteHandlerModelEmitter.EmitVerb(endpoint)}}, filePath, lineNumber);
    }
""");

        var thunksAndEndpoints = thunks.Collect().Combine(stronglyTypedEndpointDefinitions.Collect());

        context.RegisterSourceOutput(thunksAndEndpoints, (context, sources) =>
        {
            var (thunks, endpoints) = sources;

            var endpointsCode = new StringBuilder();
            var thunksCode = new StringBuilder();
            foreach (var endpoint in endpoints)
            {
                endpointsCode.AppendLine(endpoint);
            }
            foreach (var thunk in thunks)
            {
                thunksCode.AppendLine(thunk);
            }

            var code = RequestDelegateGeneratorSources.GetGeneratedRouteBuilderExtensionsSource(
                genericThunks: string.Empty,
                thunks: thunksCode.ToString(),
                endpoints: endpointsCode.ToString());
            context.AddSource("GeneratedRouteBuilderExtensions.g.cs", code);
        });
    }
}
