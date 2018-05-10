using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public partial class TransportConnection : IFeatureCollection,
                                               IHttpConnectionFeature,
                                               IConnectionIdFeature,
                                               IConnectionTransportFeature,
                                               IConnectionItemsFeature,
                                               IMemoryPoolFeature,
                                               IApplicationTransportFeature,
                                               ITransportSchedulerFeature,
                                               IConnectionLifetimeFeature,
                                               IBytesWrittenFeature
    {
        private static readonly Type IHttpConnectionFeatureType = typeof(IHttpConnectionFeature);
        private static readonly Type IConnectionIdFeatureType = typeof(IConnectionIdFeature);
        private static readonly Type IConnectionTransportFeatureType = typeof(IConnectionTransportFeature);
        private static readonly Type IConnectionItemsFeatureType = typeof(IConnectionItemsFeature);
        private static readonly Type IMemoryPoolFeatureType = typeof(IMemoryPoolFeature);
        private static readonly Type IApplicationTransportFeatureType = typeof(IApplicationTransportFeature);
        private static readonly Type ITransportSchedulerFeatureType = typeof(ITransportSchedulerFeature);
        private static readonly Type IConnectionLifetimeFeatureType = typeof(IConnectionLifetimeFeature);
        private static readonly Type IBytesWrittenFeatureType = typeof(IBytesWrittenFeature);

        private object _currentIHttpConnectionFeature;
        private object _currentIConnectionIdFeature;
        private object _currentIConnectionTransportFeature;
        private object _currentIConnectionItemsFeature;
        private object _currentIMemoryPoolFeature;
        private object _currentIApplicationTransportFeature;
        private object _currentITransportSchedulerFeature;
        private object _currentIConnectionLifetimeFeature;
        private object _currentIBytesWrittenFeature;

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

        MemoryPool<byte> IMemoryPoolFeature.MemoryPool => MemoryPool;

        IDuplexPipe IConnectionTransportFeature.Transport
        {
            get => Transport;
            set => Transport = value;
        }

        IDuplexPipe IApplicationTransportFeature.Application
        {
            get => Application;
            set => Application = value;
        }

        IDictionary<object, object> IConnectionItemsFeature.Items
        {
            get => Items;
            set => Items = value;
        }

        CancellationToken IConnectionLifetimeFeature.ConnectionClosed
        {
            get => ConnectionClosed;
            set => ConnectionClosed = value;
        }

        void IConnectionLifetimeFeature.Abort() => Abort();

        long IBytesWrittenFeature.TotalBytesWritten => TotalBytesWritten;

        PipeScheduler ITransportSchedulerFeature.InputWriterScheduler => InputWriterScheduler;
        PipeScheduler ITransportSchedulerFeature.OutputReaderScheduler => OutputReaderScheduler;

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

                if (key == IConnectionItemsFeatureType)
                {
                    return _currentIConnectionItemsFeature;
                }

                if (key == IMemoryPoolFeatureType)
                {
                    return _currentIMemoryPoolFeature;
                }

                if (key == IApplicationTransportFeatureType)
                {
                    return _currentIApplicationTransportFeature;
                }

                if (key == ITransportSchedulerFeatureType)
                {
                    return _currentITransportSchedulerFeature;
                }

                if (key == IConnectionLifetimeFeatureType)
                {
                    return _currentIConnectionLifetimeFeature;
                }

                if (key == IBytesWrittenFeatureType)
                {
                    return _currentIBytesWrittenFeature;
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
                else if (key == IConnectionItemsFeatureType)
                {
                    _currentIConnectionItemsFeature = value;
                }
                else if (key == IMemoryPoolFeatureType)
                {
                    _currentIMemoryPoolFeature = value;
                }
                else if (key == IApplicationTransportFeatureType)
                {
                    _currentIApplicationTransportFeature = value;
                }
                else if (key == ITransportSchedulerFeatureType)
                {
                    _currentITransportSchedulerFeature = value;
                }
                else if (key == IConnectionLifetimeFeatureType)
                {
                    _currentIConnectionLifetimeFeature = value;
                }
                else if (key == IBytesWrittenFeatureType)
                {
                    _currentIBytesWrittenFeature = value;
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
            else if (typeof(TFeature) == typeof(IConnectionItemsFeature))
            {
                return (TFeature)_currentIConnectionItemsFeature;
            }
            else if (typeof(TFeature) == typeof(IMemoryPoolFeature))
            {
                return (TFeature)_currentIMemoryPoolFeature;
            }
            else if (typeof(TFeature) == typeof(IApplicationTransportFeature))
            {
                return (TFeature)_currentIApplicationTransportFeature;
            }
            else if (typeof(TFeature) == typeof(ITransportSchedulerFeature))
            {
                return (TFeature)_currentITransportSchedulerFeature;
            }
            else if (typeof(TFeature) == typeof(IConnectionLifetimeFeature))
            {
                return (TFeature)_currentIConnectionLifetimeFeature;
            }
            else if (typeof(TFeature) == typeof(IBytesWrittenFeature))
            {
                return (TFeature)_currentIBytesWrittenFeature;
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
            else if (typeof(TFeature) == typeof(IConnectionItemsFeature))
            {
                _currentIConnectionItemsFeature = instance;
            }
            else if (typeof(TFeature) == typeof(IMemoryPoolFeature))
            {
                _currentIMemoryPoolFeature = instance;
            }
            else if (typeof(TFeature) == typeof(IApplicationTransportFeature))
            {
                _currentIApplicationTransportFeature = instance;
            }
            else if (typeof(TFeature) == typeof(ITransportSchedulerFeature))
            {
                _currentITransportSchedulerFeature = instance;
            }
            else if (typeof(TFeature) == typeof(IConnectionLifetimeFeature))
            {
                _currentIConnectionLifetimeFeature = instance;
            }
            else if (typeof(TFeature) == typeof(IBytesWrittenFeature))
            {
                _currentIBytesWrittenFeature = instance;
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

            if (_currentIConnectionItemsFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IConnectionItemsFeatureType, _currentIConnectionItemsFeature);
            }

            if (_currentIMemoryPoolFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IMemoryPoolFeatureType, _currentIMemoryPoolFeature);
            }

            if (_currentIApplicationTransportFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IApplicationTransportFeatureType, _currentIApplicationTransportFeature);
            }

            if (_currentITransportSchedulerFeature != null)
            {
                yield return new KeyValuePair<Type, object>(ITransportSchedulerFeatureType, _currentITransportSchedulerFeature);
            }

            if (_currentIConnectionLifetimeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IConnectionLifetimeFeatureType, _currentIConnectionLifetimeFeature);
            }

            if (_currentIBytesWrittenFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IBytesWrittenFeatureType, _currentIBytesWrittenFeature);
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
