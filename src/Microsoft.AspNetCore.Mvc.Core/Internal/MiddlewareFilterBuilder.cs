// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// Builds a middleware pipeline after receiving the pipeline from a pipeline provider
    /// </summary>
    public class MiddlewareFilterBuilder
    {
        // 'GetOrAdd' call on the dictionary is not thread safe and we might end up creating the pipeline more
        // once. To prevent this Lazy<> is used. In the worst case multiple Lazy<> objects are created for multiple
        // threads but only one of the objects succeeds in creating a pipeline.
        private readonly ConcurrentDictionary<Type, Lazy<RequestDelegate>> _pipelinesCache
            = new ConcurrentDictionary<Type, Lazy<RequestDelegate>>();
        private readonly MiddlewareFilterConfigurationProvider _configurationProvider;

        public IApplicationBuilder ApplicationBuilder { get; set; }

        public MiddlewareFilterBuilder(MiddlewareFilterConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
        }

        public RequestDelegate GetPipeline(Type configurationType)
        {
            // Build the pipeline only once. This is similar to how middlewares registered in Startup are constructed.

            var requestDelegate = _pipelinesCache.GetOrAdd(
                configurationType,
                key => new Lazy<RequestDelegate>(() => BuildPipeline(key)));

            return requestDelegate.Value;
        }

        private RequestDelegate BuildPipeline(Type middlewarePipelineProviderType)
        {
            if (ApplicationBuilder == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatMiddlewareFilterBuilder_NullApplicationBuilder(nameof(ApplicationBuilder)));
            }

            var nestedAppBuilder = ApplicationBuilder.New();

            // Get the 'Configure' method from the user provided type.
            var configureDelegate = _configurationProvider.CreateConfigureDelegate(middlewarePipelineProviderType);
            configureDelegate(nestedAppBuilder);

            // The middleware resource filter, after receiving the request executes the user configured middleware
            // pipeline. Since we want execution of the request to continue to later MVC layers (resource filters
            // or model binding), add a middleware at the end of the user provided pipeline which make sure to continue
            // this flow.
            // Example:
            // middleware filter -> user-middleware1 -> user-middleware2 -> end-middleware -> resource filters or model binding
            nestedAppBuilder.Run(async (httpContext) =>
            {
                var feature = httpContext.Features.Get<IMiddlewareFilterFeature>();
                if (feature == null)
                {
                    throw new InvalidOperationException(
                        Resources.FormatMiddlewareFilterBuilder_NoMiddlewareFeature(nameof(IMiddlewareFilterFeature)));
                }

                var resourceExecutionDelegate = feature.ResourceExecutionDelegate;

                var resourceExecutedContext = await resourceExecutionDelegate();

                // Ideally we want the experience of a middleware pipeline to behave the same as if it was registered,
                // in Startup. In this scenario an exception thrown in a middelware later in the pipeline gets propagated
                // back to earlier middleware.
                // So check if a later resource filter threw an exception and propagate that back to the middleware pipeline.
                if (!resourceExecutedContext.ExceptionHandled && resourceExecutedContext.Exception != null)
                {
                    throw resourceExecutedContext.Exception;
                }
            });

            return nestedAppBuilder.Build();
        }
    }
}
