using System.Threading.Tasks;

namespace Microsoft.AspNet.NodeServices.React
{
    public static class ReactRenderer
    {
        private static StringAsTempFile nodeScript;
        
        static ReactRenderer() {
            // Consider populating this lazily
            var script = EmbeddedResourceReader.Read(typeof (ReactRenderer), "/Content/Node/react-rendering.js");
            nodeScript = new StringAsTempFile(script); // Will be cleaned up on process exit
        }
        
        public static async Task<string> RenderToString(INodeServices nodeServices, string moduleName, string exportName, string baseUrl) {
            return await nodeServices.InvokeExport(nodeScript.FileName, "renderToString", new {
                moduleName,
                exportName,
                baseUrl
            });
        }
    }
}
