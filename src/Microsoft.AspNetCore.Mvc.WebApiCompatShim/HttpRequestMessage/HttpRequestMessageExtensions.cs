// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShimResources = Microsoft.AspNetCore.Mvc.WebApiCompatShim.Resources;

namespace System.Net.Http
{
    /// <summary>
    /// Provides extension methods for the <see cref="HttpRequestMessage"/> class.
    /// </summary>
    public static class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Helper method that performs content negotiation and creates a <see cref="HttpResponseMessage"/>
        /// representing an error with an instance of <see cref="ObjectContent{T}"/> wrapping an
        /// <see cref="HttpError"/> with message <paramref name="message"/>. If no formatter is found, this method
        /// returns a response with status 406 NotAcceptable.
        /// </summary>
        /// <remarks>
        /// This method requires that <paramref name="request"/> has been associated with an instance of
        /// <see cref="HttpContext"/>.
        /// </remarks>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="message">The error message.</param>
        /// <returns>
        /// An error response with error message <paramref name="message"/> and status code
        /// <paramref name="statusCode"/>.
        /// </returns>
        public static HttpResponseMessage CreateErrorResponse(
            this HttpRequestMessage request,
            HttpStatusCode statusCode,
            string message)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return request.CreateErrorResponse(statusCode, new HttpError(message));
        }

        /// <summary>
        /// Helper method that performs content negotiation and creates a <see cref="HttpResponseMessage"/>
        /// representing an error with an instance of <see cref="ObjectContent{T}"/> wrapping an
        /// <see cref="HttpError"/> with error message <paramref name="message"/> for exception
        /// <paramref name="exception"/>. If no formatter is found, this method returns a response with status 406
        /// NotAcceptable.
        /// </summary>
        /// <remarks>
        /// This method requires that <paramref name="request"/> has been associated with an instance of
        /// <see cref="HttpContext"/>.
        /// </remarks>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="message">The error message.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>An error response for <paramref name="exception"/> with error message <paramref name="message"/>
        /// and status code <paramref name="statusCode"/>.</returns>
        public static HttpResponseMessage CreateErrorResponse(
            this HttpRequestMessage request,
            HttpStatusCode statusCode,
            string message,
            Exception exception)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            var error = new HttpError(exception, includeErrorDetail: false) { Message = message };
            return request.CreateErrorResponse(statusCode, error);
        }

        /// <summary>
        /// Helper method that performs content negotiation and creates a <see cref="HttpResponseMessage"/>
        /// representing an error with an instance of <see cref="ObjectContent{T}"/> wrapping an
        /// <see cref="HttpError"/> for exception <paramref name="exception"/>. If no formatter is found, this method
        /// returns a response with status 406 NotAcceptable.
        /// </summary>
        /// <remarks>
        /// This method requires that <paramref name="request"/> has been associated with an instance of
        /// <see cref="HttpContext"/>.
        /// </remarks>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>
        /// An error response for <paramref name="exception"/> with status code <paramref name="statusCode"/>.
        /// </returns>
        public static HttpResponseMessage CreateErrorResponse(
            this HttpRequestMessage request,
            HttpStatusCode statusCode,
            Exception exception)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return request.CreateErrorResponse(statusCode, new HttpError(exception, includeErrorDetail: false));
        }

        /// <summary>
        /// Helper method that performs content negotiation and creates a <see cref="HttpResponseMessage"/>
        /// representing an error with an instance of <see cref="ObjectContent{T}"/> wrapping an
        /// <see cref="HttpError"/> for model state <paramref name="modelState"/>. If no formatter is found, this
        /// method returns a response with status 406 NotAcceptable.
        /// </summary>
        /// <remarks>
        /// This method requires that <paramref name="request"/> has been associated with an instance of
        /// <see cref="HttpContext"/>.
        /// </remarks>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="modelState">The model state.</param>
        /// <returns>
        /// An error response for <paramref name="modelState"/> with status code <paramref name="statusCode"/>.
        /// </returns>
        public static HttpResponseMessage CreateErrorResponse(
            this HttpRequestMessage request,
            HttpStatusCode statusCode,
            ModelStateDictionary modelState)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            return request.CreateErrorResponse(statusCode, new HttpError(modelState, includeErrorDetail: false));
        }

        /// <summary>
        /// Helper method that performs content negotiation and creates a <see cref="HttpResponseMessage"/>
        /// representing an error with an instance of <see cref="ObjectContent{T}"/> wrapping <paramref name="error"/>
        /// as the content. If no formatter is found, this method returns a response with status 406 NotAcceptable.
        /// </summary>
        /// <remarks>
        /// This method requires that <paramref name="request"/> has been associated with an instance of
        /// <see cref="HttpContext"/>.
        /// </remarks>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="error">The error to wrap.</param>
        /// <returns>
        /// An error response wrapping <paramref name="error"/> with status code <paramref name="statusCode"/>.
        /// </returns>
        public static HttpResponseMessage CreateErrorResponse(
            this HttpRequestMessage request,
            HttpStatusCode statusCode,
            HttpError error)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            return request.CreateResponse<HttpError>(statusCode, error);
        }

        /// <summary>
        /// Helper method that performs content negotiation and creates a <see cref="HttpResponseMessage"/> with an
        /// instance of <see cref="ObjectContent{T}"/> as the content and <see cref="System.Net.HttpStatusCode.OK"/>
        /// as the status code if a formatter can be found. If no formatter is found, this method returns a response
        /// with status 406 NotAcceptable.
        /// </summary>
        /// <remarks>
        /// This method requires that <paramref name="request"/> has been associated with an instance of
        /// <see cref="HttpContext"/>.
        /// </remarks>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        /// <param name="value">The value to wrap. Can be <c>null</c>.</param>
        /// <returns>
        /// A response wrapping <paramref name="value"/> with <see cref="System.Net.HttpStatusCode.OK"/> status code.
        /// </returns>
        public static HttpResponseMessage CreateResponse<T>(this HttpRequestMessage request, T value)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return request.CreateResponse<T>(HttpStatusCode.OK, value, formatters: null);
        }

        /// <summary>
        /// Helper method that performs content negotiation and creates a <see cref="HttpResponseMessage"/> with an
        /// instance of <see cref="ObjectContent{T}"/> as the content if a formatter can be found. If no formatter is
        /// found, this method returns a response with status 406 NotAcceptable.
        /// configuration.
        /// </summary>
        /// <remarks>
        /// This method requires that <paramref name="request"/> has been associated with an instance of
        /// <see cref="HttpContext"/>.
        /// </remarks>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="value">The value to wrap. Can be <c>null</c>.</param>
        /// <returns>A response wrapping <paramref name="value"/> with <paramref name="statusCode"/>.</returns>
        public static HttpResponseMessage CreateResponse<T>(
            this HttpRequestMessage request,
            HttpStatusCode statusCode,
            T value)
        {
            return request.CreateResponse<T>(statusCode, value, formatters: null);
        }

        /// <summary>
        /// Helper method that performs content negotiation and creates a <see cref="HttpResponseMessage"/> with an
        /// instance of <see cref="ObjectContent{T}"/> as the content if a formatter can be found. If no formatter is
        /// found, this method returns a response with status 406 NotAcceptable.
        /// </summary>
        /// <remarks>
        /// This method will get the <see cref="HttpContext"/> instance associated with <paramref name="request"/>.
        /// </remarks>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="value">The value to wrap. Can be <c>null</c>.</param>
        /// <param name="formatters">The set of <see cref="MediaTypeFormatter"/> objects from which to choose.</param>
        /// <returns>A response wrapping <paramref name="value"/> with <paramref name="statusCode"/>.</returns>
        public static HttpResponseMessage CreateResponse<T>(
            this HttpRequestMessage request,
            HttpStatusCode statusCode,
            T value,
            IEnumerable<MediaTypeFormatter> formatters)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var context = GetHttpContext(request);

            if (formatters == null)
            {
                // Get the default formatters from options
                var options = context.RequestServices.GetRequiredService<IOptions<WebApiCompatShimOptions>>();
                formatters = options.Value.Formatters;
            }

            var contentNegotiator = context.RequestServices.GetRequiredService<IContentNegotiator>();

            var result = contentNegotiator.Negotiate(typeof(T), request, formatters);
            if (result?.Formatter == null)
            {
                // Return a 406 when we're actually performing conneg and it fails to find a formatter.
                return new HttpResponseMessage(HttpStatusCode.NotAcceptable)
                {
                    RequestMessage = request
                };
            }
            else
            {
                return request.CreateResponse(statusCode, value, result.Formatter, result.MediaType);
            }
        }

        /// <summary>
        /// Helper method that creates a <see cref="HttpResponseMessage"/> with an <see cref="ObjectContent{T}"/>
        /// instance containing the provided <paramref name="value"/>. The given <paramref name="mediaType"/> is used
        /// to find an instance of <see cref="MediaTypeFormatter"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="value">The value to wrap. Can be <c>null</c>.</param>
        /// <param name="mediaType">
        /// The media type used to look up an instance of <see cref="MediaTypeFormatter"/>.
        /// </param>
        /// <returns>A response wrapping <paramref name="value"/> with <paramref name="statusCode"/>.</returns>
        public static HttpResponseMessage CreateResponse<T>(
            this HttpRequestMessage request,
            HttpStatusCode statusCode,
            T value,
            string mediaType)
        {
            return request.CreateResponse(statusCode, value, new MediaTypeHeaderValue(mediaType));
        }

        /// <summary>
        /// Helper method that creates a <see cref="HttpResponseMessage"/> with an <see cref="ObjectContent{T}"/>
        /// instance containing the provided <paramref name="value"/>. The given <paramref name="mediaType"/> is used
        /// to find an instance of <see cref="MediaTypeFormatter"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="value">The value to wrap. Can be <c>null</c>.</param>
        /// <param name="mediaType">
        /// The media type used to look up an instance of <see cref="MediaTypeFormatter"/>.
        /// </param>
        /// <returns>A response wrapping <paramref name="value"/> with <paramref name="statusCode"/>.</returns>
        public static HttpResponseMessage CreateResponse<T>(
            this HttpRequestMessage request,
            HttpStatusCode statusCode,
            T value,
            MediaTypeHeaderValue mediaType)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (mediaType == null)
            {
                throw new ArgumentNullException(nameof(mediaType));
            }

            var context = GetHttpContext(request);

            // Get the default formatters from options
            var options = context.RequestServices.GetRequiredService<IOptions<WebApiCompatShimOptions>>();
            var formatters = options.Value.Formatters;

            var formatter = formatters.FindWriter(typeof(T), mediaType);
            if (formatter == null)
            {
                var message = ShimResources.FormatHttpRequestMessage_CouldNotFindMatchingFormatter(
                    mediaType.ToString(),
                    value.GetType());
                throw new InvalidOperationException(message);
            }

            return request.CreateResponse(statusCode, value, formatter, mediaType);
        }

        /// <summary>
        /// Helper method that creates a <see cref="HttpResponseMessage"/> with an <see cref="ObjectContent{T}"/>
        /// instance containing the provided <paramref name="value"/> and the given <paramref name="formatter"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="value">The value to wrap. Can be <c>null</c>.</param>
        /// <param name="formatter">The formatter to use.</param>
        /// <returns>A response wrapping <paramref name="value"/> with <paramref name="statusCode"/>.</returns>
        public static HttpResponseMessage CreateResponse<T>(
            this HttpRequestMessage request,
            HttpStatusCode statusCode,
            T value,
            MediaTypeFormatter formatter)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            return request.CreateResponse(statusCode, value, formatter, (MediaTypeHeaderValue)null);
        }

        /// <summary>
        /// Helper method that creates a <see cref="HttpResponseMessage"/> with an <see cref="ObjectContent{T}"/>
        /// instance containing the provided <paramref name="value"/> and the given <paramref name="formatter"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="value">The value to wrap. Can be <c>null</c>.</param>
        /// <param name="formatter">The formatter to use.</param>
        /// <param name="mediaType">
        /// The media type override to set on the response's content. Can be <c>null</c>.
        /// </param>
        /// <returns>A response wrapping <paramref name="value"/> with <paramref name="statusCode"/>.</returns>
        public static HttpResponseMessage CreateResponse<T>(
            this HttpRequestMessage request,
            HttpStatusCode statusCode,
            T value,
            MediaTypeFormatter formatter,
            string mediaType)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var mediaTypeHeader = mediaType != null ? new MediaTypeHeaderValue(mediaType) : null;
            return request.CreateResponse(statusCode, value, formatter, mediaTypeHeader);
        }

        /// <summary>
        /// Helper method that creates a <see cref="HttpResponseMessage"/> with an <see cref="ObjectContent{T}"/>
        /// instance containing the provided <paramref name="value"/> and the given <paramref name="formatter"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="value">The value to wrap. Can be <c>null</c>.</param>
        /// <param name="formatter">The formatter to use.</param>
        /// <param name="mediaType">
        /// The media type override to set on the response's content. Can be <c>null</c>.
        /// </param>
        /// <returns>A response wrapping <paramref name="value"/> with <paramref name="statusCode"/>.</returns>
        public static HttpResponseMessage CreateResponse<T>(
            this HttpRequestMessage request,
            HttpStatusCode statusCode,
            T value,
            MediaTypeFormatter formatter,
            MediaTypeHeaderValue mediaType)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var response = new HttpResponseMessage(statusCode)
            {
                RequestMessage = request,
            };

            response.Content = new ObjectContent<T>(value, formatter, mediaType);

            return response;
        }

        private static HttpContext GetHttpContext(HttpRequestMessage request)
        {
            var context = request.GetProperty<HttpContext>(nameof(HttpContext));
            if (context == null)
            {
                var message = ShimResources.FormatHttpRequestMessage_MustHaveHttpContext(
                    nameof(HttpRequestMessage),
                    "HttpRequestMessageHttpContextExtensions.GetHttpRequestMessage");
                throw new InvalidOperationException(message);
            }

            return context;
        }

        private static T GetProperty<T>(this HttpRequestMessage request, string key)
        {
            object value;
            request.Properties.TryGetValue(key, out value);

            if (value is T)
            {
                return (T)value;
            }
            else
            {
                return default(T);
            }
        }
    }
}
