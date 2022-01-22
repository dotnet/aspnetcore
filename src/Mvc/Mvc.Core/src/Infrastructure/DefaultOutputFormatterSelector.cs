// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// The default implementation of <see cref="OutputFormatterSelector"/>.
/// </summary>
public partial class DefaultOutputFormatterSelector : OutputFormatterSelector
{
    private static readonly Comparison<MediaTypeSegmentWithQuality> _sortFunction = (left, right) =>
    {
        return left.Quality > right.Quality ? -1 : (left.Quality == right.Quality ? 0 : 1);
    };

    private readonly ILogger _logger;
    private readonly IList<IOutputFormatter> _formatters;
    private readonly bool _respectBrowserAcceptHeader;
    private readonly bool _returnHttpNotAcceptable;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultOutputFormatterSelector"/>
    /// </summary>
    /// <param name="options">Used to access <see cref="MvcOptions"/>.</param>
    /// <param name="loggerFactory">The logger factory.</param>
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

    /// <inheritdoc/>
    public override IOutputFormatter? SelectFormatter(OutputFormatterCanWriteContext context, IList<IOutputFormatter> formatters, MediaTypeCollection contentTypes)
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

        Log.RegisteredOutputFormatters(_logger, formatters);

        var request = context.HttpContext.Request;
        var acceptableMediaTypes = GetAcceptableMediaTypes(request);
        var selectFormatterWithoutRegardingAcceptHeader = false;

        IOutputFormatter? selectedFormatter = null;
        if (acceptableMediaTypes.Count == 0)
        {
            // There is either no Accept header value, or it contained */* and we
            // are not currently respecting the 'browser accept header'.
            Log.NoAcceptForNegotiation(_logger);

            selectFormatterWithoutRegardingAcceptHeader = true;
        }
        else
        {
            if (contentTypes.Count == 0)
            {
                Log.SelectingOutputFormatterUsingAcceptHeader(_logger, acceptableMediaTypes);

                // Use whatever formatter can meet the client's request
                selectedFormatter = SelectFormatterUsingSortedAcceptHeaders(
                    context,
                    formatters,
                    acceptableMediaTypes);
            }
            else
            {
                Log.SelectingOutputFormatterUsingAcceptHeaderAndExplicitContentTypes(_logger, acceptableMediaTypes, contentTypes);

                // Verify that a content type from the context is compatible with the client's request
                selectedFormatter = SelectFormatterUsingSortedAcceptHeadersAndContentTypes(
                    context,
                    formatters,
                    acceptableMediaTypes,
                    contentTypes);
            }

            if (selectedFormatter == null)
            {
                Log.NoFormatterFromNegotiation(_logger, acceptableMediaTypes);

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
                Log.SelectingOutputFormatterWithoutUsingContentTypes(_logger);

                selectedFormatter = SelectFormatterNotUsingContentType(
                    context,
                    formatters);
            }
            else
            {
                Log.SelectingOutputFormatterUsingContentTypes(_logger, contentTypes);

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
        AcceptHeaderParser.ParseAcceptHeader(request.Headers.Accept, result);
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

    private IOutputFormatter? SelectFormatterNotUsingContentType(
        OutputFormatterCanWriteContext formatterContext,
        IList<IOutputFormatter> formatters)
    {
        Log.SelectFirstCanWriteFormatter(_logger);

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

    private static IOutputFormatter? SelectFormatterUsingSortedAcceptHeaders(
        OutputFormatterCanWriteContext formatterContext,
        IList<IOutputFormatter> formatters,
        IList<MediaTypeSegmentWithQuality> sortedAcceptHeaders)
    {
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

    private static IOutputFormatter? SelectFormatterUsingAnyAcceptableContentType(
        OutputFormatterCanWriteContext formatterContext,
        IList<IOutputFormatter> formatters,
        MediaTypeCollection acceptableContentTypes)
    {
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

    private static IOutputFormatter? SelectFormatterUsingSortedAcceptHeadersAndContentTypes(
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

    private static void ValidateContentTypes(MediaTypeCollection contentTypes)
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

    private static partial class Log
    {
        [LoggerMessage(4, LogLevel.Debug, "No information found on request to perform content negotiation.", EventName = "NoAcceptForNegotiation")]
        public static partial void NoAcceptForNegotiation(ILogger logger);

        [LoggerMessage(5, LogLevel.Debug, "Could not find an output formatter based on content negotiation. Accepted types were ({AcceptTypes})", EventName = "NoFormatterFromNegotiation")]
        public static partial void NoFormatterFromNegotiation(ILogger logger, IList<MediaTypeSegmentWithQuality> acceptTypes);

        [LoggerMessage(6, LogLevel.Debug, "Attempting to select an output formatter based on Accept header '{AcceptHeader}'.", EventName = "SelectingOutputFormatterUsingAcceptHeader")]
        public static partial void SelectingOutputFormatterUsingAcceptHeader(ILogger logger, IEnumerable<MediaTypeSegmentWithQuality> acceptHeader);

        [LoggerMessage(7, LogLevel.Debug, "Attempting to select an output formatter based on Accept header '{AcceptHeader}' and explicitly specified content types '{ExplicitContentTypes}'. The content types in the accept header must be a subset of the explicitly set content types.", EventName = "SelectingOutputFormatterUsingAcceptHeaderAndExplicitContentTypes")]
        public static partial void SelectingOutputFormatterUsingAcceptHeaderAndExplicitContentTypes(ILogger logger, IEnumerable<MediaTypeSegmentWithQuality> acceptHeader, MediaTypeCollection explicitContentTypes);

        [LoggerMessage(8, LogLevel.Debug, "Attempting to select an output formatter without using a content type as no explicit content types were specified for the response.", EventName = "SelectingOutputFormatterWithoutUsingContentTypes")]
        public static partial void SelectingOutputFormatterWithoutUsingContentTypes(ILogger logger);

        [LoggerMessage(9, LogLevel.Debug, "Attempting to select the first output formatter in the output formatters list which supports a content type from the explicitly specified content types '{ExplicitContentTypes}'.", EventName = "SelectingOutputFormatterUsingContentTypes")]
        public static partial void SelectingOutputFormatterUsingContentTypes(ILogger logger, MediaTypeCollection explicitContentTypes);

        [LoggerMessage(10, LogLevel.Debug, "Attempting to select the first formatter in the output formatters list which can write the result.", EventName = "SelectingFirstCanWriteFormatter")]
        public static partial void SelectFirstCanWriteFormatter(ILogger logger);

        [LoggerMessage(11, LogLevel.Debug, "List of registered output formatters, in the following order: {OutputFormatters}", EventName = "RegisteredOutputFormatters")]
        public static partial void RegisteredOutputFormatters(ILogger logger, IEnumerable<IOutputFormatter> outputFormatters);
    }
}
