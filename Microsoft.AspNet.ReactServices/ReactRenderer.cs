using System.Threading.Tasks;
using Microsoft.AspNet.NodeServices;

namespace Microsoft.AspNet.ReactServices
{
    public static class ReactRenderer
    {
        private static StringAsTempFile nodeScript;
        
        static ReactRenderer() {
            // Consider populating this lazily
            var script = EmbeddedResourceReader.Read(typeof (ReactRenderer), "/Content/Node/react-rendering.js");
            nodeScript = new StringAsTempFile(script); // Will be cleaned up on process exit
        }
        
        public static async Task<string> RenderToString(INodeServices nodeServices, string componentModuleName, string componentExportName, string requestUrl) {
            return await nodeServices.InvokeExport<string>(nodeScript.FileName, "renderToString", new {
                moduleName = componentModuleName,
                exportName = componentExportName,
                requestUrl = requestUrl
            });
        }
    }
}
