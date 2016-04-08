using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices {
    public interface INodeServices : IDisposable {
        Task<T> Invoke<T>(string moduleName, params object[] args);

        Task<T> InvokeExport<T>(string moduleName, string exportedFunctionName, params object[] args);
    }
}
