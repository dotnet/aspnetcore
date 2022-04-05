// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Options for controlling the behavior of the <see cref="RequestDelegate" /> when created using <see cref="RequestDelegateFactory" />.
/// </summary>
public sealed class RequestDelegateFactoryOptions
{
    /// <summary>
    /// The <see cref="IServiceProvider"/> instance used to access application services.
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
    public bool DisableInferBodyFromParameters { get; init; }

    /// <summary>
    /// The list of filters that must run in the pipeline for a given route handler.
    /// </summary>
    public IReadOnlyList<Func<RouteHandlerContext, RouteHandlerFilterDelegate, RouteHandlerFilterDelegate>>? RouteHandlerFilterFactories { get; init; }

    /// <summary>
    /// The default endpoint metadata to add as part of the creation of the <see cref="RequestDelegateResult.RequestDelegate"/>.
    /// </summary>
    /// <remarks>
    /// This metadata will be included in <see cref="RequestDelegateResult.EndpointMetadata" /> after any metadata inferred during creation of the
    /// <see cref="RequestDelegateResult.RequestDelegate"/> but before any metadata provided by types in the delegate signature that implement
    /// <see cref="IEndpointMetadataProvider" /> or <see cref="IEndpointParameterMetadataProvider" />.
    /// </remarks>
    public IReadOnlyList<object>? DefaultEndpointMetadata { get; init; }
}
