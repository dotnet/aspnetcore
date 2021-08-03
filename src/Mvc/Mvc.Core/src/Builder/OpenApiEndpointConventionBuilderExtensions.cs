// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Extension methods for adding response type metadata to endpoints.
    /// </summary>
    public static class OpenApiEndpointConventionBuilderExtensions
    {
        private static readonly ExcludeFromDescriptionAttribute _excludeFromApiMetadataAttribute = new();

        /// <summary>
        /// Adds the <see cref="IExcludeFromDescriptionMetadata"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="MinimalActionEndpointConventionBuilder"/>.</param>
        /// <returns>A <see cref="MinimalActionEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder ExcludeFromDescription(this MinimalActionEndpointConventionBuilder builder)
        {
            builder.WithMetadata(_excludeFromApiMetadataAttribute);

            return builder;
        }

        /// <summary>
        /// Adds the <see cref="ProducesResponseTypeAttribute"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="builder">The <see cref="MinimalActionEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code. Defaults to StatusCodes.Status200OK.</param>
        /// <param name="contentType">The response content type. Defaults to "application/json".</param>
        /// <param name="additionalContentTypes">Additional response content types the endpoint produces for the supplied status code.</param>
        /// <returns>A <see cref="MinimalActionEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
#pragma warning disable RS0026
        public static MinimalActionEndpointConventionBuilder Produces<TResponse>(this MinimalActionEndpointConventionBuilder builder,
#pragma warning restore RS0026
            int statusCode = StatusCodes.Status200OK,
            string? contentType =  null,
            params string[] additionalContentTypes)
        {
            return Produces(builder, statusCode, typeof(TResponse), contentType, additionalContentTypes);
        }

        /// <summary>
        /// Adds the <see cref="ProducesResponseTypeAttribute"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="MinimalActionEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code.</param>
        /// <param name="responseType">The type of the response. Defaults to null.</param>
        /// <param name="contentType">The response content type. Defaults to "application/json" if responseType is not null, otherwise defaults to null.</param>
        /// <param name="additionalContentTypes">Additional response content types the endpoint produces for the supplied status code.</param>
        /// <returns>A <see cref="MinimalActionEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
#pragma warning disable RS0026
        public static MinimalActionEndpointConventionBuilder Produces(this MinimalActionEndpointConventionBuilder builder,
#pragma warning restore RS0026
            int statusCode,
            Type? responseType = null,
            string? contentType = null,
            params string[] additionalContentTypes)
        {
            if (responseType is Type && string.IsNullOrEmpty(contentType))
            {
                contentType = "application/json";
            }

            if (contentType is null)
            {
                builder.WithMetadata(new ProducesResponseTypeAttribute(responseType ?? typeof(void), statusCode));
                return builder;
            }

            builder.WithMetadata(new ProducesResponseTypeAttribute(responseType ?? typeof(void), statusCode, contentType, additionalContentTypes));

            return builder;
        }

        /// <summary>
        /// Adds the <see cref="ProducesResponseTypeAttribute"/> with a <see cref="ProblemDetails"/> type
        /// to <see cref="EndpointBuilder.Metadata"/> for all builders produced by <paramref name="builder"/>. 
        /// </summary>
        /// <param name="builder">The <see cref="MinimalActionEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code.</param>
        /// <param name="contentType">The response content type. Defaults to "application/problem+json".</param>
        /// <returns>A <see cref="MinimalActionEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder ProducesProblem(this MinimalActionEndpointConventionBuilder builder,
            int statusCode,
            string contentType = "application/problem+json")
        {
            return Produces<ProblemDetails>(builder, statusCode, contentType);
        }

        /// <summary>
        /// Adds the <see cref="ProducesResponseTypeAttribute"/> with a <see cref="HttpValidationProblemDetails"/> type
        /// to <see cref="EndpointBuilder.Metadata"/> for all builders produced by <paramref name="builder"/>. 
        /// </summary>
        /// <param name="builder">The <see cref="MinimalActionEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code. Defaults to StatusCodes.Status400BadRequest.</param>
        /// <param name="contentType">The response content type. Defaults to "application/problem+json".</param>
        /// <returns>A <see cref="MinimalActionEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder ProducesValidationProblem(this MinimalActionEndpointConventionBuilder builder,
            int statusCode = StatusCodes.Status400BadRequest,
            string contentType = "application/problem+json")
        {
            return Produces<HttpValidationProblemDetails>(builder, statusCode, contentType);
        }
    }
}
