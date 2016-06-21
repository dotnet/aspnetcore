using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    public static class Prerenderer
    {
        private static readonly Lazy<StringAsTempFile> NodeScript;

        static Prerenderer()
        {
            NodeScript = new Lazy<StringAsTempFile>(() =>
            {
                var script = EmbeddedResourceReader.Read(typeof(Prerenderer), "/Content/Node/prerenderer.js");
                return new StringAsTempFile(script); // Will be cleaned up on process exit
            });
        }

        public static Task<RenderToStringResult> RenderToString(
            string applicationBasePath,
            INodeServices nodeServices,
            JavaScriptModuleExport bootModule,
            string requestAbsoluteUrl,
            string requestPathAndQuery,
            object customDataParameter)
        {
            return nodeServices.InvokeExport<RenderToStringResult>(
                NodeScript.Value.FileName,
                "renderToString",
                applicationBasePath,
                bootModule,
                requestAbsoluteUrl,
                requestPathAndQuery,
                customDataParameter);
        }
    }
}