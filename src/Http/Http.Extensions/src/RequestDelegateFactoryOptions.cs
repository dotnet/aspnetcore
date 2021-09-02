// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Options for controlling the behavior of the <see cref="RequestDelegate" /> when created using <see cref="RequestDelegateFactory" />.
    /// </summary>
    public sealed class RequestDelegateFactoryOptions
    {
        /// <summary>
        /// The <see cref="IServiceProvider"/> instance used to detect if handler parameters are services.
        /// </summary>
        public IServiceProvider? ServiceProvider { get; init; }

        /// <summary>
        /// The list of route parameter names that are specified for this handler.
        /// </summary>
        public IEnumerable<string>? RouteParameterNames { get; init; }

        /// <summary>
        /// Controls whether the <see cref="RequestDelegate"/> should throw a <see cref="BadHttpRequestException"/> in addition to
        /// writing a <see cref="LogLevel.Debug"/> log when handling invalid requests.
        /// </summary>
        public bool ThrowOnBadRequest { get; init; }

        /// <summary>
        /// Prevent the <see cref="RequestDelegateFactory" /> from inferring a parameter should be bound from the request body without an attribute that implements <see cref="IFromBodyMetadata"/>.
        /// </summary>
        public bool DisableInferredBody { get; set; }
    }
}
