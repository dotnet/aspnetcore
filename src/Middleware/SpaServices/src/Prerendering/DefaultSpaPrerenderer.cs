// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.NodeServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    /// <summary>
    /// Default implementation of a DI service that provides convenient access to
    /// server-side prerendering APIs. This is an alternative to prerendering via
    /// the asp-prerender-module tag helper.
    /// </summary>
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    internal class DefaultSpaPrerenderer : ISpaPrerenderer
    {
        private readonly string _applicationBasePath;
        private readonly CancellationToken _applicationStoppingToken;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INodeServices _nodeServices;

        public DefaultSpaPrerenderer(
            INodeServices nodeServices,
            IHostApplicationLifetime applicationLifetime,
            IWebHostEnvironment hostingEnvironment,
            IHttpContextAccessor httpContextAccessor)
        {
            _applicationBasePath = hostingEnvironment.ContentRootPath;
            _applicationStoppingToken = applicationLifetime.ApplicationStopping;
            _httpContextAccessor = httpContextAccessor;
            _nodeServices = nodeServices;
        }

        public Task<RenderToStringResult> RenderToString(
            string moduleName,
            string exportName = null,
            object customDataParameter = null,
            int timeoutMilliseconds = default(int))
        {
            return Prerenderer.RenderToString(
                _applicationBasePath,
                _nodeServices,
                _applicationStoppingToken,
                new JavaScriptModuleExport(moduleName) { ExportName = exportName },
                _httpContextAccessor.HttpContext,
                customDataParameter,
                timeoutMilliseconds);
        }
    }
}
