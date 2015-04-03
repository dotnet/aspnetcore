// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    public class ObjectResult : ActionResult
    {
        public ObjectResult(object value)
        {
            Value = value;
            Formatters = new List<IOutputFormatter>();
            ContentTypes = new List<MediaTypeHeaderValue>();
        }

        public object Value { get; set; }

        public IList<IOutputFormatter> Formatters { get; set; }

        public IList<MediaTypeHeaderValue> ContentTypes { get; set; }

        public Type DeclaredType { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            // See if the list of content types added to this object result is valid.
            ThrowIfUnsupportedContentType();
            var formatters = GetDefaultFormatters(context);
            var formatterContext = new OutputFormatterContext()
            {
                DeclaredType = DeclaredType,
                ActionContext = context,
                Object = Value,
                StatusCode = StatusCode
            };

            var selectedFormatter = SelectFormatter(formatterContext, formatters);
            if (selectedFormatter == null)
            {
                // No formatter supports this.
                context.HttpContext.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                return;
            }

            if (StatusCode.HasValue)
            {
                context.HttpContext.Response.StatusCode = StatusCode.Value;
            }

            OnFormatting(context);
            await selectedFormatter.WriteAsync(formatterContext);
        }

        public virtual IOutputFormatter SelectFormatter(
            OutputFormatterContext formatterContext,
            IEnumerable<IOutputFormatter> formatters)
        {
            // Check if any content-type was explicitly set (for example, via ProducesAttribute 
            // or Url path extension mapping). If yes, then ignore content-negotiation and use this content-type.
            if (ContentTypes.Count == 1)
            {
                return SelectFormatterUsingAnyAcceptableContentType(formatterContext,
                                                                    formatters,
                                                                    ContentTypes);
            }

            var sortedAcceptHeaderMediaTypes = GetSortedAcceptHeaderMediaTypes(formatterContext);

            IOutputFormatter selectedFormatter = null;
            if (ContentTypes == null || ContentTypes.Count == 0)
            {
                // Check if we have enough information to do content-negotiation, otherwise get the first formatter
                // which can write the type.
                MediaTypeHeaderValue requestContentType = null;
                MediaTypeHeaderValue.TryParse(
                    formatterContext.ActionContext.HttpContext.Request.ContentType,
                    out requestContentType);
                if (!sortedAcceptHeaderMediaTypes.Any() && requestContentType == null)
                {
                    return SelectFormatterBasedOnTypeMatch(formatterContext, formatters);
                }

                //
                // Content-Negotiation starts from this point on.
                //

                // 1. Select based on sorted accept headers.
                if (sortedAcceptHeaderMediaTypes.Any())
                {
                    selectedFormatter = SelectFormatterUsingSortedAcceptHeaders(
                                                                            formatterContext,
                                                                            formatters,
                                                                            sortedAcceptHeaderMediaTypes);
                }

                // 2. No formatter was found based on accept headers, fall back on request Content-Type header.
                if (selectedFormatter == null && requestContentType != null)
                {
                    selectedFormatter = SelectFormatterUsingAnyAcceptableContentType(
                                                                                formatterContext,
                                                                                formatters,
                                                                                new[] { requestContentType });
                }

                // 3. No formatter was found based on Accept and request Content-Type headers, so
                // fallback on type based match.
                if (selectedFormatter == null)
                {
                    // Set this flag to indicate that content-negotiation has failed to let formatters decide
                    // if they want to write the response or not.
                    formatterContext.FailedContentNegotiation = true;

                    return SelectFormatterBasedOnTypeMatch(formatterContext, formatters);
                }
            }
            else
            {
                if (sortedAcceptHeaderMediaTypes.Any())
                {
                    // Filter and remove accept headers which cannot support any of the user specified content types.
                    var filteredAndSortedAcceptHeaders = sortedAcceptHeaderMediaTypes
                                                                    .Where(acceptHeader => 
                                                                        ContentTypes.Any(contentType => 
                                                                            contentType.IsSubsetOf(acceptHeader)));

                    selectedFormatter = SelectFormatterUsingSortedAcceptHeaders(
                                                                        formatterContext,
                                                                        formatters,
                                                                        filteredAndSortedAcceptHeaders);
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
                                                                        ContentTypes);
                }
            }

            return selectedFormatter;
        }

        public virtual IOutputFormatter SelectFormatterBasedOnTypeMatch(
            OutputFormatterContext formatterContext,
            IEnumerable<IOutputFormatter> formatters)
        {
            foreach (var formatter in formatters)
            {
                if (formatter.CanWriteResult(formatterContext, contentType: null))
                {
                    return formatter;
                }
            }

            return null;
        }

        public virtual IOutputFormatter SelectFormatterUsingSortedAcceptHeaders(
                                                            OutputFormatterContext formatterContext,
                                                            IEnumerable<IOutputFormatter> formatters,
                                                            IEnumerable<MediaTypeHeaderValue> sortedAcceptHeaders)
        {
            IOutputFormatter selectedFormatter = null;
            foreach (var contentType in sortedAcceptHeaders)
            {
                // Loop through each of the formatters and see if any one will support this
                // mediaType Value.
                selectedFormatter = formatters.FirstOrDefault(
                                                    formatter =>
                                                        formatter.CanWriteResult(formatterContext, contentType));
                if (selectedFormatter != null)
                {
                    // we found our match.
                    break;
                }
            }

            return selectedFormatter;
        }

        public virtual IOutputFormatter SelectFormatterUsingAnyAcceptableContentType(
                                                            OutputFormatterContext formatterContext,
                                                            IEnumerable<IOutputFormatter> formatters,
                                                            IEnumerable<MediaTypeHeaderValue> acceptableContentTypes)
        {
            var selectedFormatter = formatters.FirstOrDefault(
                                            formatter =>
                                                    acceptableContentTypes
                                                    .Any(contentType =>
                                                            formatter.CanWriteResult(formatterContext, contentType)));
            return selectedFormatter;
        }

        private IEnumerable<MediaTypeHeaderValue> GetSortedAcceptHeaderMediaTypes(
            OutputFormatterContext formatterContext)
        {
            var request = formatterContext.ActionContext.HttpContext.Request;
            var incomingAcceptHeaderMediaTypes = request.GetTypedHeaders().Accept ?? new MediaTypeHeaderValue[] { };

            // By default we want to ignore considering accept headers for content negotiation when
            // they have a media type like */* in them. Browsers typically have these media types.
            // In these cases we would want the first formatter in the list of output formatters to
            // write the response. This default behavior can be changed through options, so checking here.
            var options = formatterContext.ActionContext.HttpContext
                                                        .RequestServices
                                                        .GetRequiredService<IOptions<MvcOptions>>()
                                                        .Options;

            var respectAcceptHeader = true;
            if (options.RespectBrowserAcceptHeader == false
                && incomingAcceptHeaderMediaTypes.Any(mediaType => mediaType.MatchesAllTypes))
            {
                respectAcceptHeader = false;
            }

            var sortedAcceptHeaderMediaTypes = Enumerable.Empty<MediaTypeHeaderValue>();
            if (respectAcceptHeader)
            {
                sortedAcceptHeaderMediaTypes = SortMediaTypeHeaderValues(incomingAcceptHeaderMediaTypes)
                                                .Where(header => header.Quality != HeaderQuality.NoMatch);
            }

            return sortedAcceptHeaderMediaTypes;
        }

        private void ThrowIfUnsupportedContentType()
        {
            var matchAllContentType = ContentTypes?.FirstOrDefault(
                contentType => contentType.MatchesAllSubTypes || contentType.MatchesAllTypes);
            if (matchAllContentType != null)
            {
                throw new InvalidOperationException(
                    Resources.FormatObjectResult_MatchAllContentType(matchAllContentType, nameof(ContentTypes)));
            }
        }

        private static IEnumerable<MediaTypeHeaderValue> SortMediaTypeHeaderValues(
            IEnumerable<MediaTypeHeaderValue> headerValues)
        {
            // Use OrderBy() instead of Array.Sort() as it performs fewer comparisons. In this case the comparisons
            // are quite expensive so OrderBy() performs better.
            return headerValues.OrderByDescending(headerValue =>
                                                    headerValue,
                                                    MediaTypeHeaderValueComparer.QualityComparer);
        }

        private IEnumerable<IOutputFormatter> GetDefaultFormatters(ActionContext context)
        {
            IEnumerable<IOutputFormatter> formatters = null;
            if (Formatters == null || Formatters.Count == 0)
            {
                formatters = context
                    .HttpContext
                    .RequestServices
                    .GetRequiredService<IOptions<MvcOptions>>()
                    .Options
                    .OutputFormatters;
            }
            else
            {
                formatters = Formatters;
            }

            return formatters;
        }

        /// <summary>
        /// This method is called before the formatter writes to the output stream.
        /// </summary>
        protected virtual void OnFormatting([NotNull] ActionContext context)
        {
        }
    }
}