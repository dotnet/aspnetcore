using System.Collections.Generic;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Connections
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

        private IConnectionItemsFeature ConnectionItemsFeature =>
            _features.Fetch(ref _features.Cache.ConnectionItems, _ => null);

        public override string ConnectionId
        {
            get => ConnectionIdFeature.ConnectionId;
            set => ConnectionIdFeature.ConnectionId = value;
        }

        public override IFeatureCollection Features => _features.Collection;

        public override IDuplexPipe Transport
        {
            get => ConnectionTransportFeature.Transport;
            set => ConnectionTransportFeature.Transport = value;
        }

        public override IDictionary<object, object> Items
        {
            get => ConnectionItemsFeature.Items;
            set => ConnectionItemsFeature.Items = value;
        }

        struct FeatureInterfaces
        {
            public IConnectionIdFeature ConnectionId;

            public IConnectionTransportFeature ConnectionTransport;

            public IConnectionItemsFeature ConnectionItems;
        }
    }
}
