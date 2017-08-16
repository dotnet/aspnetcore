using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;

namespace Microsoft.AspNetCore.Protocols.Features
{
    public interface IConnectionTransportFeature
    {
        PipeFactory PipeFactory { get; }

        IPipeConnection Connection { get; set; }

        IScheduler InputWriterScheduler { get; }

        IScheduler OutputReaderScheduler { get; }
    }
}
