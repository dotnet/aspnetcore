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
    /// The mutable <see cref="Builder.EndpointBuilder"/> used to assist in the creation of the <see cref="RequestDelegateResult.RequestDelegate"/>.
    /// This is primarily used to run <see cref="EndpointBuilder.FilterFactories"/> and populate inferred <see cref="EndpointBuilder.Metadata"/>.
    /// The <see cref="EndpointBuilder.RequestDelegate"/> must be <see langword="null"/>. After the call to <see cref="RequestDelegateFactory.Create(Delegate, RequestDelegateFactoryOptions?)"/>,
    /// <see cref="EndpointBuilder.RequestDelegate"/> will be the same as <see cref="RequestDelegateResult.RequestDelegate"/>.
    /// </summary>
    /// <remarks>
    /// Any metadata already in <see cref="EndpointBuilder.Metadata"/> will be included in <see cref="RequestDelegateResult.EndpointMetadata" /> <b>before</b>
    /// most metadata inferred during creation of the <see cref="RequestDelegateResult.RequestDelegate"/> and <b>before</b> any metadata provided by types in
    /// the delegate signature that implement <see cref="IEndpointMetadataProvider" /> or <see cref="IEndpointParameterMetadataProvider" />. The exception to this general rule is the
    /// <see cref="IAcceptsMetadata"/> that <see cref="RequestDelegateFactory.Create(Delegate, RequestDelegateFactoryOptions?)"/> infers automatically
    /// without any custom metadata providers which instead is inserted at the start to give it lower precedence. Custom metadata providers can choose to
    /// insert their metadata at the start to give lower precedence, but this is unusual.
    /// </remarks>
    public EndpointBuilder? EndpointBuilder { get; init; }
}
