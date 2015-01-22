// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A filter which will use the format value in the route data or query string to set the content type on an 
    /// <see cref="ObjectResult" /> returned from an action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class FormatFilterAttribute : Attribute, IFormatFilter, IResourceFilter, IResultFilter
    {
        /// <summary>
        /// As a <see cref="IResourceFilter"/>, this filter looks at the request and rejects it before going ahead if
        /// 1. The format in the request doesnt match any format in the map.
        /// 2. If there is a conflicting producesFilter.
        /// </summary>
        /// <param name="context">The <see cref="ResourceExecutingContext"/>.</param>
        public void OnResourceExecuting([NotNull] ResourceExecutingContext context)
        {
            var format = GetFormat(context);

            if (!string.IsNullOrEmpty(format))
            {
                var formatContentType = GetContentType(format, context);
                if (formatContentType == null)
                {
                    // no contentType exists for the format, return 404
                    context.Result = new HttpNotFoundResult();
                }
                else
                {
                    var responseTypeFilters = context.Filters.OfType<IApiResponseMetadataProvider>();                    
                    var contentTypes = new List<MediaTypeHeaderValue>();

                    foreach (var filter in responseTypeFilters)
                    {
                        filter.SetContentTypes(contentTypes);
                    }

                    if (contentTypes.Count() != 0)
                    {
                        // If formatfilterContentType is not subset of any of the content types produced by 
                        // IApiResponseMetadataProviders, return 404
                        if (!contentTypes.Any(c => formatContentType.IsSubsetOf(c)))
                        {
                            context.Result = new HttpNotFoundResult();
                        }
                    }                   
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
            var format = GetFormat(context);
            if (!string.IsNullOrEmpty(format))
            {
                var objectResult = context.Result as ObjectResult;
                if (objectResult != null)
                {
                    var contentType = GetContentType(format, context);
                    Debug.Assert(contentType != null);
                    objectResult.ContentTypes.Clear();
                    objectResult.ContentTypes.Add(contentType);
                }
            }
        }
        
        /// <inheritdoc />     
        public void OnResultExecuted([NotNull] ResultExecutedContext context)
        {

        }

        /// <summary>
        /// If the current request contains format value, returns true. It means the format filter is going to execute.
        /// </summary>
        /// <param name="context">The <see cref="FilterContext"/></param>
        /// <returns>If the filter is active and will execute.</returns>
        public bool IsActive(FilterContext context)
        {
            var format = GetFormat(context);

            return !string.IsNullOrEmpty(format);
        }

        private string GetFormat(FilterContext context)
        {
            object format = null;

            if (!context.RouteData.Values.TryGetValue("format", out format))
            {
                format = context.HttpContext.Request.Query["format"];
            }

            return (string)format;
        }

        private MediaTypeHeaderValue GetContentType(string format, FilterContext context)
        {
            var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>();
            var contentType = options.Options.FormatterMappings.GetMediaTypeMappingForFormat(format);

            return contentType;
        }
    }
}