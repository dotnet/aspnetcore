using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices
{
    public interface INodeServices : IDisposable
    {
        Task<T> InvokeAsync<T>(string moduleName, params object[] args);
        Task<T> InvokeAsync<T>(CancellationToken cancellationToken, string moduleName, params object[] args);

        Task<T> InvokeExportAsync<T>(string moduleName, string exportedFunctionName, params object[] args);
        Task<T> InvokeExportAsync<T>(CancellationToken cancellationToken, string moduleName, string exportedFunctionName, params object[] args);


        [Obsolete("Use InvokeAsync instead")]
        Task<T> Invoke<T>(string moduleName, params object[] args);

        [Obsolete("Use InvokeExportAsync instead")]
        Task<T> InvokeExport<T>(string moduleName, string exportedFunctionName, params object[] args);
    }
}