using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    /// <summary>
    /// Performs server-side prerendering by invoking code in Node.js.
    /// </summary>
    public static class Prerenderer
    {
        private static readonly object CreateNodeScriptLock = new object();

        private static StringAsTempFile NodeScript;

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
            lock(CreateNodeScriptLock)
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