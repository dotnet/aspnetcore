// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
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
    /// The mutable initial endpoint metadata to add as part of the creation of the <see cref="RequestDelegateResult.RequestDelegate"/>. In most cases,
    /// this should come from <see cref="EndpointBuilder.Metadata"/>.
    /// </summary>
    /// <remarks>
    /// This metadata will be included in <see cref="RequestDelegateResult.EndpointMetadata" /> <b>before</b> most metadata inferred during creation of the
    /// <see cref="RequestDelegateResult.RequestDelegate"/> and <b>before</b> any metadata provided by types in the delegate signature that implement
    /// <see cref="IEndpointMetadataProvider" /> or <see cref="IEndpointParameterMetadataProvider" />. The exception to this general rule is the
    /// <see cref="IAcceptsMetadata"/> that <see cref="RequestDelegateFactory.Create(Delegate, RequestDelegateFactoryOptions?)"/> infers automatically
    /// without any custom metadata providers which instead is inserted at the start to give it lower precedence. Custom metadata providers can choose to
    /// insert their metadata at the start to give lower precedence, but this is unusual.
    /// </remarks>
    public IList<object>? EndpointMetadata { get; init; }

    // TODO: Add a RouteEndpointBuilder property and remove the EndpointMetadata property. Then do the same in RouteHandlerContext, EndpointMetadataContext
    // and EndpointParameterMetadataContext. This will allow seeing the entire route pattern if the caller chooses to allow it.
    // We'll probably want to add the RouteEndpointBuilder constructor without a RequestDelegate back and make it public too.
}
