using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public interface ITransport : IDisposable
    {
        Task StartAsync(Uri url, IPipelineConnection pipeline);
    }
}
