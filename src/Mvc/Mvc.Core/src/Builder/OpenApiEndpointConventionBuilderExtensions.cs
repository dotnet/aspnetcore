// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
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
        /// <param name="builder">The <see cref="MinimalActionEndpointConventionBuilder"/>.</param>
        /// <returns>A <see cref="MinimalActionEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder ExcludeFromDescription(this MinimalActionEndpointConventionBuilder builder)
        {
            builder.WithMetadata(_excludeFromDescriptionMetadataAttribute);

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
        /// <param name="builder">The <see cref="MinimalActionEndpointConventionBuilder"/>.</param>
        /// <param name="statusCode">The response status code. Defaults to StatusCodes.Status400BadRequest.</param>
        /// <param name="contentType">The response content type. Defaults to "application/validationproblem+json".</param>
        /// <returns>A <see cref="MinimalActionEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder ProducesValidationProblem(this MinimalActionEndpointConventionBuilder builder,
            int statusCode = StatusCodes.Status400BadRequest,
            string? contentType = null)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = "application/validationproblem+json";
            }

            return Produces<HttpValidationProblemDetails>(builder, statusCode, contentType);
        }

#pragma warning disable CS0419 // Ambiguous reference in cref attribute
        /// <summary>
        /// Adds the <see cref="Accepts"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <param name="builder">The <see cref="MinimalActionEndpointConventionBuilder"/>.</param>
        /// <param name="contentType">The request content type. Defaults to "application/json" if empty.</param>
        /// <param name="additionalContentTypes">Additional response content types the endpoint produces for the supplied status code.</param>
        /// <returns>A <see cref="MinimalActionEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static MinimalActionEndpointConventionBuilder Accepts<TRequest>(this MinimalActionEndpointConventionBuilder builder, string? contentType = null, params string[] additionalContentTypes)
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            Accepts(builder, typeof(TRequest), contentType, additionalContentTypes);

            return builder;
        }



#pragma warning disable CS0419 // Ambiguous reference in cref attribute
        /// <summary>
        /// Adds the <see cref="Accepts"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="MinimalActionEndpointConventionBuilder"/>.</param>
        /// <param name="requestType">The type of the request. Defaults to null.</param>
        /// <param name="contentType">The response content type that the endpoint accepts.</param>
        /// <param name="additionalContentTypes">Additional response content types the endpoint accepts</param>
        /// <returns>A <see cref="MinimalActionEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static MinimalActionEndpointConventionBuilder Accepts(this MinimalActionEndpointConventionBuilder builder, Type requestType ,
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
            string? contentType = null, params string[] additionalContentTypes)
        {
            
            builder.WithMetadata(new AcceptsMetadata(requestType, GetAllContentTypes(contentType, additionalContentTypes)));
            return builder;
        }



#pragma warning disable CS0419 // Ambiguous reference in cref attribute
        /// <summary>
        /// Adds the <see cref="Accepts"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
        /// produced by <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="MinimalActionEndpointConventionBuilder"/>.</param>
        /// <param name="contentType">The response content type that the endpoint accepts.</param>
        /// <param name="additionalContentTypes">Additional response content types the endpoint accepts</param>
        /// <returns>A <see cref="MinimalActionEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder Accepts(this MinimalActionEndpointConventionBuilder builder,
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
            string contentType, params string[] additionalContentTypes)
        {

            var allContentTypes = GetAllContentTypes(contentType, additionalContentTypes);
            builder.WithMetadata(new AcceptsMetadata(allContentTypes));

            return builder;
        }

        private static string[] GetAllContentTypes(string? contentType, string[] additionalContentTypes)
        {

            if (string.IsNullOrEmpty(contentType))
            {
                contentType = "application/json";
            }

            var allContentTypes = new List<string>()
            {
                contentType
            };
            allContentTypes.AddRange(additionalContentTypes);
            return allContentTypes.ToArray();
        }
    }
}
