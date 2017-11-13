using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Protocols.Features;

namespace Microsoft.AspNetCore.Protocols
{
    public class DefaultConnectionContext : ConnectionContext
    {
        private FeatureReferences<FeatureInterfaces> _features;

        public DefaultConnectionContext(IFeatureCollection features)
        {
            _features = new FeatureReferences<FeatureInterfaces>(features);
        }

        private IConnectionIdFeature ConnectionIdFeature =>
            _features.Fetch(ref _features.Cache.ConnectionId, _ => null);

        private IConnectionTransportFeature ConnectionTransportFeature =>
            _features.Fetch(ref _features.Cache.ConnectionTransport, _ => null);

        public override string ConnectionId
        {
            get => ConnectionIdFeature.ConnectionId;
            set => ConnectionIdFeature.ConnectionId = value;
        }

        public override IFeatureCollection Features => _features.Collection;

        public override BufferPool BufferPool => ConnectionTransportFeature.BufferPool;

        public override IPipeConnection Transport
        {
            get => ConnectionTransportFeature.Transport;
            set => ConnectionTransportFeature.Transport = value;
        }

        struct FeatureInterfaces
        {
            public IConnectionIdFeature ConnectionId;

            public IConnectionTransportFeature ConnectionTransport;
        }
    }
}
