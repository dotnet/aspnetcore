// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    /// <summary>
    /// A tag helper for prerendering JavaScript applications on the server.
    /// </summary>
    [HtmlTargetElement(Attributes = PrerenderModuleAttributeName)]
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public class PrerenderTagHelper : TagHelper
    {
        private const string PrerenderModuleAttributeName = "asp-prerender-module";
        private const string PrerenderExportAttributeName = "asp-prerender-export";
        private const string PrerenderDataAttributeName = "asp-prerender-data";
        private const string PrerenderTimeoutAttributeName = "asp-prerender-timeout";
        private static INodeServices _fallbackNodeServices; // Used only if no INodeServices was registered with DI

        private readonly string _applicationBasePath;
        private readonly CancellationToken _applicationStoppingToken;
        private readonly INodeServices _nodeServices;

        /// <summary>
        /// Creates a new instance of <see cref="PrerenderTagHelper"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        public PrerenderTagHelper(IServiceProvider serviceProvider)
        {
            var hostEnv = (IWebHostEnvironment)serviceProvider.GetService(typeof(IWebHostEnvironment));
            _nodeServices = (INodeServices)serviceProvider.GetService(typeof(INodeServices)) ?? _fallbackNodeServices;
            _applicationBasePath = hostEnv.ContentRootPath;

            var applicationLifetime = (IHostApplicationLifetime)serviceProvider.GetService(typeof(IHostApplicationLifetime));
            _applicationStoppingToken = applicationLifetime.ApplicationStopping;

            // Consider removing the following. Having it means you can get away with not putting app.AddNodeServices()
            // in your startup file, but then again it might be confusing that you don't need to.
            if (_nodeServices == null)
            {
                _nodeServices = _fallbackNodeServices = NodeServicesFactory.CreateNodeServices(
                    new NodeServicesOptions(serviceProvider));
            }
        }

        /// <summary>
        /// Specifies the path to the JavaScript module containing prerendering code.
        /// </summary>
        [HtmlAttributeName(PrerenderModuleAttributeName)]
        public string ModuleName { get; set; }

        /// <summary>
        /// If set, specifies the name of the CommonJS export that is the prerendering function to execute.
        /// If not set, the JavaScript module's default CommonJS export must itself be the prerendering function.
        /// </summary>
        [HtmlAttributeName(PrerenderExportAttributeName)]
        public string ExportName { get; set; }

        /// <summary>
        /// An optional JSON-serializable parameter to be supplied to the prerendering code.
        /// </summary>
        [HtmlAttributeName(PrerenderDataAttributeName)]
        public object CustomDataParameter { get; set; }

        /// <summary>
        /// The maximum duration to wait for prerendering to complete.
        /// </summary>
        [HtmlAttributeName(PrerenderTimeoutAttributeName)]
        public int TimeoutMillisecondsParameter { get; set; }

        /// <summary>
        /// The <see cref="ViewContext"/>.
        /// </summary>
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// Executes the tag helper to perform server-side prerendering.
        /// </summary>
        /// <param name="context">The <see cref="TagHelperContext"/>.</param>
        /// <param name="output">The <see cref="TagHelperOutput"/>.</param>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var result = await Prerenderer.RenderToString(
                _applicationBasePath,
                _nodeServices,
                _applicationStoppingToken,
                new JavaScriptModuleExport(ModuleName)
                {
                    ExportName = ExportName
                },
                ViewContext.HttpContext,
                CustomDataParameter,
                TimeoutMillisecondsParameter);

            if (!string.IsNullOrEmpty(result.RedirectUrl))
            {
                // It's a redirection
                var permanentRedirect = result.StatusCode.GetValueOrDefault() == 301;
                ViewContext.HttpContext.Response.Redirect(result.RedirectUrl, permanentRedirect);
                return;
            }

            if (result.StatusCode.HasValue)
            {
                ViewContext.HttpContext.Response.StatusCode = result.StatusCode.Value;
            }

            // It's some HTML to inject
            output.Content.SetHtmlContent(result.Html);

            // Also attach any specified globals to the 'window' object. This is useful for transferring
            // general state between server and client.
            var globalsScript = result.CreateGlobalsAssignmentScript();
            if (!string.IsNullOrEmpty(globalsScript))
            {
                output.PostElement.SetHtmlContent($"<script>{globalsScript}</script>");
            }
        }
    }
}
