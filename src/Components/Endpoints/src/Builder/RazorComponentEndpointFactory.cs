// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Discovery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class RazorComponentEndpointFactory
{
    private static readonly HttpMethodMetadata HttpMethodsMetadata = new(new[] { HttpMethods.Get, HttpMethods.Post });

#pragma warning disable CA1822 // It's a singleton
    internal void AddEndpoints(
#pragma warning restore CA1822 // It's a singleton
        List<Endpoint> endpoints,
        Type rootComponent,
        PageComponentInfo pageDefinition,
        IReadOnlyList<Action<EndpointBuilder>> conventions,
        IReadOnlyList<Action<EndpointBuilder>> finallyConventions)
    {
        // We do not provide a way to establish the order or the name for the page routes.
        // Order is not supported in our client router.
        // Name is only relevant for Link generation, which we don't support either.
        var builder = new RouteEndpointBuilder(
            null,
            RoutePatternFactory.Parse(pageDefinition.Route),
            order: 0);

        // All attributes defined for the type are included as metadata.
        foreach (var attribute in pageDefinition.Metadata)
        {
            builder.Metadata.Add(attribute);
        }

        // We do not support link generation, so explicitly opt-out.
        builder.Metadata.Add(new SuppressLinkGenerationMetadata());
        builder.Metadata.Add(HttpMethodsMetadata);
        builder.Metadata.Add(new ComponentTypeMetadata(pageDefinition.Type));
        builder.Metadata.Add(new RootComponentMetadata(rootComponent));

        foreach (var convention in conventions)
        {
            convention(builder);
        }

        foreach (var finallyConvention in finallyConventions)
        {
            finallyConvention(builder);
        }

        // Always override the order, since our client router does not support it.
        builder.Order = 0;

        // The display name is for debug purposes by endpoint routing.
        builder.DisplayName = $"{builder.RoutePattern.RawText} ({pageDefinition.DisplayName})";

        builder.RequestDelegate = httpContext => new RazorComponentEndpointInvoker(httpContext, rootComponent, pageDefinition.Type).RenderComponent();

        endpoints.Add(builder.Build());
    }
}
