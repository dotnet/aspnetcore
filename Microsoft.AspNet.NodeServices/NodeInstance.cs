using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.NodeServices {
    public class NodeInstance : IDisposable {
        private readonly NodeHost _nodeHost;
    
        public NodeInstance(NodeHostingModel hostingModel = NodeHostingModel.Http) {
            switch (hostingModel) {
                case NodeHostingModel.Http:
                    this._nodeHost = new HttpNodeHost();
                    break;
                case NodeHostingModel.InputOutputStream:
                    this._nodeHost = new InputOutputStreamNodeHost();
                    break;
                default:
                    throw new ArgumentException("Unknown hosting model: " + hostingModel.ToString());
            }
        }
    
        public Task<string> Invoke(string moduleName, params object[] args) {
            return this.InvokeExport(moduleName, null, args);
        }
    
        public async Task<string> InvokeExport(string moduleName, string exportedFunctionName, params object[] args) {
            return await this._nodeHost.Invoke(new NodeInvocationInfo {
                ModuleName = moduleName,
                ExportedFunctionName = exportedFunctionName,
                Args = args
            });
        }
    
        public void Dispose()
        {
            this._nodeHost.Dispose();
        }
    }
}
