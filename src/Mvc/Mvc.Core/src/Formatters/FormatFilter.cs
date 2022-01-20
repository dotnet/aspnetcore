// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// A filter that will use the format value in the route data or query string to set the content type on an
/// <see cref="ObjectResult"/> returned from an action.
/// </summary>
public partial class FormatFilter : IFormatFilter, IResourceFilter, IResultFilter
{
    private readonly MvcOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes an instance of <see cref="FormatFilter"/>.
    /// </summary>
    /// <param name="options">The <see cref="IOptions{MvcOptions}"/></param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public FormatFilter(IOptions<MvcOptions> options, ILoggerFactory loggerFactory)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        _options = options.Value;
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <inheritdoc />
    public virtual string? GetFormat(ActionContext context)
    {
        if (context.RouteData.Values.TryGetValue("format", out var obj))
        {
            // null and string.Empty are equivalent for route values.
            var routeValue = Convert.ToString(obj, CultureInfo.InvariantCulture);
            return string.IsNullOrEmpty(routeValue) ? null : routeValue;
        }

        var query = context.HttpContext.Request.Query["format"];
        if (query.Count > 0)
        {
            return query.ToString();
        }

        return null;
    }

    /// <summary>
    /// As a <see cref="IResourceFilter"/>, this filter looks at the request and rejects it before going ahead if
    /// 1. The format in the request does not match any format in the map.
    /// 2. If there is a conflicting producesFilter.
    /// </summary>
    /// <param name="context">The <see cref="ResourceExecutingContext"/>.</param>
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var format = GetFormat(context);
        if (format == null)
        {
            // no format specified by user, so the filter is muted
            return;
        }

        var contentType = _options.FormatterMappings.GetMediaTypeMappingForFormat(format);
        if (contentType == null)
        {
            Log.UnsupportedFormatFilterContentType(_logger, format);

            // no contentType exists for the format, return 404
            context.Result = new NotFoundResult();
            return;
        }

        // Determine media types this action supports.
        var responseTypeFilters = context.Filters.OfType<IApiResponseMetadataProvider>();
        var supportedMediaTypes = new MediaTypeCollection();
        foreach (var filter in responseTypeFilters)
        {
            filter.SetContentTypes(supportedMediaTypes);
        }

        // Check if support is adequate for requested media type.
        if (supportedMediaTypes.Count == 0)
        {
            Log.ActionDoesNotExplicitlySpecifyContentTypes(_logger);
            return;
        }

        // We need to check if the action can generate the content type the user asked for. That is, treat the
        // request's format and IApiResponseMetadataProvider-provided content types similarly to an Accept
        // header and an output formatter's SupportedMediaTypes: Confirm action supports a more specific media
        // type than requested e.g. OK if "text/*" requested and action supports "text/plain".
        if (!IsSuperSetOfAnySupportedMediaType(contentType, supportedMediaTypes))
        {
            _logger.ActionDoesNotSupportFormatFilterContentType(contentType, supportedMediaTypes);
            context.Result = new NotFoundResult();
        }
    }

    private static bool IsSuperSetOfAnySupportedMediaType(string contentType, MediaTypeCollection supportedMediaTypes)
    {
        var parsedContentType = new MediaType(contentType);
        for (var i = 0; i < supportedMediaTypes.Count; i++)
        {
            var supportedMediaType = new MediaType(supportedMediaTypes[i]);
            if (supportedMediaType.IsSubsetOf(parsedContentType))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }

    /// <summary>
    /// Sets a Content Type on an  <see cref="ObjectResult" />  using a format value from the request.
    /// </summary>
    /// <param name="context">The <see cref="ResultExecutingContext"/>.</param>
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var format = GetFormat(context);
        if (format == null)
        {
            // no format specified by user, so the filter is muted
            return;
        }

        if (context.Result is not ObjectResult objectResult)
        {
            return;
        }

        // If the action sets a single content type, then it takes precedence over the user
        // supplied content type based on format mapping.
        if (objectResult.ContentTypes.Count == 1 ||
            !string.IsNullOrEmpty(context.HttpContext.Response.ContentType))
        {
            Log.CannotApplyFormatFilterContentType(_logger, format);
            return;
        }

        var contentType = _options.FormatterMappings.GetMediaTypeMappingForFormat(format);
        objectResult.ContentTypes.Clear();
        if (!string.IsNullOrEmpty(contentType))
        {
            objectResult.ContentTypes.Add(contentType);
        }
    }

    /// <inheritdoc />
    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Could not find a media type for the format '{FormatFilterContentType}'.", EventName = "UnsupportedFormatFilterContentType")]
        public static partial void UnsupportedFormatFilterContentType(ILogger logger, string formatFilterContentType);

        [LoggerMessage(3, LogLevel.Debug, "Cannot apply content type '{FormatFilterContentType}' to the response as current action had explicitly set a preferred content type.", EventName = "CannotApplyFormatFilterContentType")]
        public static partial void CannotApplyFormatFilterContentType(ILogger logger, string formatFilterContentType);

        [LoggerMessage(5, LogLevel.Debug, "Current action does not explicitly specify any content types for the response.", EventName = "ActionDoesNotExplicitlySpecifyContentTypes")]
        public static partial void ActionDoesNotExplicitlySpecifyContentTypes(ILogger logger);
    }
}
