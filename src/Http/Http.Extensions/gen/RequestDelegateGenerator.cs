// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel.Emitters;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator;

[Generator]
public sealed class RequestDelegateGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var endpointsWithDiagnostics = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node.TryGetMapMethodName(out var method) && InvocationOperationExtensions.KnownMethods.Contains(method),
            transform: static (context, token) =>
            {
                var operation = context.SemanticModel.GetOperation(context.Node, token);
                var wellKnownTypes = WellKnownTypes.GetOrCreate(context.SemanticModel.Compilation);
                if (operation is IInvocationOperation invocationOperation &&
                    invocationOperation.TryGetRouteHandlerArgument(out var routeHandlerParameter) &&
                    routeHandlerParameter is { Parameter.Type: {} delegateType } &&
                    SymbolEqualityComparer.Default.Equals(delegateType, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Delegate)))
                {
                    return new Endpoint(invocationOperation, wellKnownTypes, context.SemanticModel);
                }
                return null;
            })
            .Where(static endpoint => endpoint != null)
            .Select((endpoint, _) =>
            {
                AnalyzerDebug.Assert(endpoint != null, "Invalid endpoints should not be processed.");
                return endpoint;
            })
            .WithTrackingName(GeneratorSteps.EndpointModelStep);

        context.RegisterSourceOutput(endpointsWithDiagnostics, (context, endpoint) =>
        {
            foreach (var diagnostic in endpoint.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        });

        var endpoints = endpointsWithDiagnostics
            .Where(endpoint => endpoint.Diagnostics.Count == 0)
            .WithTrackingName(GeneratorSteps.EndpointsWithoutDiagnosicsStep);

        var thunks = endpoints.Select((endpoint, _) =>
        {
            using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            using var codeWriter = new CodeWriter(stringWriter, baseIndent: 3);
            codeWriter.InitializeIndent();
            codeWriter.WriteLine($"[{endpoint.EmitSourceKey()}] = (");
            codeWriter.Indent++;
            codeWriter.WriteLine("(methodInfo, options) =>");
            codeWriter.StartBlock();
            codeWriter.WriteLine(@"Debug.Assert(options != null, ""RequestDelegateFactoryOptions not found."");");
            codeWriter.WriteLine(@"Debug.Assert(options.EndpointBuilder != null, ""EndpointBuilder not found."");");
            codeWriter.WriteLine($"options.EndpointBuilder.Metadata.Add(new SourceKey{endpoint.EmitSourceKey()});");
            endpoint.EmitEndpointMetadataPopulation(codeWriter);
            codeWriter.WriteLine("return new RequestDelegateMetadataResult { EndpointMetadata = options.EndpointBuilder.Metadata.AsReadOnly() };");
            codeWriter.EndBlockWithComma();
            codeWriter.WriteLine("(del, options, inferredMetadataResult) =>");
            codeWriter.StartBlock();
            codeWriter.WriteLine(@"Debug.Assert(options != null, ""RequestDelegateFactoryOptions not found."");");
            codeWriter.WriteLine(@"Debug.Assert(options.EndpointBuilder != null, ""EndpointBuilder not found."");");
            codeWriter.WriteLine(@"Debug.Assert(options.EndpointBuilder.ApplicationServices != null, ""ApplicationServices not found."");");
            codeWriter.WriteLine(@"Debug.Assert(options.EndpointBuilder.FilterFactories != null, ""FilterFactories not found."");");
            codeWriter.WriteLine($"var handler = ({endpoint.EmitHandlerDelegateType(considerOptionality: true)})del;");
            codeWriter.WriteLine("EndpointFilterDelegate? filteredInvocation = null;");
            if (endpoint.EmitterContext.RequiresLoggingHelper || endpoint.EmitterContext.HasJsonBodyOrService || endpoint.Response?.IsSerializableJsonResponse(out var _) is true)
            {
                codeWriter.WriteLine("var serviceProvider = options.ServiceProvider ?? options.EndpointBuilder.ApplicationServices;");
            }
            endpoint.EmitLoggingPreamble(codeWriter);
            endpoint.EmitRouteOrQueryResolver(codeWriter);
            endpoint.EmitJsonBodyOrServiceResolver(codeWriter);
            endpoint.Response?.EmitJsonPreparation(codeWriter);
            if (endpoint.NeedsParameterArray)
            {
                codeWriter.WriteLine("var parameters = del.Method.GetParameters();");
            }
            codeWriter.WriteLineNoTabs(string.Empty);
            codeWriter.WriteLine("if (options.EndpointBuilder.FilterFactories.Count > 0)");
            codeWriter.StartBlock();
            codeWriter.WriteLine(endpoint.Response?.IsAwaitable == true
                ? "filteredInvocation = GeneratedRouteBuilderExtensionsCore.BuildFilterDelegate(async ic =>"
                : "filteredInvocation = GeneratedRouteBuilderExtensionsCore.BuildFilterDelegate(ic =>");
            codeWriter.StartBlock();
            codeWriter.WriteLine("if (ic.HttpContext.Response.StatusCode == 400)");
            codeWriter.StartBlock();
            codeWriter.WriteLine(endpoint.Response?.IsAwaitable == true
                ? "return (object?)Results.Empty;"
                : "return ValueTask.FromResult<object?>(Results.Empty);");
            codeWriter.EndBlock();
            endpoint.EmitFilteredInvocation(codeWriter);
            codeWriter.EndBlockWithComma();
            codeWriter.WriteLine("options.EndpointBuilder,");
            codeWriter.WriteLine("handler.Method);");
            codeWriter.EndBlock();
            codeWriter.WriteLineNoTabs(string.Empty);
            endpoint.EmitRequestHandler(codeWriter);
            codeWriter.WriteLineNoTabs(string.Empty);
            endpoint.EmitFilteredRequestHandler(codeWriter);
            codeWriter.WriteLineNoTabs(string.Empty);
            codeWriter.WriteLine("RequestDelegate targetDelegate = filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;");
            codeWriter.WriteLine("var metadata = inferredMetadataResult?.EndpointMetadata ?? ReadOnlyCollection<object>.Empty;");
            codeWriter.WriteLine("return new RequestDelegateResult(targetDelegate, metadata);");
            codeWriter.Indent--;
            codeWriter.Write("}),");
            return stringWriter.ToString();
        });

        var stronglyTypedEndpointDefinitions = endpoints
            .Collect()
            .Select((endpoints, _) =>
            {
                var dedupedByDelegate = endpoints.Distinct<Endpoint>(EndpointDelegateComparer.Instance);
                using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                using var codeWriter = new CodeWriter(stringWriter, baseIndent: 2);
                foreach (var endpoint in dedupedByDelegate)
                {
                    codeWriter.WriteLine($"internal static global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder {endpoint.HttpMethod}(");
                    codeWriter.Indent++;
                    codeWriter.WriteLine("this global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints,");
                    // MapFallback overloads that only take a delegate do not need a pattern argument
                    if (endpoint.HttpMethod != "MapFallback" || endpoint.Operation.Arguments.Length != 2)
                    {
                        codeWriter.WriteLine(@"[global::System.Diagnostics.CodeAnalysis.StringSyntax(""Route"")] string pattern,");
                    }
                    // MapMethods overloads define an additional `httpMethods` parameter
                    if (endpoint.HttpMethod == "MapMethods")
                    {
                        codeWriter.WriteLine("global::System.Collections.Generic.IEnumerable<string> httpMethods,");
                    }
                    codeWriter.WriteLine($"global::{endpoint.EmitHandlerDelegateType()} handler,");
                    codeWriter.WriteLine(@"[global::System.Runtime.CompilerServices.CallerFilePath] string filePath = """",");
                    codeWriter.WriteLine("[global::System.Runtime.CompilerServices.CallerLineNumber]int lineNumber = 0)");
                    codeWriter.Indent--;
                    codeWriter.StartBlock();
                    codeWriter.WriteLine("return global::Microsoft.AspNetCore.Http.Generated.GeneratedRouteBuilderExtensionsCore.MapCore(");
                    codeWriter.Indent++;
                    codeWriter.WriteLine("endpoints,");
                    // For `MapFallback` overloads that only take a delegate, provide the assumed default
                    // Otherwise, pass the pattern provided from the MapX invocation
                    if (endpoint.HttpMethod != "MapFallback" && endpoint.Operation.Arguments.Length != 2)
                    {
                        codeWriter.WriteLine("pattern,");
                    }
                    else
                    {
                        codeWriter.WriteLine($"{SymbolDisplay.FormatLiteral("{*path:nonfile}", true)},");
                    }
                    codeWriter.WriteLine("handler,");
                    codeWriter.WriteLine($"{endpoint.EmitVerb()},");
                    codeWriter.WriteLine("filePath,");
                    codeWriter.WriteLine("lineNumber);");
                    codeWriter.Indent--;
                    codeWriter.EndBlock();
                }

                return stringWriter.ToString();
            });

        var endpointHelpers = endpoints
            .Collect()
            .Select((endpoints, _) =>
            {
                var hasJsonBodyOrService = endpoints.Any(endpoint => endpoint.EmitterContext.HasJsonBodyOrService);
                var hasJsonBodyOrQuery = endpoints.Any(endpoint => endpoint.EmitterContext.HasJsonBodyOrQuery);
                var hasJsonBody = endpoints.Any(endpoint => endpoint.EmitterContext.HasJsonBody);
                var hasFormBody = endpoints.Any(endpoint => endpoint.EmitterContext.HasFormBody);
                var hasRouteOrQuery = endpoints.Any(endpoint => endpoint.EmitterContext.HasRouteOrQuery);
                var hasBindAsync = endpoints.Any(endpoint => endpoint.EmitterContext.HasBindAsync);
                var hasParsable = endpoints.Any(endpoint => endpoint.EmitterContext.HasParsable);
                var hasJsonResponse = endpoints.Any(endpoint => endpoint.EmitterContext.HasJsonResponse);
                var hasEndpointMetadataProvider = endpoints.Any(endpoint => endpoint.EmitterContext.HasEndpointMetadataProvider);
                var hasEndpointParameterMetadataProvider = endpoints.Any(endpoint => endpoint.EmitterContext.HasEndpointParameterMetadataProvider);
                var hasIResult = endpoints.Any(endpoint => endpoint.Response?.IsIResult == true);

                using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                using var codeWriter = new CodeWriter(stringWriter, baseIndent: 0);

                if (hasRouteOrQuery)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.ResolveFromRouteOrQueryMethod);
                }

                if (hasJsonResponse)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.WriteToResponseAsyncMethod);
                }

                if (hasJsonBody || hasJsonBodyOrService || hasJsonBodyOrQuery)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.TryResolveBodyAsyncMethod);
                }

                if (hasFormBody)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.TryResolveFormAsyncMethod);
                }

                if (hasBindAsync)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.BindAsyncMethod);
                }

                if (hasJsonBodyOrService)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.ResolveJsonBodyOrServiceMethod);
                }

                if (hasParsable)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.TryParseExplicitMethod);
                }

                if (hasIResult)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.ExecuteAsyncExplicitMethod);
                }

                if (hasEndpointMetadataProvider)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.PopulateEndpointMetadataMethod);
                }

                if (hasEndpointParameterMetadataProvider)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.PopulateEndpointParameterMetadataMethod);
                }

                return stringWriter.ToString();
            });

        var helperTypes = endpoints
            .Collect()
            .Select((endpoints, _) =>
            {
                var hasFormBody = endpoints.Any(endpoint => endpoint.EmitterContext.HasFormBody);
                var hasJsonBody = endpoints.Any(endpoint => endpoint.EmitterContext.HasJsonBody || endpoint.EmitterContext.HasJsonBodyOrService || endpoint.EmitterContext.HasJsonBodyOrQuery);
                var hasResponseMetadata = endpoints.Any(endpoint => endpoint.EmitterContext.HasResponseMetadata);
                var requiresPropertyAsParameterInfo = endpoints.Any(endpoint => endpoint.EmitterContext.RequiresPropertyAsParameterInfo);

                using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                using var codeWriter = new CodeWriter(stringWriter, baseIndent: 0);

                if (hasFormBody || hasJsonBody)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.AcceptsMetadataType);
                }

                if (hasResponseMetadata)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.ProducesResponseTypeMetadataType);
                }

                if (hasFormBody || hasJsonBody || hasResponseMetadata)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.ContentTypeConstantsType);
                }

                if (requiresPropertyAsParameterInfo)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.PropertyAsParameterInfoClass);
                }

                return stringWriter.ToString();
            });

        var thunksAndEndpoints = thunks.Collect().Combine(stronglyTypedEndpointDefinitions).Combine(endpointHelpers).Combine(helperTypes);

        context.RegisterSourceOutput(thunksAndEndpoints, (context, sources) =>
        {
            var (((thunks, endpointsCode), helperMethods), helperTypes) = sources;

            if (thunks.IsDefaultOrEmpty || string.IsNullOrEmpty(endpointsCode))
            {
                return;
            }

            var thunksCode = new StringBuilder();
            foreach (var thunk in thunks)
            {
                thunksCode.AppendLine(thunk);
            }

            var code = RequestDelegateGeneratorSources.GetGeneratedRouteBuilderExtensionsSource(
                genericThunks: string.Empty,
                thunks: thunksCode.ToString(),
                endpoints: endpointsCode,
                helperMethods: helperMethods ?? string.Empty,
                helperTypes: helperTypes ?? string.Empty);

            context.AddSource("GeneratedRouteBuilderExtensions.g.cs", code);
        });
    }
}
