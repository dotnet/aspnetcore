// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web.Http;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.WebApiCompatShim;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using ShimResources = Microsoft.AspNet.Mvc.WebApiCompatShim.Resources;

namespace System.Net.Http
{
    /// <summary>
    /// Provides extension methods for the <see cref="HttpRequestMessage"/> class.
    /// </summary>
    public static class HttpRequestMessageExtensions
    {
#if !ASPNETCORE50

        /// <summary>
        /// Helper method for creating an <see cref="HttpResponseMessage"/> message with a "416 (Requested Range Not
        /// Satisfiable)" status code. This response can be used in combination with the
        /// <see cref="ByteRangeStreamContent"/> to indicate that the requested range or
        /// ranges do not overlap with the current resource. The response contains a "Content-Range" header indicating
        /// the valid upper and lower bounds for requested ranges.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="invalidByteRangeException">An <see cref="InvalidByteRangeException"/> instance, typically
        /// thrown by a <see cref="ByteRangeStreamContent"/> instance.</param>
        /// <returns>
        /// An 416 (Requested Range Not Satisfiable) error response with a Content-Range header indicating the valid
        /// range.
        /// </returns>
        public static HttpResponseMessage CreateErrorResponse(
            [NotNull] this HttpRequestMessage request,
            [NotNull] InvalidByteRangeException invalidByteRangeException)
        {
            var rangeNotSatisfiableResponse = request.CreateErrorResponse(
                HttpStatusCode.RequestedRangeNotSatisfiable,
                invalidByteRangeException);
            rangeNotSatisfiableResponse.Content.Headers.ContentRange = invalidByteRangeException.ContentRange;
            return rangeNotSatisfiableResponse;
        }

#endif

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
            [NotNull] this HttpRequestMessage request,
            HttpStatusCode statusCode,
            [NotNull] string message)
        {
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
            [NotNull] this HttpRequestMessage request,
            HttpStatusCode statusCode,
            [NotNull] string message,
            [NotNull] Exception exception)
        {
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
            [NotNull] this HttpRequestMessage request,
            HttpStatusCode statusCode,
            [NotNull] Exception exception)
        {
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
            [NotNull] this HttpRequestMessage request,
            HttpStatusCode statusCode,
            [NotNull] ModelStateDictionary modelState)
        {
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
            [NotNull] this HttpRequestMessage request,
            HttpStatusCode statusCode,
            [NotNull] HttpError error)
        {
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
        public static HttpResponseMessage CreateResponse<T>([NotNull] this HttpRequestMessage request, T value)
        {
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
        /// This method will use the provided <paramref name="configuration"/> or it will get the
        /// <see cref="HttpContext"/> instance associated with <paramref name="request"/>.
        /// </remarks>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="request">The request.</param>
        /// <param name="statusCode">The status code of the created response.</param>
        /// <param name="value">The value to wrap. Can be <c>null</c>.</param>
        /// <param name="configuration">The configuration to use. Can be <c>null</c>.</param>
        /// <returns>A response wrapping <paramref name="value"/> with <paramref name="statusCode"/>.</returns>
        public static HttpResponseMessage CreateResponse<T>(
            [NotNull] this HttpRequestMessage request,
            HttpStatusCode statusCode,
            T value,
            IEnumerable<MediaTypeFormatter> formatters)
        {
            var context = GetHttpContext(request);

            if (formatters == null)
            {
                // Get the default formatters from options
                var options = context.RequestServices.GetRequiredService<IOptions<WebApiCompatShimOptions>>();
                formatters = options.Options.Formatters;
            }

            var contentNegotiator = context.RequestServices.GetRequiredService<IContentNegotiator>();

            var result = contentNegotiator.Negotiate(typeof(T), request, formatters);
            if (result?.Formatter == null)
            {
                // Return a 406 when we're actually performing conneg and it fails to find a formatter.
                return request.CreateResponse(HttpStatusCode.NotAcceptable);
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
            [NotNull] this HttpRequestMessage request,
            HttpStatusCode statusCode,
            [NotNull] T value,
            [NotNull] MediaTypeHeaderValue mediaType)
        {
            var context = GetHttpContext(request);

            // Get the default formatters from options
            var options = context.RequestServices.GetRequiredService<IOptions<WebApiCompatShimOptions>>();
            var formatters = options.Options.Formatters;

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
            [NotNull] this HttpRequestMessage request,
            HttpStatusCode statusCode,
            [NotNull] T value,
            [NotNull] MediaTypeFormatter formatter)
        {
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
            [NotNull] this HttpRequestMessage request,
            HttpStatusCode statusCode,
            [NotNull] T value,
            [NotNull] MediaTypeFormatter formatter,
            string mediaType)
        {
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
            [NotNull] this HttpRequestMessage request,
            HttpStatusCode statusCode,
            T value,
            [NotNull] MediaTypeFormatter formatter,
            MediaTypeHeaderValue mediaType)
        {
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