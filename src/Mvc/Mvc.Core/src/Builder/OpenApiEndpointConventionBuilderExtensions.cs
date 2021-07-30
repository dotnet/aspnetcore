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
        /// <summary>
        /// Adds metadata to support suppressing OpenAPI documentation from
        /// being generated for this endpoint.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder SuppressApi(this IEndpointConventionBuilder builder)
        {
            builder.WithMetadata(new SuppressApiMetadata());

            return builder;
        }

        /// <summary>
        /// Adds metadata indicating the type of response an endpoint produces.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code. Defatuls to StatusCodes.Status200OK.</param>
        /// <param name="contentType">The response content type. Defaults to "application/json"</param>
        /// <param name="additionalContentTypes">Additional response content types the endpoint produces for the supplied status code.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder Produces<TResponse>(this IEndpointConventionBuilder builder,
            int statusCode = StatusCodes.Status200OK,
            string? contentType = "application/json",
            params string[] additionalContentTypes)
        {
            return Produces(builder, statusCode, typeof(TResponse), contentType, additionalContentTypes);
        }

        /// <summary>
        /// Adds metadata indicating the type of response an endpoint produces.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code. Defatuls to StatusCodes.Status200OK.</param>
        /// <param name="responseType">The type of the response. Defaults to null.</param>
        /// <param name="contentType">The response content type. Defaults to "application/json" if responseType is not null, otherwise defaults to null.</param>
        /// <param name="additionalContentTypes">Additional response content types the endpoint produces for the supplied status code.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder Produces(this IEndpointConventionBuilder builder,
            int statusCode = StatusCodes.Status200OK,
            Type? responseType = null,
            string? contentType = null,
            params string[] additionalContentTypes)
        {
            if (responseType is Type && string.IsNullOrEmpty(contentType))
            {
                contentType = "application/json";
            }

            builder.WithMetadata(new ProducesResponseTypeAttribute(responseType ?? typeof(void), statusCode, contentType, additionalContentTypes));

            return builder;
        }

        /// <summary>
        /// Adds metadata indicating that the endpoint produces a Problem Details response.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code. Defatuls to StatusCodes.Status500InternalServerError.</param>
        /// <param name="contentType">The response content type. Defaults to "application/problem+json".</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder ProducesProblem(this IEndpointConventionBuilder builder,
            int statusCode = StatusCodes.Status500InternalServerError,
            string contentType = "application/problem+json")
        {
            return Produces<ProblemDetails>(builder, statusCode, contentType);
        }

        /// <summary>
        /// Adds metadata indicating that the endpoint produces a ProblemDetails response for validation errors.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code. Defatuls to StatusCodes.Status400BadRequest.</param>
        /// <param name="contentType">The response content type. Defaults to "application/problem+json".</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder ProducesValidationProblem(this IEndpointConventionBuilder builder,
            int statusCode = StatusCodes.Status400BadRequest,
            string contentType = "application/problem+json")
        {
            return Produces<HttpValidationProblemDetails>(builder, statusCode, contentType);
        }
    }
}