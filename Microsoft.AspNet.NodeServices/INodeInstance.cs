using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.NodeServices {
    public interface INodeServices : IDisposable {    
        Task<string> Invoke(string moduleName, params object[] args);
    
        Task<string> InvokeExport(string moduleName, string exportedFunctionName, params object[] args);
    }
}
