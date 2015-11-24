// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Routing;

namespace UrlHelperSample.Web
{
    /// <summary>
    /// Following are some of the scenarios exercised here:
    /// 1. Based on configuration, generate Content urls pointing to local or a CDN server
    /// 2. Based on configuration, generate lower case urls
    /// </summary>
    public class CustomUrlHelper : UrlHelper
    {
        private readonly AppOptions _options;

        public CustomUrlHelper(ActionContext actionContext, AppOptions options)
            : base(actionContext)
        {
            _options = options;
        }

        /// <summary>
        /// Depending on config data, generates an absolute url pointing to a CDN server
        /// or falls back to the default behavior
        /// </summary>
        /// <param name="contentPath">The virtual path of the content.</param>
        /// <returns>The absolute url.</returns>
        public override string Content(string contentPath)
        {
            if (_options.ServeCDNContent
                && contentPath.StartsWith("~/", StringComparison.Ordinal))
            {
                var segment = new PathString(contentPath.Substring(1));

                return ConvertToLowercaseUrl(_options.CDNServerBaseUrl + segment);
            }

            return ConvertToLowercaseUrl(base.Content(contentPath));
        }

        public override string RouteUrl(UrlRouteContext routeContext)
        {
            return ConvertToLowercaseUrl(base.RouteUrl(routeContext));
        }

        public override string Action(UrlActionContext actionContext)
        {
            return ConvertToLowercaseUrl(base.Action(actionContext));
        }

        private string ConvertToLowercaseUrl(string url)
        {
            if (!string.IsNullOrEmpty(url)
                && _options.GenerateLowercaseUrls)
            {
                return url.ToLowerInvariant();
            }

            return url;
        }
    }
}