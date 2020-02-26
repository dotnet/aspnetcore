using System.Collections.Generic;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public partial class Http3TestBase
    {
        internal class TestStreamContext : ConnectionContext, IStreamDirectionFeature, IStreamIdFeature, IProtocolErrorCodeFeature
        {
            private readonly IDuplexPipe _application;

            public TestStreamContext(bool canRead, bool canWrite, IDuplexPipe transport, IDuplexPipe application)
            {
                Features = new FeatureCollection();
                Features.Set<IStreamDirectionFeature>(this);
                Features.Set<IStreamIdFeature>(this);
                Features.Set<IProtocolErrorCodeFeature>(this);
                CanRead = canRead;
                CanWrite = canWrite;
                Transport = transport;
                _application = application;
            }

            public long Error { get; set; }

            public override string ConnectionId { get; set; }

            public long StreamId { get; }

            public override IFeatureCollection Features { get; }

            public override IDictionary<object, object> Items { get; set; }

            public override IDuplexPipe Transport { get; set; }

            public bool CanRead { get; }

            public bool CanWrite { get; }

            public override void Abort(ConnectionAbortedException abortReason)
            {
                _application.Output.Complete(abortReason);
            }
        }
    }
}
