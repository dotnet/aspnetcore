using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Protocols.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public partial class TransportConnection : IFeatureCollection,
                                               IHttpConnectionFeature,
                                               IConnectionIdFeature,
                                               IConnectionTransportFeature
    {
        private static readonly Type IHttpConnectionFeatureType = typeof(IHttpConnectionFeature);
        private static readonly Type IConnectionIdFeatureType = typeof(IConnectionIdFeature);
        private static readonly Type IConnectionTransportFeatureType = typeof(IConnectionTransportFeature);

        private object _currentIHttpConnectionFeature;
        private object _currentIConnectionIdFeature;
        private object _currentIConnectionTransportFeature;

        private int _featureRevision;

        private List<KeyValuePair<Type, object>> MaybeExtra;

        private object ExtraFeatureGet(Type key)
        {
            if (MaybeExtra == null)
            {
                return null;
            }
            for (var i = 0; i < MaybeExtra.Count; i++)
            {
                var kv = MaybeExtra[i];
                if (kv.Key == key)
                {
                    return kv.Value;
                }
            }
            return null;
        }

        private void ExtraFeatureSet(Type key, object value)
        {
            if (MaybeExtra == null)
            {
                MaybeExtra = new List<KeyValuePair<Type, object>>(2);
            }

            for (var i = 0; i < MaybeExtra.Count; i++)
            {
                if (MaybeExtra[i].Key == key)
                {
                    MaybeExtra[i] = new KeyValuePair<Type, object>(key, value);
                    return;
                }
            }
            MaybeExtra.Add(new KeyValuePair<Type, object>(key, value));
        }

        bool IFeatureCollection.IsReadOnly => false;

        int IFeatureCollection.Revision => _featureRevision;

        string IHttpConnectionFeature.ConnectionId
        {
            get => ConnectionId;
            set => ConnectionId = value;
        }

        IPAddress IHttpConnectionFeature.RemoteIpAddress
        {
            get => RemoteAddress;
            set => RemoteAddress = value;
        }

        IPAddress IHttpConnectionFeature.LocalIpAddress
        {
            get => LocalAddress;
            set => LocalAddress = value;
        }

        int IHttpConnectionFeature.RemotePort
        {
            get => RemotePort;
            set => RemotePort = value;
        }

        int IHttpConnectionFeature.LocalPort
        {
            get => LocalPort;
            set => LocalPort = value;
        }

        MemoryPool IConnectionTransportFeature.MemoryPool => MemoryPool;

        IDuplexPipe IConnectionTransportFeature.Transport
        {
            get => Transport;
            set => Transport = value;
        }

        IDuplexPipe IConnectionTransportFeature.Application
        {
            get => Application;
            set => Application = value;
        }

        object IFeatureCollection.this[Type key]
        {
            get
            {
                if (key == IHttpConnectionFeatureType)
                {
                    return _currentIHttpConnectionFeature;
                }

                if (key == IConnectionIdFeatureType)
                {
                    return _currentIConnectionIdFeature;
                }

                if (key == IConnectionTransportFeatureType)
                {
                    return _currentIConnectionTransportFeature;
                }

                if (MaybeExtra != null)
                {
                    return ExtraFeatureGet(key);
                }

                return null;
            }
            set
            {
                _featureRevision++;

                if (key == IHttpConnectionFeatureType)
                {
                    _currentIHttpConnectionFeature = value;
                }
                else if (key == IConnectionIdFeatureType)
                {
                    _currentIConnectionIdFeature = value;
                }
                else if (key == IConnectionTransportFeatureType)
                {
                    _currentIConnectionTransportFeature = value;
                }
                else
                {
                    ExtraFeatureSet(key, value);
                }
            }
        }

        TFeature IFeatureCollection.Get<TFeature>()
        {
            if (typeof(TFeature) == typeof(IHttpConnectionFeature))
            {
                return (TFeature)_currentIHttpConnectionFeature;
            }
            else if (typeof(TFeature) == typeof(IConnectionIdFeature))
            {
                return (TFeature)_currentIConnectionIdFeature;
            }
            else if (typeof(TFeature) == typeof(IConnectionTransportFeature))
            {
                return (TFeature)_currentIConnectionTransportFeature;
            }
            else if (MaybeExtra != null)
            {
                return (TFeature)ExtraFeatureGet(typeof(TFeature));
            }

            return default;
        }

        void IFeatureCollection.Set<TFeature>(TFeature instance)
        {
            _featureRevision++;

            if (typeof(TFeature) == typeof(IHttpConnectionFeature))
            {
                _currentIHttpConnectionFeature = instance;
            }
            else if (typeof(TFeature) == typeof(IConnectionIdFeature))
            {
                _currentIConnectionIdFeature = instance;
            }
            else if (typeof(TFeature) == typeof(IConnectionTransportFeature))
            {
                _currentIConnectionTransportFeature = instance;
            }
            else
            {
                ExtraFeatureSet(typeof(TFeature), instance);
            }
        }

        IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator() => FastEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => FastEnumerable().GetEnumerator();

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {
            if (_currentIHttpConnectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpConnectionFeatureType, _currentIHttpConnectionFeature);
            }

            if (_currentIConnectionIdFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IConnectionIdFeatureType, _currentIConnectionIdFeature);
            }

            if (_currentIConnectionTransportFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IConnectionTransportFeatureType, _currentIConnectionTransportFeature);
            }

            if (MaybeExtra != null)
            {
                foreach (var item in MaybeExtra)
                {
                    yield return item;
                }
            }
        }
    }
}
