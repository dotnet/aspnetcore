using System.Threading.Tasks;
using Microsoft.AspNet.NodeServices;

namespace Microsoft.AspNet.AngularServices
{
    public static class AngularRenderer
    {
        private static StringAsTempFile nodeScript;
        
        static AngularRenderer() {
            // Consider populating this lazily
            var script = EmbeddedResourceReader.Read(typeof (AngularRenderer), "/Content/Node/angular-rendering.js");
            nodeScript = new StringAsTempFile(script); // Will be cleaned up on process exit
        }
        
        public static async Task<string> RenderToString(INodeServices nodeServices, string componentModuleName, string componentExportName, string componentTagName, string requestUrl) {
            return await nodeServices.InvokeExport<string>(nodeScript.FileName, "renderToString", new {
                moduleName = componentModuleName,
                exportName = componentExportName,
                tagName = componentTagName,
                requestUrl = requestUrl
            });
        }
    }
}
