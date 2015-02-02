// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace UrlHelperWebSite
{
    /// <summary>
    /// Following are some of the scenarios exercised here:
    /// 1. Based on configuration, generate Content urls pointing to local or a CDN server
    /// 2. Based on configuration, generate lower case urls
    /// </summary>
    public class CustomUrlHelper : UrlHelper
    {
        private readonly IOptions<AppOptions> _appOptions;
        private readonly HttpContext _httpContext;

        public CustomUrlHelper(IScopedInstance<ActionContext> contextAccessor, IActionSelector actionSelector,
                               IOptions<AppOptions> appOptions)
            : base(contextAccessor, actionSelector)
        {
            _appOptions = appOptions;
            _httpContext = contextAccessor.Value.HttpContext;
        }

        /// <summary>
        /// Depending on config data, generates an absolute url pointing to a CDN server
        /// or falls back to the default behavior
        /// </summary>
        /// <param name="contentPath"></param>
        /// <returns></returns>
        public override string Content(string contentPath)
        {
            if (_appOptions.Options.ServeCDNContent
                && contentPath.StartsWith("~/", StringComparison.Ordinal))
            {
                var segment = new PathString(contentPath.Substring(1));

                return ConvertToLowercaseUrl(_appOptions.Options.CDNServerBaseUrl + segment);
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
                && _appOptions.Options.GenerateLowercaseUrls)
            {
                return url.ToLowerInvariant();
            }

            return url;
        }
    }
}