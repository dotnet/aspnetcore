// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing;

internal sealed class RouteEndpointDataSource : EndpointDataSource
{
    private readonly List<RouteEntry> _routeEntries = new();
    private readonly IServiceProvider _applicationServices;
    private readonly bool _throwOnBadRequest;

    public RouteEndpointDataSource(IServiceProvider applicationServices, bool throwOnBadRequest)
    {
        _applicationServices = applicationServices;
        _throwOnBadRequest = throwOnBadRequest;
    }

    public ICollection<Action<EndpointBuilder>> AddEndpoint(
        RoutePattern pattern,
        Delegate routeHandler,
        IEnumerable<string>? httpMethods,
        bool isFallback)
    {
        RouteEntry entry = new()
        {
            RoutePattern = pattern,
            RouteHandler = routeHandler,
            HttpMethods = httpMethods,
            IsFallback = isFallback,
            Conventions = new ThrowOnAddAfterBuildCollection(),
        };

        _routeEntries.Add(entry);

        return entry.Conventions;
    }

    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            var endpoints = new List<RouteEndpoint>(_routeEntries.Count);
            foreach (var entry in _routeEntries)
            {
                endpoints.Add((RouteEndpoint)CreateRouteEndpointBuilder(entry).Build());
            }
            return endpoints;
        }
    }

    public override IReadOnlyList<RouteEndpoint> GetGroupedEndpoints(RouteGroupContext context)
    {
        var endpoints = new List<RouteEndpoint>(_routeEntries.Count);
        foreach (var entry in _routeEntries)
        {
            endpoints.Add((RouteEndpoint)CreateRouteEndpointBuilder(entry, context.Prefix, context.Conventions).Build());
        }
        return endpoints;
    }

    public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;

    // For testing
    internal RouteEndpointBuilder GetSingleRouteEndpointBuilder()
    {
        if (_routeEntries.Count != 1)
        {
            throw new InvalidOperationException($"There are {_routeEntries.Count} endpoints defined! This can only be called for a single endpoint.");
        }

        return CreateRouteEndpointBuilder(_routeEntries[0]);
    }

    [UnconditionalSuppressMessage("Trimmer", "IL2026",
        Justification = "We surface a RequireUnreferencedCode in the call to Map method adding this EndpointDataSource. " +
                        "The trimmer is unable to infer this.")]
    private RouteEndpointBuilder CreateRouteEndpointBuilder(RouteEntry entry, RoutePattern? groupPrefix = null, IReadOnlyList<Action<EndpointBuilder>>? groupConventions = null)
    {
        var pattern = RoutePatternFactory.Combine(groupPrefix, entry.RoutePattern);
        var handler = entry.RouteHandler;
        var displayName = pattern.RawText ?? pattern.DebuggerToString();

        // Methods defined in a top-level program are generated as statics so the delegate target will be null.
        // Inline lambdas are compiler generated method so they be filtered that way.
        if (GeneratedNameParser.TryParseLocalFunctionName(handler.Method.Name, out var endpointName)
            || !TypeHelper.IsCompilerGeneratedMethod(handler.Method))
        {
            endpointName ??= handler.Method.Name;
            displayName = $"{displayName} => {endpointName}";
        }

        if (entry.HttpMethods is not null)
        {
            // Prepends the HTTP method to the DisplayName produced with pattern + method name
            displayName = $"HTTP: {string.Join(", ", entry.HttpMethods)} {displayName}";
        }

        if (entry.IsFallback)
        {
            displayName = $"Fallback {displayName}";
        }

        RequestDelegate? factoryCreatedRequestDelegate = null;
        RequestDelegate redirectedRequestDelegate = context =>
        {
            if (factoryCreatedRequestDelegate is null)
            {
                throw new InvalidOperationException(Resources.RouteEndpointDataSource_RequestDelegateCannotBeCalledBeforeBuild);
            }

            return factoryCreatedRequestDelegate(context);
        };

        // The Map methods don't support customizing the order apart from using int.MaxValue to give MapFallback the lowest precedences.
        // Otherwise, we always use the default of 0 unless a convention changes it later.
        var order = entry.IsFallback ? int.MaxValue : 0;

        var builder = new RouteEndpointBuilder(redirectedRequestDelegate, pattern, order)
        {
            DisplayName = displayName,
            ApplicationServices = _applicationServices,
        };

        // We own EndpointBuilder.Metadata (in another assembly), so we know it's just a List.
        var metadata = (List<object>)builder.Metadata;

        // Add MethodInfo as first metadata item
        builder.Metadata.Add(handler.Method);

        if (entry.HttpMethods is not null)
        {
            builder.Metadata.Add(new HttpMethodMetadata(entry.HttpMethods));
        }

        // Apply group conventions before entry-specific conventions added to the RouteHandlerBuilder.
        if (groupConventions is not null)
        {
            foreach (var groupConvention in groupConventions)
            {
                groupConvention(builder);
            }
        }

        // Add delegate attributes as metadata before programmatic conventions.
        var attributes = handler.Method.GetCustomAttributes();
        if (attributes is not null)
        {
            metadata.AddRange(attributes);
        }

        entry.Conventions.HasBeenBuilt = true;
        foreach (var entrySpecificConvention in entry.Conventions)
        {
            entrySpecificConvention(builder);
        }

        // Let's see if any of the conventions added a filter before creating the RequestDelegate.
        List<Func<RouteHandlerContext, RouteHandlerFilterDelegate, RouteHandlerFilterDelegate>>? routeHandlerFilterFactories = null;

        foreach (var item in builder.Metadata)
        {
            if (item is RouteFilterMetadata filterMetadata)
            {
                routeHandlerFilterFactories ??= new();
                routeHandlerFilterFactories.AddRange(filterMetadata.FilterFactories);
            }
        }

        var routeParams = pattern.Parameters;
        var routeParamNames = new List<string>(routeParams.Count);
        foreach (var parameter in routeParams)
        {
            routeParamNames.Add(parameter.Name);
        }

        var factoryOptions = new RequestDelegateFactoryOptions
        {
            ServiceProvider = _applicationServices,
            RouteParameterNames = routeParamNames,
            ThrowOnBadRequest = _throwOnBadRequest,
            DisableInferBodyFromParameters = ShouldDisableInferredBodyParameters(entry.HttpMethods),
            EndpointMetadata = metadata,
            RouteHandlerFilterFactories = routeHandlerFilterFactories,
        };

        // We ignore the returned EndpointMetadata has been already populated since we passed in non-null EndpointMetadata.
        factoryCreatedRequestDelegate = RequestDelegateFactory.Create(entry.RouteHandler, factoryOptions).RequestDelegate;

        if (ReferenceEquals(builder.RequestDelegate, redirectedRequestDelegate))
        {
            // No convention has changed builder.RequestDelegate, so we can just replace it with the final version as an optimization.
            // We still set factoryRequestDelegate in case something is still referencing the redirected version of the RequestDelegate.
            builder.RequestDelegate = factoryCreatedRequestDelegate;
        }

        return builder;
    }

    private static bool ShouldDisableInferredBodyParameters(IEnumerable<string>? httpMethods)
    {
        static bool ShouldDisableInferredBodyForMethod(string method) =>
            // GET, DELETE, HEAD, CONNECT, TRACE, and OPTIONS normally do not contain bodies
            method.Equals(HttpMethods.Get, StringComparison.Ordinal) ||
            method.Equals(HttpMethods.Delete, StringComparison.Ordinal) ||
            method.Equals(HttpMethods.Head, StringComparison.Ordinal) ||
            method.Equals(HttpMethods.Options, StringComparison.Ordinal) ||
            method.Equals(HttpMethods.Trace, StringComparison.Ordinal) ||
            method.Equals(HttpMethods.Connect, StringComparison.Ordinal);

        // If the endpoint accepts any kind of request, we should still infer parameters can come from the body.
        if (httpMethods is null)
        {
            return false;
        }

        foreach (var method in httpMethods)
        {
            if (ShouldDisableInferredBodyForMethod(method))
            {
                // If the route handler was mapped explicitly to handle an HTTP method that does not normally have a request body,
                // we assume any invocation of the handler will not have a request body no matter what other HTTP methods it may support.
                return true;
            }
        }

        return false;
    }

    private struct RouteEntry
    {
        public RoutePattern RoutePattern { get; init; }
        public Delegate RouteHandler { get; init; }
        public IEnumerable<string>? HttpMethods { get; init; }
        public bool IsFallback { get; init; }
        public ThrowOnAddAfterBuildCollection Conventions { get; init; }
    }

    // This private class is only exposed to internal code via ICollection<Action<EndpointBuilder>> in RouteEndpointBuilder where only Add is called.
    private sealed class ThrowOnAddAfterBuildCollection : List<Action<EndpointBuilder>>, ICollection<Action<EndpointBuilder>>
    {
        public bool HasBeenBuilt { get; set; }

        void ICollection<Action<EndpointBuilder>>.Add(Action<EndpointBuilder> convention)
        {
            if (HasBeenBuilt)
            {
                throw new InvalidOperationException(Resources.RouteEndpointDataSource_ConventionsCannotBeModifiedAfterBuild);
            }

            Add(convention);
        }
    }
}
