// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class DefaultOutputFormatterSelector : OutputFormatterSelector
    {
        private static readonly Comparison<MediaTypeSegmentWithQuality> _sortFunction = (left, right) =>
        {
            return left.Quality > right.Quality ? -1 : (left.Quality == right.Quality ? 0 : 1);
        };

        private readonly ILogger _logger;
        private readonly IList<IOutputFormatter> _formatters;
        private readonly bool _respectBrowserAcceptHeader;
        private readonly bool _returnHttpNotAcceptable;

        public DefaultOutputFormatterSelector(IOptions<MvcOptions> options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<DefaultOutputFormatterSelector>();

            _formatters = new ReadOnlyCollection<IOutputFormatter>(options.Value.OutputFormatters);
            _respectBrowserAcceptHeader = options.Value.RespectBrowserAcceptHeader;
            _returnHttpNotAcceptable = options.Value.ReturnHttpNotAcceptable;
        }

        public override IOutputFormatter SelectFormatter(OutputFormatterCanWriteContext context, IList<IOutputFormatter> formatters, MediaTypeCollection contentTypes)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (formatters == null)
            {
                throw new ArgumentNullException(nameof(formatters));
            }

            if (contentTypes == null)
            {
                throw new ArgumentNullException(nameof(contentTypes));
            }

            ValidateContentTypes(contentTypes);

            if (formatters.Count == 0)
            {
                formatters = _formatters;
                if (formatters.Count == 0)
                {
                    throw new InvalidOperationException(Resources.FormatOutputFormattersAreRequired(
                        typeof(MvcOptions).FullName,
                        nameof(MvcOptions.OutputFormatters),
                        typeof(IOutputFormatter).FullName));
                }
            }

            _logger.RegisteredOutputFormatters(formatters);

            var request = context.HttpContext.Request;
            var acceptableMediaTypes = GetAcceptableMediaTypes(request);
            var selectFormatterWithoutRegardingAcceptHeader = false;

            IOutputFormatter selectedFormatter = null;
            if (acceptableMediaTypes.Count == 0)
            {
                // There is either no Accept header value, or it contained */* and we
                // are not currently respecting the 'browser accept header'.
                _logger.NoAcceptForNegotiation();

                selectFormatterWithoutRegardingAcceptHeader = true;
            }
            else
            {
                if (contentTypes.Count == 0)
                {
                    _logger.SelectingOutputFormatterUsingAcceptHeader(acceptableMediaTypes);

                    // Use whatever formatter can meet the client's request
                    selectedFormatter = SelectFormatterUsingSortedAcceptHeaders(
                        context,
                        formatters,
                        acceptableMediaTypes);
                }
                else
                {
                    _logger.SelectingOutputFormatterUsingAcceptHeaderAndExplicitContentTypes(acceptableMediaTypes, contentTypes);

                    // Verify that a content type from the context is compatible with the client's request
                    selectedFormatter = SelectFormatterUsingSortedAcceptHeadersAndContentTypes(
                        context,
                        formatters,
                        acceptableMediaTypes,
                        contentTypes);
                }

                if (selectedFormatter == null)
                {
                    _logger.NoFormatterFromNegotiation(acceptableMediaTypes);

                    if (!_returnHttpNotAcceptable)
                    {
                        selectFormatterWithoutRegardingAcceptHeader = true;
                    }
                }
            }

            if (selectFormatterWithoutRegardingAcceptHeader)
            {
                if (contentTypes.Count == 0)
                {
                    _logger.SelectingOutputFormatterWithoutUsingContentTypes();

                    selectedFormatter = SelectFormatterNotUsingContentType(
                        context,
                        formatters);
                }
                else
                {
                    _logger.SelectingOutputFormatterUsingContentTypes(contentTypes);

                    selectedFormatter = SelectFormatterUsingAnyAcceptableContentType(
                        context,
                        formatters,
                        contentTypes);
                }
            }

            if (selectedFormatter != null)
            {
                _logger.FormatterSelected(selectedFormatter, context);
            }

            return selectedFormatter;
        }

        private List<MediaTypeSegmentWithQuality> GetAcceptableMediaTypes(HttpRequest request)
        {
            var result = new List<MediaTypeSegmentWithQuality>();
            AcceptHeaderParser.ParseAcceptHeader(request.Headers[HeaderNames.Accept], result);
            for (var i = 0; i < result.Count; i++)
            {
                var mediaType = new MediaType(result[i].MediaType);
                if (!_respectBrowserAcceptHeader && mediaType.MatchesAllSubTypes && mediaType.MatchesAllTypes)
                {
                    result.Clear();
                    return result;
                }
            }

            result.Sort(_sortFunction);

            return result;
        }

        private IOutputFormatter SelectFormatterNotUsingContentType(
            OutputFormatterCanWriteContext formatterContext,
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

            _logger.SelectFirstCanWriteFormatter();

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

        private IOutputFormatter SelectFormatterUsingSortedAcceptHeaders(
            OutputFormatterCanWriteContext formatterContext,
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

        private IOutputFormatter SelectFormatterUsingAnyAcceptableContentType(
            OutputFormatterCanWriteContext formatterContext,
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

        private IOutputFormatter SelectFormatterUsingSortedAcceptHeadersAndContentTypes(
            OutputFormatterCanWriteContext formatterContext,
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
