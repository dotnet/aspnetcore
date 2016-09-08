using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices.HostingModels
{
    public interface INodeInstance : IDisposable
    {
        Task<T> InvokeExportAsync<T>(CancellationToken cancellationToken, string moduleName, string exportNameOrNull, params object[] args);
    }
}