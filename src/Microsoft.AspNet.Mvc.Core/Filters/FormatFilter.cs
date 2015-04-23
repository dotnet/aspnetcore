// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ApiExplorer;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A filter which will use the format value in the route data or query string to set the content type on an 
    /// <see cref="ObjectResult" /> returned from an action.
    /// </summary>
    public class FormatFilter : IFormatFilter, IResourceFilter, IResultFilter
    {
        /// <summary>
        /// Initializes an instance of <see cref="FormatFilter"/>.
        /// </summary>
        /// <param name="options">The <see cref="IOptions{MvcOptions}"/></param>
        /// <param name="actionContext">The <see cref="IScopedInstance{ActionContext}"/></param>
        public FormatFilter(IOptions<MvcOptions> options, IScopedInstance<ActionContext> actionContext)
        {
            IsActive = true;
            Format = GetFormat(actionContext.Value);

            if (string.IsNullOrEmpty(Format))
            {
                IsActive = false;
                return;
            }

            ContentType = options.Options.FormatterMappings.GetMediaTypeMappingForFormat(Format);
        }

        /// <summary>
        /// Format value in the current request. <c>null</c> if format not present in the current request.
        /// </summary>
        public string Format { get; }

        /// <summary>
        /// <see cref="MediaTypeHeaderValue"/> for the format value in the current request.
        /// </summary>
        public MediaTypeHeaderValue ContentType { get; }

        /// <summary>
        /// <c>true</c> if the current <see cref="FormatFilter"/> is active and will execute. 
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// As a <see cref="IResourceFilter"/>, this filter looks at the request and rejects it before going ahead if
        /// 1. The format in the request doesnt match any format in the map.
        /// 2. If there is a conflicting producesFilter.
        /// </summary>
        /// <param name="context">The <see cref="ResourceExecutingContext"/>.</param>
        public void OnResourceExecuting([NotNull] ResourceExecutingContext context)
        {
            if (!IsActive)
            {
                return; // no format specified by user, so the filter is muted
            }

            if (ContentType == null)
            {
                // no contentType exists for the format, return 404
                context.Result = new HttpNotFoundResult();
                return;
            }

            var responseTypeFilters = context.Filters.OfType<IApiResponseMetadataProvider>();
            var contentTypes = new List<MediaTypeHeaderValue>();

            foreach (var filter in responseTypeFilters)
            {
                filter.SetContentTypes(contentTypes);
            }

            if (contentTypes.Count != 0)
            {
                // We need to check if the action can generate the content type the user asked for. If it cannot, exit
                // here with not found result. 
                if (!contentTypes.Any(c => ContentType.IsSubsetOf(c)))
                {
                    context.Result = new HttpNotFoundResult();
                }
            }
        }

        /// <inheritdoc />
        public void OnResourceExecuted([NotNull] ResourceExecutedContext context)
        {
        }

        /// <summary>
        /// Sets a Content Type on an  <see cref="ObjectResult" />  using a format value from the request.
        /// </summary>
        /// <param name="context">The <see cref="ResultExecutingContext"/>.</param>
        public void OnResultExecuting([NotNull] ResultExecutingContext context)
        {
            if (!IsActive)
            {
                return; // no format specified by user, so the filter is muted
            }

            var objectResult = context.Result as ObjectResult;
            if (objectResult != null)
            {
                objectResult.ContentTypes.Clear();
                objectResult.ContentTypes.Add(ContentType);
            }
        }

        /// <inheritdoc />
        public void OnResultExecuted([NotNull] ResultExecutedContext context)
        {
        }

        private string GetFormat(ActionContext context)
        {
            object format = null;

            if (!context.RouteData.Values.TryGetValue("format", out format))
            {
                format = context.HttpContext.Request.Query["format"];
            }

            if (format != null)
            {
                return format.ToString();
            }

            return null;
        }
    }
}