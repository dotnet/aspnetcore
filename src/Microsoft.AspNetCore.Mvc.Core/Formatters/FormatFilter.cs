// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// A filter that will use the format value in the route data or query string to set the content type on an
    /// <see cref="ObjectResult"/> returned from an action.
    /// </summary>
    public class FormatFilter : IFormatFilter, IResourceFilter, IResultFilter
    {
        private readonly MvcOptions _options;

        /// <summary>
        /// Initializes an instance of <see cref="FormatFilter"/>.
        /// </summary>
        /// <param name="options">The <see cref="IOptions{MvcOptions}"/></param>
        public FormatFilter(IOptions<MvcOptions> options)
        {
            _options = options.Value;
        }

        /// <inheritdoc />
        public virtual string GetFormat(ActionContext context)
        {
            if (context.RouteData.Values.TryGetValue("format", out var obj))
            {
                // null and string.Empty are equivalent for route values.
                var routeValue = obj?.ToString();
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
            if (supportedMediaTypes.Count != 0)
            {
                // We need to check if the action can generate the content type the user asked for. That is, treat the
                // request's format and IApiResponseMetadataProvider-provided content types similarly to an Accept
                // header and an output formatter's SupportedMediaTypes: Confirm action supports a more specific media
                // type than requested e.g. OK if "text/*" requested and action supports "text/plain".
                if (!IsSuperSetOfAnySupportedMediaType(contentType, supportedMediaTypes))
                {
                    context.Result = new NotFoundResult();
                }
            }
        }

        private bool IsSuperSetOfAnySupportedMediaType(string contentType, MediaTypeCollection supportedMediaTypes)
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

            var objectResult = context.Result as ObjectResult;
            if (objectResult == null)
            {
                return;
            }

            // If the action sets a single content type, then it takes precedence over the user
            // supplied content type based on format mapping.
            if ((objectResult.ContentTypes != null && objectResult.ContentTypes.Count == 1) ||
                !string.IsNullOrEmpty(context.HttpContext.Response.ContentType))
            {
                return;
            }

            var contentType = _options.FormatterMappings.GetMediaTypeMappingForFormat(format);
            objectResult.ContentTypes.Clear();
            objectResult.ContentTypes.Add(contentType);
        }

        /// <inheritdoc />
        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}
