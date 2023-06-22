// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Default implementation for <see cref="IApplicationBuilder"/>.
/// </summary>
[DebuggerDisplay("Middleware = {MiddlewareCount}")]
[DebuggerTypeProxy(typeof(ApplicationBuilderDebugView))]
public partial class ApplicationBuilder : IApplicationBuilder
{
    private const string ServerFeaturesKey = "server.Features";
    private const string ApplicationServicesKey = "application.Services";
    private const string MiddlewareDescriptionsKey = "__MiddlewareDescriptions";
    private const string RequestUnhandledKey = "__RequestUnhandled";

    private readonly List<Func<RequestDelegate, RequestDelegate>> _components = new();
    private readonly List<string>? _descriptions;
    private readonly IDebugger _debugger;

    /// <summary>
    /// Initializes a new instance of <see cref="ApplicationBuilder"/>.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> for application services.</param>
    public ApplicationBuilder(IServiceProvider serviceProvider) : this(serviceProvider, new FeatureCollection())
    {
    }

    private int MiddlewareCount => _components.Count;

    /// <summary>
    /// Initializes a new instance of <see cref="ApplicationBuilder"/>.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> for application services.</param>
    /// <param name="server">The server instance that hosts the application.</param>
    public ApplicationBuilder(IServiceProvider serviceProvider, object server)
    {
        Properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        ApplicationServices = serviceProvider;

        SetProperty(ServerFeaturesKey, server);

        // IDebugger service can optionally be added by tests to simulate the debugger being attached.
        _debugger = (IDebugger?)serviceProvider?.GetService(typeof(IDebugger)) ?? DebuggerWrapper.Instance;

        if (_debugger.IsAttached)
        {
            _descriptions = new();
            // Add component descriptions collection to properties so debugging tools can display
            // a list of configured middleware for an application.
            SetProperty(MiddlewareDescriptionsKey, _descriptions);
        }
    }

    private ApplicationBuilder(ApplicationBuilder builder)
    {
        Properties = new CopyOnWriteDictionary<string, object?>(builder.Properties, StringComparer.Ordinal);
        _debugger = builder._debugger;
        if (_debugger.IsAttached)
        {
            _descriptions = new();
        }
    }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> for application services.
    /// </summary>
    public IServiceProvider ApplicationServices
    {
        get
        {
            return GetProperty<IServiceProvider>(ApplicationServicesKey)!;
        }
        set
        {
            SetProperty<IServiceProvider>(ApplicationServicesKey, value);
        }
    }

    /// <summary>
    /// Gets the <see cref="IFeatureCollection"/> for server features.
    /// </summary>
    /// <remarks>
    /// An empty collection is returned if a server wasn't specified for the application builder.
    /// </remarks>
    public IFeatureCollection ServerFeatures
    {
        get
        {
            return GetProperty<IFeatureCollection>(ServerFeaturesKey)!;
        }
    }

    /// <summary>
    /// Gets a set of properties for <see cref="ApplicationBuilder"/>.
    /// </summary>
    public IDictionary<string, object?> Properties { get; }

    private T? GetProperty<T>(string key)
    {
        return Properties.TryGetValue(key, out var value) ? (T?)value : default(T);
    }

    private void SetProperty<T>(string key, T value)
    {
        Properties[key] = value;
    }

    /// <summary>
    /// Adds the middleware to the application request pipeline.
    /// </summary>
    /// <param name="middleware">The middleware.</param>
    /// <returns>An instance of <see cref="IApplicationBuilder"/> after the operation has completed.</returns>
    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        _components.Add(middleware);
        _descriptions?.Add(CreateMiddlewareDescription(middleware));

        return this;
    }

    private static string CreateMiddlewareDescription(Func<RequestDelegate, RequestDelegate> middleware)
    {
        if (middleware.Target != null)
        {
            // To IApplicationBuilder, middleware is just a func. Getting a good description is hard.
            // Inspect the incoming func and attempt to resolve it back to a middleware type if possible.
            // UseMiddlewareExtensions adds middleware via a method with the name CreateMiddleware.
            // If this pattern is matched, then ToString on the target returns the middleware type name.
            if (middleware.Method.Name == "CreateMiddleware")
            {
                return middleware.Target.ToString()!;
            }

            return middleware.Target.GetType().FullName + "." + middleware.Method.Name;
        }

        return middleware.Method.Name.ToString();
    }

    /// <summary>
    /// Creates a copy of this application builder.
    /// <para>
    /// The created clone has the same properties as the current instance, but does not copy
    /// the request pipeline.
    /// </para>
    /// </summary>
    /// <returns>The cloned instance.</returns>
    public IApplicationBuilder New()
    {
        return new ApplicationBuilder(this);
    }

    /// <summary>
    /// Produces a <see cref="RequestDelegate"/> that executes added middlewares.
    /// </summary>
    /// <returns>The <see cref="RequestDelegate"/>.</returns>
    public RequestDelegate Build()
    {
        RequestDelegate app = context =>
        {
            // If we reach the end of the pipeline, but we have an endpoint, then something unexpected has happened.
            // This could happen if user code sets an endpoint, but they forgot to add the UseEndpoint middleware.
            var endpoint = context.GetEndpoint();
            var endpointRequestDelegate = endpoint?.RequestDelegate;
            if (endpointRequestDelegate != null)
            {
                var message =
                    $"The request reached the end of the pipeline without executing the endpoint: '{endpoint!.DisplayName}'. " +
                    $"Please register the EndpointMiddleware using '{nameof(IApplicationBuilder)}.UseEndpoints(...)' if using " +
                    $"routing.";
                throw new InvalidOperationException(message);
            }

            // Flushing the response and calling through to the next middleware in the pipeline is
            // a user error, but don't attempt to set the status code if this happens. It leads to a confusing
            // behavior where the client response looks fine, but the server side logic results in an exception.
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }

            // Communicates to higher layers that the request wasn't handled by the app pipeline.
            context.Items[RequestUnhandledKey] = true;

            return Task.CompletedTask;
        };

        for (var c = _components.Count - 1; c >= 0; c--)
        {
            app = _components[c](app);
        }

        return app;
    }

    private sealed class ApplicationBuilderDebugView(ApplicationBuilder applicationBuilder)
    {
        private readonly ApplicationBuilder _applicationBuilder = applicationBuilder;

        public IServiceProvider ApplicationServices => _applicationBuilder.ApplicationServices;
        public IDictionary<string, object?> Properties => _applicationBuilder.Properties;
        public IFeatureCollection ServerFeatures => _applicationBuilder.ServerFeatures;
        public IList<string>? Middleware
        {
            get
            {
                if (_applicationBuilder.Properties.TryGetValue("__MiddlewareDescriptions", out var value) &&
                    value is IList<string> descriptions)
                {
                    return descriptions;
                }

                throw new NotSupportedException("Unable to get configured middleware.");
            }
        }
    }
}
