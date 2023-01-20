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
        "Map",
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
                            return System.Threading.Tasks.ValueTask.FromResult<object?>(Results.Empty);
                        }
                        {{StaticRouteHandlerModelEmitter.EmitFilteredInvocation()}}
                    },
                    options.EndpointBuilder,
                    handler.Method);
                }

                {{StaticRouteHandlerModelEmitter.EmitRequestHandler()}}
                {{StaticRouteHandlerModelEmitter.EmitFilteredRequestHandler()}}

                RequestDelegate targetDelegate = filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
                return new RequestDelegateResult(targetDelegate, inferredMetadataResult.EndpointMetadata);
            }),
""");

        var stronglyTypedEndpointDefinitions = endpoints.Select((endpoint, _) => $$"""
{{RequestDelegateGeneratorSources.GeneratedCodeAttribute}}
internal static Microsoft.AspNetCore.Builder.RouteHandlerBuilder {{endpoint.HttpMethod}}(
        this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints,
        [System.Diagnostics.CodeAnalysis.StringSyntax("Route")] string pattern,
        {{StaticRouteHandlerModelEmitter.EmitHandlerDelegateType(endpoint)}} handler,
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber]int lineNumber = 0)
    {
        return GeneratedRouteBuilderExtensionsCore.MapCore(endpoints, pattern, handler, GetVerb, filePath, lineNumber);
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
