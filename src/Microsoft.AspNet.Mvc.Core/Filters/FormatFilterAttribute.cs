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
    /// This will look at the format parameter if present in the route data or query data and 
    /// sets the content type in ObjectResult corresponding to the format value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class FormatFilterAttribute : Attribute, IFormatFilter, IResourceFilter, IResultFilter
    {
        /// <summary>
        /// As a resourceFilter, this filter looks at the request and rejects it
        /// before going ahead if
        /// 1. The Format in the request doesnt match any format in the map.
        /// 2. If there is a conflicting producesFilter.
        /// </summary>
        /// <param name="context"></param>
        public void OnResourceExecuting([NotNull] ResourceExecutingContext context)
        {
            var format = GetFormat(context);

            if (format != null && !string.IsNullOrEmpty(format.ToString()))
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
                    if (responseTypeFilters.Count() != 0)
                    {
                        var contentTypes = new List<MediaTypeHeaderValue>();
                        foreach (var filter in responseTypeFilters)
                        {
                            filter.SetContentTypes(contentTypes);
                        }

                        if (!contentTypes.Any(c => c.IsSubsetOf(formatContentType)))
                        {
                            context.Result = new HttpNotFoundResult();
                        }
                    }
                }
            }
        }

        public void OnResourceExecuted([NotNull] ResourceExecutedContext context)
        {

        }

        public void OnResultExecuting([NotNull] ResultExecutingContext context)
        {
            var format = GetFormat(context);
            if (format != null)
            {
                var contentType = GetContentType(format, context);
                Debug.Assert(contentType != null);

                var objectResult = context.Result as ObjectResult;
                if (objectResult != null)
                {
                    objectResult.ContentTypes.Clear();
                    objectResult.ContentTypes.Add(contentType);
                }
            }
        }

        public void OnResultExecuted([NotNull] ResultExecutedContext context)
        {

        }

        public MediaTypeHeaderValue GetContentTypeForCurrentRequest(FilterContext context)
        {
            var format = GetFormat(context);
            if (format != null && !string.IsNullOrEmpty(format.ToString()))
            {
                return GetContentType(format, context);
            }

            return null;
        }

        private object GetFormat(FilterContext context)
        {
            object format = null;

            if (!context.RouteData.Values.TryGetValue("format", out format))
            {
                if (context.HttpContext.Request.Query.ContainsKey("format"))
                {
                    format = context.HttpContext.Request.Query.Get("format");
                }
            }

            return format;
        }

        private MediaTypeHeaderValue GetContentType(object format, FilterContext context)
        {
            Debug.Assert(format != null);
            var options = context.HttpContext.RequestServices.GetService<IOptions<MvcOptions>>();
            var contentType = options.Options.FormatterMappings.GetContentTypeForFormat(format.ToString());
            return contentType;
        }
    }
}