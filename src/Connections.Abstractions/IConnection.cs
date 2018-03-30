using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Connections
{
    public interface IConnection
    {
        IDuplexPipe Transport { get; }
        IFeatureCollection Features { get; }

        Task StartAsync();
        Task StartAsync(TransferFormat transferFormat);
        Task DisposeAsync();
    }
}
