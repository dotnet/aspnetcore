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

        Scheduler InputWriterScheduler { get; }

        Scheduler OutputReaderScheduler { get; }
    }
}
