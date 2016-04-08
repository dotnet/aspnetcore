using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    public static class Prerenderer
    {
        private static Lazy<StringAsTempFile> nodeScript;

        static Prerenderer() {
            nodeScript = new Lazy<StringAsTempFile>(() => {
                var script = EmbeddedResourceReader.Read(typeof(Prerenderer), "/Content/Node/prerenderer.js");
                return new StringAsTempFile(script); // Will be cleaned up on process exit
            });
        }

        public static async Task<RenderToStringResult> RenderToString(string applicationBasePath, INodeServices nodeServices, JavaScriptModuleExport bootModule, string requestAbsoluteUrl, string requestPathAndQuery) {
            return await nodeServices.InvokeExport<RenderToStringResult>(nodeScript.Value.FileName, "renderToString",
                applicationBasePath,
                bootModule,
                requestAbsoluteUrl,
                requestPathAndQuery);
        }
    }

    public class JavaScriptModuleExport {
        public string moduleName { get; private set; }
        public string exportName { get; set; }
        public string webpackConfig { get; set; }

        public JavaScriptModuleExport(string moduleName) {
            this.moduleName = moduleName;
        }
    }

    public class RenderToStringResult {
        public string Html;
        public JObject Globals;
    }
}
