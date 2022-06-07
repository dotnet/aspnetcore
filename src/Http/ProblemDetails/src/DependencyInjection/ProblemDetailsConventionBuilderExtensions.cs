// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Http.ProblemDetails.DependencyInjection;

public static class ProblemDetailsConventionBuilderExtensions
{
    public static TBuilder WithProblemDetails<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    => builder.WithMetadata(new TagsAttribute(tags));
}
