using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Protocols.Features
{
    public interface IConnectionTransportFeature
    {
        PipeFactory PipeFactory { get; }

        IPipeConnection Transport { get; set; }

        IPipeConnection Application { get; set; }

        IScheduler InputWriterScheduler { get; }

        IScheduler OutputReaderScheduler { get; }
    }
}
