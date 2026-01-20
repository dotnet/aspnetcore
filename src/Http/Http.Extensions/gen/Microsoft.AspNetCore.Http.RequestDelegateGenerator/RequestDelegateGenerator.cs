// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator;

[Generator]
public sealed partial class RequestDelegateGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var endpointsWithDiagnostics = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: IsEndpointInvocation,
            transform: TransformEndpoint)
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
            .Select((endpointWithLocations, _) => EmitInterceptorDefinition(endpointWithLocations));

        var httpVerbs = endpoints
            .Collect()
            .Select((endpoints, _) => EmitHttpVerbs(endpoints));

        var endpointHelpers = endpoints
            .Collect()
            .Select((endpoints, _) => EmitEndpointHelpers(endpoints));

        var helperTypes = endpoints
            .Collect()
            .Select((endpoints, _) => EmitHelperTypes(endpoints));

        var endpointsAndHelpers = interceptorDefinitions.Collect().Combine(endpointHelpers).Combine(httpVerbs).Combine(helperTypes);

        context.RegisterSourceOutput(endpointsAndHelpers, (context, sources) =>
        {
            var (((endpointsCode, helperMethods), httpVerbs), helperTypes) = sources;
            Emit(context, endpointsCode, helperMethods ?? string.Empty, httpVerbs, helperTypes ?? string.Empty);
        });
    }
}
