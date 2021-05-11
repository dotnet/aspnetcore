// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Default implementation for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public class ApplicationBuilder : IApplicationBuilder
    {
        private const string ServerFeaturesKey = "server.Features";
        private const string ApplicationServicesKey = "application.Services";

        private readonly List<Func<RequestDelegate, RequestDelegate>> _components = new();

        /// <summary>
        /// Initializes a new instance of <see cref="ApplicationBuilder"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> for application services.</param>
        public ApplicationBuilder(IServiceProvider serviceProvider)
        {
            Properties = new Dictionary<string, object?>(StringComparer.Ordinal);
            ApplicationServices = serviceProvider;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ApplicationBuilder"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> for application services.</param>
        /// <param name="server">The server instance that hosts the application.</param>
        public ApplicationBuilder(IServiceProvider serviceProvider, object server)
            : this(serviceProvider)
        {
            SetProperty(ServerFeaturesKey, server);
        }

        private ApplicationBuilder(ApplicationBuilder builder)
        {
            Properties = new CopyOnWriteDictionary<string, object?>(builder.Properties, StringComparer.Ordinal);
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
            return this;
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

                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            };

            for (var c = _components.Count - 1; c >= 0; c--)
            {
                app = _components[c](app);
            }

            return app;
        }
    }
}
