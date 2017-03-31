// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// Executes an <see cref="ObjectResult"/> to write to the response.
    /// </summary>
    public class ObjectResultExecutor
    {
        /// <summary>
        /// Creates a new <see cref="ObjectResultExecutor"/>.
        /// </summary>
        /// <param name="options">An accessor to <see cref="MvcOptions"/>.</param>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public ObjectResultExecutor(
            IOptions<MvcOptions> options,
            IHttpResponseStreamWriterFactory writerFactory,
            ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            OptionsFormatters = options.Value.OutputFormatters;
            RespectBrowserAcceptHeader = options.Value.RespectBrowserAcceptHeader;
            ReturnHttpNotAcceptable = options.Value.ReturnHttpNotAcceptable;
            Logger = loggerFactory.CreateLogger<ObjectResultExecutor>();
            WriterFactory = writerFactory.CreateWriter;
        }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the list of <see cref="IOutputFormatter"/> instances from <see cref="MvcOptions"/>.
        /// </summary>
        protected FormatterCollection<IOutputFormatter> OptionsFormatters { get; }

        /// <summary>
        /// Gets the value of <see cref="MvcOptions.RespectBrowserAcceptHeader"/>.
        /// </summary>
        protected bool RespectBrowserAcceptHeader { get; }

        /// <summary>
        /// Gets the value of <see cref="MvcOptions.ReturnHttpNotAcceptable"/>.
        /// </summary>
        protected bool ReturnHttpNotAcceptable { get; }

        /// <summary>
        /// Gets the writer factory delegate.
        /// </summary>
        protected Func<Stream, Encoding, TextWriter> WriterFactory { get; }

        /// <summary>
        /// Executes the <see cref="ObjectResult"/>.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/> for the current request.</param>
        /// <param name="result">The <see cref="ObjectResult"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> which will complete once the <see cref="ObjectResult"/> is written to the response.
        /// </returns>
        public virtual Task ExecuteAsync(ActionContext context, ObjectResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            // If the user sets the content type both on the ObjectResult (example: by Produces) and Response object,
            // then the one set on ObjectResult takes precedence over the Response object
            if (result.ContentTypes == null || result.ContentTypes.Count == 0)
            {
                var responseContentType = context.HttpContext.Response.ContentType;
                if (!string.IsNullOrEmpty(responseContentType))
                {
                    if (result.ContentTypes == null)
                    {
                        result.ContentTypes = new MediaTypeCollection();
                    }

                    result.ContentTypes.Add(responseContentType);
                }
            }

            ValidateContentTypes(result.ContentTypes);

            var formatters = result.Formatters;
            if (formatters == null || formatters.Count == 0)
            {
                formatters = OptionsFormatters;

                // Complain about MvcOptions.OutputFormatters only if the result has an empty Formatters.
                Debug.Assert(formatters != null, "MvcOptions.OutputFormatters cannot be null.");
                if (formatters.Count == 0)
                {
                    throw new InvalidOperationException(Resources.FormatOutputFormattersAreRequired(
                        typeof(MvcOptions).FullName,
                        nameof(MvcOptions.OutputFormatters),
                        typeof(IOutputFormatter).FullName));
                }
            }

            var objectType = result.DeclaredType;
            if (objectType == null || objectType == typeof(object))
            {
                objectType = result.Value?.GetType();
            }

            var formatterContext = new OutputFormatterWriteContext(
                context.HttpContext,
                WriterFactory,
                objectType,
                result.Value);

            var selectedFormatter = SelectFormatter(formatterContext, result.ContentTypes, formatters);
            if (selectedFormatter == null)
            {
                // No formatter supports this.
                Logger.NoFormatter(formatterContext);

                context.HttpContext.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                return TaskCache.CompletedTask;
            }

            Logger.FormatterSelected(selectedFormatter, formatterContext);
            Logger.ObjectResultExecuting(context);

            result.OnFormatting(context);
            return selectedFormatter.WriteAsync(formatterContext);
        }

        /// <summary>
        /// Selects the <see cref="IOutputFormatter"/> to write the response.
        /// </summary>
        /// <param name="formatterContext">The <see cref="OutputFormatterWriteContext"/>.</param>
        /// <param name="contentTypes">
        /// The list of content types provided by <see cref="ObjectResult.ContentTypes"/>.
        /// </param>
        /// <param name="formatters">
        /// The list of <see cref="IOutputFormatter"/> instances to consider.
        /// </param>
        /// <returns>
        /// The selected <see cref="IOutputFormatter"/> or <c>null</c> if no formatter can write the response.
        /// </returns>
        protected virtual IOutputFormatter SelectFormatter(
            OutputFormatterWriteContext formatterContext,
            MediaTypeCollection contentTypes,
            IList<IOutputFormatter> formatters)
        {
            if (formatterContext == null)
            {
                throw new ArgumentNullException(nameof(formatterContext));
            }

            if (contentTypes == null)
            {
                throw new ArgumentNullException(nameof(contentTypes));
            }

            if (formatters == null)
            {
                throw new ArgumentNullException(nameof(formatters));
            }

            var request = formatterContext.HttpContext.Request;
            var acceptableMediaTypes = GetAcceptableMediaTypes(contentTypes, request);
            var selectFormatterWithoutRegardingAcceptHeader = false;
            IOutputFormatter selectedFormatter = null;

            if (acceptableMediaTypes.Count == 0)
            {
                // There is either no Accept header value, or it contained */* and we
                // are not currently respecting the 'browser accept header'.
                Logger.NoAcceptForNegotiation();

                selectFormatterWithoutRegardingAcceptHeader = true;
            }
            else
            {
                if (contentTypes.Count == 0)
                {
                    // Use whatever formatter can meet the client's request
                    selectedFormatter = SelectFormatterUsingSortedAcceptHeaders(
                        formatterContext,
                        formatters,
                        acceptableMediaTypes);
                }
                else
                {
                    // Verify that a content type from the context is compatible with the client's request
                    selectedFormatter = SelectFormatterUsingSortedAcceptHeadersAndContentTypes(
                        formatterContext,
                        formatters,
                        acceptableMediaTypes,
                        contentTypes);
                }

                if (selectedFormatter == null && !ReturnHttpNotAcceptable)
                {
                    Logger.NoFormatterFromNegotiation(acceptableMediaTypes);

                    selectFormatterWithoutRegardingAcceptHeader = true;
                }
            }

            if (selectFormatterWithoutRegardingAcceptHeader)
            {
                if (contentTypes.Count == 0)
                {
                    selectedFormatter = SelectFormatterNotUsingContentType(
                        formatterContext,
                        formatters);
                }
                else
                {
                    selectedFormatter = SelectFormatterUsingAnyAcceptableContentType(
                        formatterContext,
                        formatters,
                        contentTypes);
                }
            }

            return selectedFormatter;
        }

        private List<MediaTypeSegmentWithQuality> GetAcceptableMediaTypes(
            MediaTypeCollection contentTypes,
            HttpRequest request)
        {
            var result = new List<MediaTypeSegmentWithQuality>();
            AcceptHeaderParser.ParseAcceptHeader(request.Headers[HeaderNames.Accept], result);
            for (int i = 0; i < result.Count; i++)
            {
                var mediaType = new MediaType(result[i].MediaType);
                if (!RespectBrowserAcceptHeader && mediaType.MatchesAllSubTypes && mediaType.MatchesAllTypes)
                {
                    result.Clear();
                    return result;
                }
            }

            result.Sort((left, right) => left.Quality > right.Quality ? -1 : (left.Quality == right.Quality ? 0 : 1));

            return result;
        }

        /// <summary>
        /// Selects the <see cref="IOutputFormatter"/> to write the response. The first formatter which
        /// can write the response should be chosen without any consideration for content type.
        /// </summary>
        /// <param name="formatterContext">The <see cref="OutputFormatterWriteContext"/>.</param>
        /// <param name="formatters">
        /// The list of <see cref="IOutputFormatter"/> instances to consider.
        /// </param>
        /// <returns>
        /// The selected <see cref="IOutputFormatter"/> or <c>null</c> if no formatter can write the response.
        /// </returns>
        protected virtual IOutputFormatter SelectFormatterNotUsingContentType(
            OutputFormatterWriteContext formatterContext,
            IList<IOutputFormatter> formatters)
        {
            if (formatterContext == null)
            {
                throw new ArgumentNullException(nameof(formatterContext));
            }

            if (formatters == null)
            {
                throw new ArgumentNullException(nameof(formatters));
            }

            foreach (var formatter in formatters)
            {
                formatterContext.ContentType = new StringSegment();
                formatterContext.ContentTypeIsServerDefined = false;
                if (formatter.CanWriteResult(formatterContext))
                {
                    return formatter;
                }
            }

            return null;
        }

        /// <summary>
        /// Selects the <see cref="IOutputFormatter"/> to write the response based on the content type values
        /// present in <paramref name="sortedAcceptHeaders"/>.
        /// </summary>
        /// <param name="formatterContext">The <see cref="OutputFormatterWriteContext"/>.</param>
        /// <param name="formatters">
        /// The list of <see cref="IOutputFormatter"/> instances to consider.
        /// </param>
        /// <param name="sortedAcceptHeaders">
        /// The ordered content types from the <c>Accept</c> header, sorted by descending q-value.
        /// </param>
        /// <returns>
        /// The selected <see cref="IOutputFormatter"/> or <c>null</c> if no formatter can write the response.
        /// </returns>
        protected virtual IOutputFormatter SelectFormatterUsingSortedAcceptHeaders(
            OutputFormatterWriteContext formatterContext,
            IList<IOutputFormatter> formatters,
            IList<MediaTypeSegmentWithQuality> sortedAcceptHeaders)
        {
            if (formatterContext == null)
            {
                throw new ArgumentNullException(nameof(formatterContext));
            }

            if (formatters == null)
            {
                throw new ArgumentNullException(nameof(formatters));
            }

            if (sortedAcceptHeaders == null)
            {
                throw new ArgumentNullException(nameof(sortedAcceptHeaders));
            }

            for (var i = 0; i < sortedAcceptHeaders.Count; i++)
            {
                var mediaType = sortedAcceptHeaders[i];
                formatterContext.ContentType = mediaType.MediaType;
                formatterContext.ContentTypeIsServerDefined = false;
                for (var j = 0; j < formatters.Count; j++)
                {
                    var formatter = formatters[j];
                    if (formatter.CanWriteResult(formatterContext))
                    {
                        return formatter;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Selects the <see cref="IOutputFormatter"/> to write the response based on the content type values
        /// present in <paramref name="acceptableContentTypes"/>.
        /// </summary>
        /// <param name="formatterContext">The <see cref="OutputFormatterWriteContext"/>.</param>
        /// <param name="formatters">
        /// The list of <see cref="IOutputFormatter"/> instances to consider.
        /// </param>
        /// <param name="acceptableContentTypes">
        /// The ordered content types from <see cref="ObjectResult.ContentTypes"/> in descending priority order.
        /// </param>
        /// <returns>
        /// The selected <see cref="IOutputFormatter"/> or <c>null</c> if no formatter can write the response.
        /// </returns>
        protected virtual IOutputFormatter SelectFormatterUsingAnyAcceptableContentType(
            OutputFormatterWriteContext formatterContext,
            IList<IOutputFormatter> formatters,
            MediaTypeCollection acceptableContentTypes)
        {
            if (formatterContext == null)
            {
                throw new ArgumentNullException(nameof(formatterContext));
            }

            if (formatters == null)
            {
                throw new ArgumentNullException(nameof(formatters));
            }

            if (acceptableContentTypes == null)
            {
                throw new ArgumentNullException(nameof(acceptableContentTypes));
            }

            foreach (var formatter in formatters)
            {
                foreach (var contentType in acceptableContentTypes)
                {
                    formatterContext.ContentType = new StringSegment(contentType);
                    formatterContext.ContentTypeIsServerDefined = true;
                    if (formatter.CanWriteResult(formatterContext))
                    {
                        return formatter;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Selects the <see cref="IOutputFormatter"/> to write the response based on the content type values
        /// present in <paramref name="sortedAcceptableContentTypes"/> and <paramref name="possibleOutputContentTypes"/>.
        /// </summary>
        /// <param name="formatterContext">The <see cref="OutputFormatterWriteContext"/>.</param>
        /// <param name="formatters">
        /// The list of <see cref="IOutputFormatter"/> instances to consider.
        /// </param>
        /// <param name="sortedAcceptableContentTypes">
        /// The ordered content types from the <c>Accept</c> header, sorted by descending q-value.
        /// </param>
        /// <param name="possibleOutputContentTypes">
        /// The ordered content types from <see cref="ObjectResult.ContentTypes"/> in descending priority order.
        /// </param>
        /// <returns>
        /// The selected <see cref="IOutputFormatter"/> or <c>null</c> if no formatter can write the response.
        /// </returns>
        protected virtual IOutputFormatter SelectFormatterUsingSortedAcceptHeadersAndContentTypes(
            OutputFormatterWriteContext formatterContext,
            IList<IOutputFormatter> formatters,
            IList<MediaTypeSegmentWithQuality> sortedAcceptableContentTypes,
            MediaTypeCollection possibleOutputContentTypes)
        {
            for (var i = 0; i < sortedAcceptableContentTypes.Count; i++)
            {
                var acceptableContentType = new MediaType(sortedAcceptableContentTypes[i].MediaType);
                for (var j = 0; j < possibleOutputContentTypes.Count; j++)
                {
                    var candidateContentType = new MediaType(possibleOutputContentTypes[j]);
                    if (candidateContentType.IsSubsetOf(acceptableContentType))
                    {
                        for (var k = 0; k < formatters.Count; k++)
                        {
                            var formatter = formatters[k];
                            formatterContext.ContentType = new StringSegment(possibleOutputContentTypes[j]);
                            formatterContext.ContentTypeIsServerDefined = true;
                            if (formatter.CanWriteResult(formatterContext))
                            {
                                return formatter;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private void ValidateContentTypes(MediaTypeCollection contentTypes)
        {
            if (contentTypes == null)
            {
                return;
            }

            for (var i = 0; i < contentTypes.Count; i++)
            {
                var contentType = contentTypes[i];
                var parsedContentType = new MediaType(contentType);
                if (parsedContentType.HasWildcard)
                {
                    var message = Resources.FormatObjectResult_MatchAllContentType(
                        contentType,
                        nameof(ObjectResult.ContentTypes));
                    throw new InvalidOperationException(message);
                }
            }
        }
    }
}
