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
        IEnumerable<object>? initialEndpointMetadata,
        bool disableInferFromBodyParameters)
    {
        RouteEntry entry = new()
        {
            RoutePattern = pattern,
            RouteHandler = routeHandler,
            InitialEndpointMetadata = initialEndpointMetadata,
            DisableInferFromBodyParameters = disableInferFromBodyParameters,
            Conventions = new()
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

        RequestDelegate? factoryCreatedRequestDelegate = null;
        RequestDelegate redirectedRequestDelegate = context =>
        {
            if (factoryCreatedRequestDelegate is null)
            {
                throw new InvalidOperationException(Resources.RouteEndpointDataSource_RequestDelegateCannotBeCalledBeforeBuild);
            }

            return factoryCreatedRequestDelegate(context);
        };

        // The Map methods don't support customizing the order, so we always use the default of 0 unless a convention changes it later.
        var builder = new RouteEndpointBuilder(redirectedRequestDelegate, pattern, order: 0)
        {
            DisplayName = displayName,
            ApplicationServices = _applicationServices,
        };

        // We own EndpointBuilder.Metadata (in another assembly), so we know it's just a List.
        var metadata = (List<object>)builder.Metadata;

        // Add MethodInfo as first metadata item
        builder.Metadata.Add(handler.Method);

        if (entry.InitialEndpointMetadata is not null)
        {
            metadata.AddRange(entry.InitialEndpointMetadata);
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
            DisableInferBodyFromParameters = entry.DisableInferFromBodyParameters,
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

    private struct RouteEntry
    {
        public RoutePattern RoutePattern { get; init; }
        public Delegate RouteHandler { get; init; }
        public IEnumerable<object>? InitialEndpointMetadata { get; init; }
        public bool DisableInferFromBodyParameters { get; init; }
        public ThrowOnAddAfterBuildCollection Conventions { get; init; }
    }

    // This private class is only exposed to internal code via ICollection<Action<EndpointBuilder>> in RouteEndpointBuilder where only Add is called.
    private class ThrowOnAddAfterBuildCollection : List<Action<EndpointBuilder>>, ICollection<Action<EndpointBuilder>>
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
