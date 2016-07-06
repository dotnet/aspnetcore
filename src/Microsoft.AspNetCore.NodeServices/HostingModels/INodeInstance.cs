using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices.HostingModels
{
    public interface INodeInstance : IDisposable
    {
        Task<T> InvokeExportAsync<T>(string moduleName, string exportNameOrNull, params object[] args);
    }
}