using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Protocols.Features
{
    public interface IConnectionTransportFeature
    {
        PipeFactory PipeFactory { get; }

        IPipeConnection Transport { get; set; }

        IPipeConnection Application { get; set; }

        IScheduler InputWriterScheduler { get; }

        IScheduler OutputReaderScheduler { get; }

        Task ConnectionAborted { get; }

        Task ConnectionClosed { get; }
    }
}
