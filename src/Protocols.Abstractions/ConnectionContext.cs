using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Protocols
{
    public abstract class ConnectionContext
    {
        public abstract string ConnectionId { get; set; }

        public abstract IFeatureCollection Features { get; }

        public abstract IPipeConnection Transport { get; set; }

        public abstract PipeFactory PipeFactory { get; }
    }
}
