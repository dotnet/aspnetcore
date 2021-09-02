// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Extension methods for adding <see cref="Endpoint.Metadata"/> that is
    /// meant to be consumed by OpenAPI libraries.
    /// </summary>
    public static class OpenApiDelegateEndpointConventionBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="ITagsMetadata"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <remarks>
        /// The OpenAPI specification supports a tags classification to categorize operations
        /// into related groups. These tags are typically included in the generated specification
        /// and are typically used to group operations by tags in the UI.
        /// </remarks>
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