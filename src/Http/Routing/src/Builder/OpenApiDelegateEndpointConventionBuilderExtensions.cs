// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for adding response type metadata to endpoints.
    /// </summary>
    public static class OpenApiDelegateEndpointConventionBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="ITagsMetadata"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="DelegateEndpointConventionBuilder"/>.</param>
        /// <param name="tags">A collection of tags to be associated with the endpoint.</param>
        /// <returns>A <see cref="DelegateEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder WithTags(this DelegateEndpointConventionBuilder builder, params string[] tags)
        {
            builder.WithMetadata(new TagsAttribute(tags));
            return builder;
        }
    }
}