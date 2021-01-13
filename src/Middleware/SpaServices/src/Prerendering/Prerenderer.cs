// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    /// <summary>
    /// Performs server-side prerendering by invoking code in Node.js.
    /// </summary>
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public static class Prerenderer
    {
        private static readonly object CreateNodeScriptLock = new object();

        private static StringAsTempFile NodeScript;

        [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
        internal static Task<RenderToStringResult> RenderToString(
            string applicationBasePath,
            INodeServices nodeServices,
            CancellationToken applicationStoppingToken,
            JavaScriptModuleExport bootModule,
            HttpContext httpContext,
            object customDataParameter,
            int timeoutMilliseconds)
        {
            // We want to pass the original, unencoded incoming URL data through to Node, so that
            // server-side code has the same view of the URL as client-side code (on the client,
            // location.pathname returns an unencoded string).
            // The following logic handles special characters in URL paths in the same way that
            // Node and client-side JS does. For example, the path "/a=b%20c" gets passed through
            // unchanged (whereas other .NET APIs do change it - Path.Value will return it as
            // "/a=b c" and Path.ToString() will return it as "/a%3db%20c")
            var requestFeature = httpContext.Features.Get<IHttpRequestFeature>();
            var unencodedPathAndQuery = requestFeature.RawTarget;

            var request = httpContext.Request;
            var unencodedAbsoluteUrl = $"{request.Scheme}://{request.Host}{unencodedPathAndQuery}";

            return RenderToString(
                applicationBasePath,
                nodeServices,
                applicationStoppingToken,
                bootModule,
                unencodedAbsoluteUrl,
                unencodedPathAndQuery,
                customDataParameter,
                timeoutMilliseconds,
                request.PathBase.ToString());
        }

        /// <summary>
        /// Performs server-side prerendering by invoking code in Node.js.
        /// </summary>
        /// <param name="applicationBasePath">The root path to your application. This is used when resolving project-relative paths.</param>
        /// <param name="nodeServices">The instance of <see cref="INodeServices"/> that will be used to invoke JavaScript code.</param>
        /// <param name="applicationStoppingToken">A token that indicates when the host application is stopping.</param>
        /// <param name="bootModule">The path to the JavaScript file containing the prerendering logic.</param>
        /// <param name="requestAbsoluteUrl">The URL of the currently-executing HTTP request. This is supplied to the prerendering code.</param>
        /// <param name="requestPathAndQuery">The path and query part of the URL of the currently-executing HTTP request. This is supplied to the prerendering code.</param>
        /// <param name="customDataParameter">An optional JSON-serializable parameter to be supplied to the prerendering code.</param>
        /// <param name="timeoutMilliseconds">The maximum duration to wait for prerendering to complete.</param>
        /// <param name="requestPathBase">The PathBase for the currently-executing HTTP request.</param>
        /// <returns></returns>
        [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
        public static Task<RenderToStringResult> RenderToString(
            string applicationBasePath,
            INodeServices nodeServices,
            CancellationToken applicationStoppingToken,
            JavaScriptModuleExport bootModule,
            string requestAbsoluteUrl,
            string requestPathAndQuery,
            object customDataParameter,
            int timeoutMilliseconds,
            string requestPathBase)
        {
            return nodeServices.InvokeExportAsync<RenderToStringResult>(
                GetNodeScriptFilename(applicationStoppingToken),
                "renderToString",
                applicationBasePath,
                bootModule,
                requestAbsoluteUrl,
                requestPathAndQuery,
                customDataParameter,
                timeoutMilliseconds,
                requestPathBase);
        }

        private static string GetNodeScriptFilename(CancellationToken applicationStoppingToken)
        {
            lock (CreateNodeScriptLock)
            {
                if (NodeScript == null)
                {
                    var script = EmbeddedResourceReader.Read(typeof(Prerenderer), "/Content/Node/prerenderer.js");
                    NodeScript = new StringAsTempFile(script, applicationStoppingToken); // Will be cleaned up on process exit
                }
            }

            return NodeScript.FileName;
        }
    }
}
