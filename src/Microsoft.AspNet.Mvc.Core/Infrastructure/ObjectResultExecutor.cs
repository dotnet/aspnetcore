// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Infrastructure
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

            ValidateContentTypes(result.ContentTypes);

            var formatters = result.Formatters;
            if (formatters == null || formatters.Count == 0)
            {
                formatters = OptionsFormatters;
            }

            var objectType = result.DeclaredType;
            if (objectType == null || objectType == typeof(object))
            {
                objectType = result.Value?.GetType();
            };

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
            IList<MediaTypeHeaderValue> contentTypes,
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

            // Check if any content-type was explicitly set (for example, via ProducesAttribute
            // or URL path extension mapping). If yes, then ignore content-negotiation and use this content-type.
            if (contentTypes.Count == 1)
            {
                Logger.SkippedContentNegotiation(contentTypes[0]);

                return SelectFormatterUsingAnyAcceptableContentType(formatterContext, formatters, contentTypes);
            }

            var request = formatterContext.HttpContext.Request;
            var acceptValues = PrepareAcceptValues(request.GetTypedHeaders().Accept);

            IOutputFormatter selectedFormatter = null;
            if (contentTypes == null || contentTypes.Count == 0)
            {
                // Check if we have enough information to do content-negotiation, otherwise get the first formatter
                // which can write the type. Let the formatter choose the Content-Type.
                if (acceptValues == null || acceptValues.Count == 0)
                {
                    Logger.NoAcceptForNegotiation();

                    return SelectFormatterNotUsingAcceptHeaders(formatterContext, formatters);
                }

                //
                // Content-Negotiation starts from this point on.
                //

                // 1. Select based on sorted accept headers.
                selectedFormatter = SelectFormatterUsingSortedAcceptHeaders(
                    formatterContext,
                    formatters,
                    acceptValues);

                // 2. No formatter was found based on Accept header. Fallback to the first formatter which can write
                // the type. Let the formatter choose the Content-Type.
                if (selectedFormatter == null)
                {
                    Logger.NoFormatterFromNegotiation(acceptValues);

                    // Set this flag to indicate that content-negotiation has failed to let formatters decide
                    // if they want to write the response or not.
                    formatterContext.FailedContentNegotiation = true;

                    return SelectFormatterNotUsingAcceptHeaders(formatterContext, formatters);
                }
            }
            else
            {
                if (acceptValues != null && acceptValues.Count > 0)
                {
                    // Filter and remove accept headers which cannot support any of the user specified content types.
                    // That is, confirm this result supports a more specific media type than requested e.g. OK if
                    // "text/*" requested and result supports "text/plain".
                    for (var i = acceptValues.Count - 1; i >= 0; i--)
                    {
                        var isCompatible = false;
                        for (var j = 0; j < contentTypes.Count; j++)
                        {
                            if (contentTypes[j].IsSubsetOf(acceptValues[i]))
                            {
                                isCompatible = true;
                            }
                        }

                        if (!isCompatible)
                        {
                            acceptValues.RemoveAt(i);
                        }
                    }

                    selectedFormatter = SelectFormatterUsingSortedAcceptHeaders(
                        formatterContext,
                        formatters,
                        acceptValues);
                }

                if (selectedFormatter == null)
                {
                    // Either there were no acceptHeaders that were present OR
                    // There were no accept headers which matched OR
                    // There were acceptHeaders which matched but there was no formatter
                    // which supported any of them.
                    // In any of these cases, if the user has specified content types,
                    // do a last effort to find a formatter which can write any of the user specified content type.
                    selectedFormatter = SelectFormatterUsingAnyAcceptableContentType(
                        formatterContext,
                        formatters,
                        contentTypes);
                }
            }

            return selectedFormatter;
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
        protected virtual IOutputFormatter SelectFormatterNotUsingAcceptHeaders(
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
                formatterContext.ContentType = null;
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
            IList<MediaTypeHeaderValue> sortedAcceptHeaders)
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
            
            foreach (var contentType in sortedAcceptHeaders)
            {
                foreach (var formatter in formatters)
                {
                    formatterContext.ContentType = contentType;
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
            IList<MediaTypeHeaderValue> acceptableContentTypes)
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
                    formatterContext.ContentType = contentType;
                    if (formatter.CanWriteResult(formatterContext))
                    {
                        return formatter;
                    }
                }
            }

            return null;
        }

        // There's no allocation-free way to sort an IList so we're going to have to live with the
        // copy + insertion sort.
        private IList<MediaTypeHeaderValue> PrepareAcceptValues(IList<MediaTypeHeaderValue> values)
        {
            if (values == null || values.Count == 0)
            {
                return null;
            }

            // By default we want to ignore considering accept headers for content negotiation when
            // they have a media type like */* in them. Browsers typically have these media types.
            // In these cases we would want the first formatter in the list of output formatters to
            // write the response. This default behavior can be changed through options, so checking here.
            if (!RespectBrowserAcceptHeader)
            {
                for (var i = 0; i < values.Count; i++)
                {
                    if (values[i].MatchesAllTypes)
                    {
                        return null;
                    }
                }
            }

            // Degenerate case, we can avoid copying anything.
            if (values.Count == 1)
            {
                return values;
            }

            var sortNeeded = false;
            var count = 0;

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value.Quality == HeaderQuality.NoMatch)
                {
                    // Exclude this one
                }
                else if (value.Quality != null)
                {
                    count++;
                    sortNeeded = true;
                }
                else
                {
                    count++;
                }
            }

            if (!sortNeeded)
            {
                return values;
            }

            var sorted = new List<MediaTypeHeaderValue>(count);
            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value.Quality == HeaderQuality.NoMatch)
                {
                    // Exclude this one
                }
                else
                {
                    var position = sorted.BinarySearch(value, MediaTypeHeaderValueComparer.QualityComparer);
                    if (position >= 0)
                    {
                        sorted.Insert(position + 1, value);
                    }
                    else
                    {
                        sorted.Insert(~position, value);
                    }
                }
            }
            
            // We want a descending sort, but BinarySearch does ascending
            sorted.Reverse();
            return sorted;
        }

        private void ValidateContentTypes(IList<MediaTypeHeaderValue> contentTypes)
        {
            if (contentTypes == null)
            {
                return;
            }

            for (var i = 0; i < contentTypes.Count; i++)
            {
                var contentType = contentTypes[i];
                if (contentType.MatchesAllTypes || contentType.MatchesAllSubTypes)
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
