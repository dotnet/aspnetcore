// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 
/// </summary>
public static class ProblemDetailsConventionBuilderExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static TBuilder WithProblemDetails<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    => builder.WithMetadata(new ProblemDetailsResponseMetadata());
}
