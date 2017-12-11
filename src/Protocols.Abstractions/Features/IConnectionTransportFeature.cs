using System.Buffers;
using System.IO.Pipelines;
using System.Threading;

namespace Microsoft.AspNetCore.Protocols.Features
{
    public interface IConnectionTransportFeature
    {
        MemoryPool MemoryPool { get; }

        IPipeConnection Transport { get; set; }

        IPipeConnection Application { get; set; }

        IScheduler InputWriterScheduler { get; }

        IScheduler OutputReaderScheduler { get; }
    }
}
