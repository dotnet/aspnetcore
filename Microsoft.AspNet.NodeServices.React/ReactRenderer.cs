using System.Threading.Tasks;

namespace Microsoft.AspNet.NodeServices.React
{
    public static class ReactRenderer
    {
        private static StringAsTempFile nodeScript;
        private static NodeInstance nodeInstance = new NodeInstance();
        
        static ReactRenderer() {
            // Consider populating this lazily
            var script = EmbeddedResourceReader.Read(typeof (ReactRenderer), "/Content/Node/react-rendering.js");
            nodeScript = new StringAsTempFile(script); // Will be cleaned up on process exit
        }
        
        public static async Task<string> RenderToString(string moduleName, string exportName, string baseUrl) {
            return await nodeInstance.InvokeExport(nodeScript.FileName, "renderToString", new {
                moduleName,
                exportName,
                baseUrl
            });
        }
    }
}
