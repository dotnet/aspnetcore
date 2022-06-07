// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;


/// <summary>
/// Specifies the type returned by default by controllers annotated with <see cref="ApiControllerAttribute"/>.
/// <para>
/// <see cref="Type"/> specifies the error model type associated with a <see cref="ProducesResponseTypeAttribute"/>
/// for a client error (HTTP Status Code 4xx) when no value is provided. When no value is specified, MVC assumes the
/// client error type to be <see cref="ProblemDetails"/>, if mapping client errors (<see cref="ApiBehaviorOptions.ClientErrorMapping"/>)
/// is used.
/// </para>
/// <para>
/// Use this <see cref="Attribute"/> to configure the default error type if your application uses a custom error type to respond.
/// </para>
/// </summary>
public interface IProducesErrorResponseMetadata
{
    /// <summary>
    /// Gets the optimistic return type of the action.
    /// </summary>
    Type? Type { get; }
}
