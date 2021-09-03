// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Extension methods for adding response type metadata to endpoints.
    /// </summary>
    public static class OpenApiEndpointConventionBuilderExtensions
    {
        private static readonly ExcludeFromDescriptionAttribute _excludeFromDescriptionMetadataAttribute = new();

        /// <summary>
        /// Adds the <see cref="IExcludeFromDescriptionMetadata"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="DelegateEndpointConventionBuilder"/>.</param>
        /// <returns>A <see cref="DelegateEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder ExcludeFromDescription(this DelegateEndpointConventionBuilder builder)
        {
            builder.WithMetadata(_excludeFromDescriptionMetadataAttribute);

            return builder;
        }

        /// <summary>
        /// Adds the <see cref="ProducesResponseTypeAttribute"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="builder">The <see cref="DelegateEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code. Defaults to StatusCodes.Status200OK.</param>
        /// <param name="contentType">The response content type. Defaults to "application/json".</param>
        /// <param name="additionalContentTypes">Additional response content types the endpoint produces for the supplied status code.</param>
        /// <returns>A <see cref="DelegateEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
#pragma warning disable RS0026
        public static DelegateEndpointConventionBuilder Produces<TResponse>(this DelegateEndpointConventionBuilder builder,
#pragma warning restore RS0026
            int statusCode = StatusCodes.Status200OK,
            string? contentType = null,
            params string[] additionalContentTypes)
        {
            return Produces(builder, statusCode, typeof(TResponse), contentType, additionalContentTypes);
        }

        /// <summary>
        /// Adds the <see cref="ProducesResponseTypeAttribute"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="DelegateEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code.</param>
        /// <param name="responseType">The type of the response. Defaults to null.</param>
        /// <param name="contentType">The response content type. Defaults to "application/json" if responseType is not null, otherwise defaults to null.</param>
        /// <param name="additionalContentTypes">Additional response content types the endpoint produces for the supplied status code.</param>
        /// <returns>A <see cref="DelegateEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
#pragma warning disable RS0026
        public static DelegateEndpointConventionBuilder Produces(this DelegateEndpointConventionBuilder builder,
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
        /// <param name="builder">The <see cref="DelegateEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code.</param>
        /// <param name="contentType">The response content type. Defaults to "application/problem+json".</param>
        /// <returns>A <see cref="DelegateEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder ProducesProblem(this DelegateEndpointConventionBuilder builder,
            int statusCode,
            string? contentType = null)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = "application/problem+json";
            }

            return Produces<ProblemDetails>(builder, statusCode, contentType);
        }

        /// <summary>
        /// Adds the <see cref="ProducesResponseTypeAttribute"/> with a <see cref="HttpValidationProblemDetails"/> type
        /// to <see cref="EndpointBuilder.Metadata"/> for all builders produced by <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="DelegateEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code. Defaults to StatusCodes.Status400BadRequest.</param>
        /// <param name="contentType">The response content type. Defaults to "application/validationproblem+json".</param>
        /// <returns>A <see cref="DelegateEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder ProducesValidationProblem(this DelegateEndpointConventionBuilder builder,
            int statusCode = StatusCodes.Status400BadRequest,
            string? contentType = null)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = "application/validationproblem+json";
            }

            return Produces<HttpValidationProblemDetails>(builder, statusCode, contentType);
        }

        /// <summary>
        /// Adds <see cref="IAcceptsMetadata"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request body.</typeparam>
        /// <param name="builder">The <see cref="DelegateEndpointConventionBuilder"/>.</param>
        /// <param name="contentType">The request content type that the endpoint accepts.</param>
        /// <param name="additionalContentTypes">The list of additional request content types that the endpoint accepts.</param>
        /// <returns>A <see cref="DelegateEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder Accepts<TRequest>(this DelegateEndpointConventionBuilder builder,
            string contentType, params string[] additionalContentTypes) where TRequest : notnull
        {
            Accepts(builder, typeof(TRequest), contentType, additionalContentTypes);

            return builder;
        }

        /// <summary>
        /// Adds <see cref="IAcceptsMetadata"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request body.</typeparam>
        /// <param name="builder">The <see cref="DelegateEndpointConventionBuilder"/>.</param>
        /// <param name="isOptional">Sets a value that determines if the request body is optional.</param>
        /// <param name="contentType">The request content type that the endpoint accepts.</param>
        /// <param name="additionalContentTypes">The list of additional request content types that the endpoint accepts.</param>
        /// <returns>A <see cref="DelegateEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder Accepts<TRequest>(this DelegateEndpointConventionBuilder builder,
            bool isOptional, string contentType, params string[] additionalContentTypes) where TRequest : notnull
        {
            Accepts(builder, typeof(TRequest), isOptional, contentType, additionalContentTypes);

            return builder;
        }

        /// <summary>
        /// Adds <see cref="IAcceptsMetadata"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="DelegateEndpointConventionBuilder"/>.</param>
        /// <param name="requestType">The type of the request body.</param>
        /// <param name="contentType">The request content type that the endpoint accepts.</param>
        /// <param name="additionalContentTypes">The list of additional request content types that the endpoint accepts.</param>
        /// <returns>A <see cref="DelegateEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder Accepts(this DelegateEndpointConventionBuilder builder,
            Type requestType, string contentType, params string[] additionalContentTypes)
        {
            builder.WithMetadata(new AcceptsMetadata(requestType, false, GetAllContentTypes(contentType, additionalContentTypes)));
            return builder;
        }


        /// <summary>
        /// Adds <see cref="IAcceptsMetadata"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="DelegateEndpointConventionBuilder"/>.</param>
        /// <param name="requestType">The type of the request body.</param>
        /// <param name="isOptional">Sets a value that determines if the request body is optional.</param>
        /// <param name="contentType">The request content type that the endpoint accepts.</param>
        /// <param name="additionalContentTypes">The list of additional request content types that the endpoint accepts.</param>
        /// <returns>A <see cref="DelegateEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder Accepts(this DelegateEndpointConventionBuilder builder,
            Type requestType, bool isOptional, string contentType, params string[] additionalContentTypes)
        {
            builder.WithMetadata(new AcceptsMetadata(requestType, isOptional, GetAllContentTypes(contentType, additionalContentTypes)));
            return builder;
        }

        private static string[] GetAllContentTypes(string contentType, string[] additionalContentTypes)
        {
            var allContentTypes = new string[additionalContentTypes.Length + 1];
            allContentTypes[0] = contentType;

            for (var i = 0; i < additionalContentTypes.Length; i++)
            {
                allContentTypes[i + 1] = additionalContentTypes[i];
            }

            return allContentTypes;
        }
    }
}
