// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    /// <summary>
    /// A builder abstraction for configuring <see cref="HttpMessageHandler"/> instances.
    /// </summary>
    /// <remarks>
    /// The <see cref="HttpMessageHandlerBuilder"/> is registered in the service collection as
    /// a transient service. Callers should retrieve a new instance for each <see cref="HttpMessageHandler"/> to
    /// be created. Implementors should expect each instance to be used a single time.
    /// </remarks>
    public abstract class HttpMessageHandlerBuilder
    {
        /// <summary>
        /// Gets or sets the name of the <see cref="HttpClient"/> being created.
        /// </summary>
        /// <remarks>
        /// The <see cref="Name"/> is set by the <see cref="IHttpClientFactory"/> infrastructure
        /// and is public for unit testing purposes only. Setting the <see cref="Name"/> outside of
        /// testing scenarios may have unpredictable results.
        /// </remarks>
        public abstract string Name { get; set; }

        /// <summary>
        /// Gets or sets the primary <see cref="HttpMessageHandler"/>.
        /// </summary>
        public abstract HttpMessageHandler PrimaryHandler { get; set; }

        /// <summary>
        /// Gets a list of additional <see cref="DelegatingHandler"/> instances used to configure an
        /// <see cref="HttpClient"/> pipeline.
        /// </summary>
        public abstract IList<DelegatingHandler> AdditionalHandlers { get; }

        /// <summary>
        /// Gets an <see cref="IServiceProvider"/> which can be used to resolve services
        /// from the dependency injection container.
        /// </summary>
        /// <remarks>
        /// This property is sensitive to the value of 
        /// <see cref="HttpClientFactoryOptions.SuppressHandlerScope"/>. If <c>true</c> this
        /// property will be a reference to the application's root service provider. If <c>false</c>
        /// (default) this will be a reference to a scoped service provider that has the same
        /// lifetime as the handler being created.
        /// </remarks>
        public virtual IServiceProvider Services { get; }

        /// <summary>
        /// Creates an <see cref="HttpMessageHandler"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="HttpMessageHandler"/> built from the <see cref="PrimaryHandler"/> and
        /// <see cref="AdditionalHandlers"/>.
        /// </returns>
        public abstract HttpMessageHandler Build();

        protected internal static HttpMessageHandler CreateHandlerPipeline(HttpMessageHandler primaryHandler, IEnumerable<DelegatingHandler> additionalHandlers)
        {
            // This is similar to https://github.com/aspnet/AspNetWebStack/blob/master/src/System.Net.Http.Formatting/HttpClientFactory.cs#L58
            // but we don't want to take that package as a dependency.

            if (primaryHandler == null)
            {
                throw new ArgumentNullException(nameof(primaryHandler));
            }

            if (additionalHandlers == null)
            {
                throw new ArgumentNullException(nameof(additionalHandlers));
            }

            var additionalHandlersList = additionalHandlers as IReadOnlyList<DelegatingHandler> ?? additionalHandlers.ToArray();

            var next = primaryHandler;
            for (var i = additionalHandlersList.Count - 1; i >= 0; i--)
            {
                var handler = additionalHandlersList[i];
                if (handler == null)
                {
                    var message = Resources.FormatHttpMessageHandlerBuilder_AdditionalHandlerIsNull(nameof(additionalHandlers));
                    throw new InvalidOperationException(message);
                }

                // Checking for this allows us to catch cases where someone has tried to re-use a handler. That really won't
                // work the way you want and it can be tricky for callers to figure out.
                if (handler.InnerHandler != null)
                {
                    var message = Resources.FormatHttpMessageHandlerBuilder_AdditionHandlerIsInvalid(
                        nameof(DelegatingHandler.InnerHandler),
                        nameof(DelegatingHandler),
                        nameof(HttpMessageHandlerBuilder),
                        Environment.NewLine,
                        handler);
                    throw new InvalidOperationException(message);
                }

                handler.InnerHandler = next;
                next = handler;
            }

            return next;
        }
    }
}
