// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel.Emitters;
using Microsoft.CodeAnalysis;
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
                if (operation.IsValidOperation(wellKnownTypes, out var invocationOperation))
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

        var interceptorDefinitions = endpoints
            .GroupWith((endpoint) => endpoint.InterceptableLocation, EndpointDelegateComparer.Instance)
            .Select((endpointWithLocations, _) =>
            {
                var endpoint = endpointWithLocations.Source;
                using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                using var codeWriter = new CodeWriter(stringWriter, baseIndent: 2);
                foreach (var location in endpointWithLocations.Elements)
                {
#pragma warning disable RSEXPERIMENTAL002 // Experimental interceptable location API
                    codeWriter.WriteLine(location.GetInterceptsLocationAttributeSyntax());
#pragma warning restore RSEXPERIMENTAL002
                }
                codeWriter.WriteLine($"internal static RouteHandlerBuilder {endpoint.HttpMethod}{endpointWithLocations.Index}(");
                codeWriter.Indent++;
                codeWriter.WriteLine("this IEndpointRouteBuilder endpoints,");
                // MapFallback overloads that only take a delegate do not need a pattern argument
                if (endpoint.HttpMethod != "MapFallback" || endpoint.Operation.Arguments.Length != 2)
                {
                    codeWriter.WriteLine(@"[StringSyntax(""Route"")] string pattern,");
                }
                // MapMethods overloads define an additional `httpMethods` parameter
                if (endpoint.HttpMethod == "MapMethods")
                {
                    codeWriter.WriteLine("IEnumerable<string> httpMethods,");
                }
                codeWriter.WriteLine("Delegate handler)");
                codeWriter.Indent--;
                codeWriter.StartBlock();
                codeWriter.WriteLine("MetadataPopulator populateMetadata = (methodInfo, options) =>");
                codeWriter.StartBlock();
                codeWriter.WriteLine(@"Debug.Assert(options != null, ""RequestDelegateFactoryOptions not found."");");
                codeWriter.WriteLine(@"Debug.Assert(options.EndpointBuilder != null, ""EndpointBuilder not found."");");
                codeWriter.WriteLine($"options.EndpointBuilder.Metadata.Add(new {RequestDelegateGeneratorSources.GeneratedCodeConstructor});");
                endpoint.EmitEndpointMetadataPopulation(codeWriter);
                codeWriter.WriteLine("return new RequestDelegateMetadataResult { EndpointMetadata = options.EndpointBuilder.Metadata.AsReadOnly() };");
                codeWriter.EndBlockWithSemicolon();
                codeWriter.WriteLine("RequestDelegateFactoryFunc createRequestDelegate = (del, options, inferredMetadataResult) =>");
                codeWriter.StartBlock();
                codeWriter.WriteLine(@"Debug.Assert(options != null, ""RequestDelegateFactoryOptions not found."");");
                codeWriter.WriteLine(@"Debug.Assert(options.EndpointBuilder != null, ""EndpointBuilder not found."");");
                codeWriter.WriteLine(@"Debug.Assert(options.EndpointBuilder.ApplicationServices != null, ""ApplicationServices not found."");");
                codeWriter.WriteLine(@"Debug.Assert(options.EndpointBuilder.FilterFactories != null, ""FilterFactories not found."");");
                codeWriter.WriteLine($"var handler = Cast(del, {endpoint.EmitHandlerDelegateType()} => throw null!);");
                codeWriter.WriteLine("EndpointFilterDelegate? filteredInvocation = null;");
                codeWriter.WriteLine("var serviceProvider = options.ServiceProvider ?? options.EndpointBuilder.ApplicationServices;");
                endpoint.EmitLoggingPreamble(codeWriter);
                endpoint.EmitJsonPreparation(codeWriter);
                endpoint.EmitRouteOrQueryResolver(codeWriter);
                endpoint.EmitJsonBodyOrServiceResolver(codeWriter);
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
                codeWriter.EndBlockWithSemicolon();
                codeWriter.WriteLine($"var castHandler = Cast(handler, {endpoint.EmitHandlerDelegateType()} => throw null!);");
                codeWriter.WriteLine("return MapCore(");
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
                codeWriter.WriteLine("populateMetadata,");
                codeWriter.WriteLine("createRequestDelegate,");
                codeWriter.WriteLine("castHandler.Method);");
                codeWriter.Indent--;
                codeWriter.EndBlock();
                return stringWriter.ToString();
            });

        var httpVerbs = endpoints
            .Collect()
            .Select((endpoints, _) =>
            {
                return endpoints
                    .Distinct(EndpointHttpMethodComparer.Instance)
                    .Select(endpoint => endpoint.EmitterContext.HttpMethod!)
                    .Where(verb => verb is not null)
                    .ToImmutableHashSet();
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
                var hasEndpointMetadataProvider = endpoints.Any(endpoint => endpoint.EmitterContext.HasEndpointMetadataProvider);
                var hasEndpointParameterMetadataProvider = endpoints.Any(endpoint => endpoint.EmitterContext.HasEndpointParameterMetadataProvider);
                var hasIResult = endpoints.Any(endpoint => endpoint.Response?.IsIResult == true);

                using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                using var codeWriter = new CodeWriter(stringWriter, baseIndent: 0);

                if (hasRouteOrQuery)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.ResolveFromRouteOrQueryMethod);
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
                var requiresParameterBindingMetadataClass = endpoints.Any(endpoint => endpoint.EmitterContext.RequiresParameterBindingMetadataClass);

                using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                using var codeWriter = new CodeWriter(stringWriter, baseIndent: 0);

                if (hasFormBody)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.AntiforgeryMetadataType);
                }

                if (hasFormBody || hasJsonBody || hasResponseMetadata)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.ContentTypeConstantsType);
                }

                if (requiresPropertyAsParameterInfo)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.PropertyAsParameterInfoClass);
                }

                if (requiresParameterBindingMetadataClass)
                {
                    codeWriter.WriteLine(RequestDelegateGeneratorSources.ParameterBindingMetadataClass);
                }

                return stringWriter.ToString();
            });

        var endpointsAndHelpers = interceptorDefinitions.Collect().Combine(endpointHelpers).Combine(httpVerbs).Combine(helperTypes);

        context.RegisterSourceOutput(endpointsAndHelpers, (context, sources) =>
        {
            var (((endpointsCode, helperMethods), httpVerbs), helperTypes) = sources;
            if (endpointsCode.IsDefaultOrEmpty)
            {
                return;
            }
            using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            using var codeWriter = new CodeWriter(stringWriter, baseIndent: 0);
            foreach (var endpoint in endpointsCode)
            {
                codeWriter.WriteLine(endpoint);
            }
            var code = RequestDelegateGeneratorSources.GetGeneratedRouteBuilderExtensionsSource(
                endpoints: stringWriter.ToString(),
                helperMethods: helperMethods ?? string.Empty,
                helperTypes: helperTypes ?? string.Empty,
                verbs: httpVerbs);

            context.AddSource("GeneratedRouteBuilderExtensions.g.cs", code);
        });
    }
}
