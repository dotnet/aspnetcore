// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Connections
{
    internal partial class TransportMultiplexedConnection : IFeatureCollection
    {
        private object _currentIConnectionIdFeature;
        private object _currentIConnectionTransportFeature;
        private object _currentIConnectionItemsFeature;
        private object _currentIMemoryPoolFeature;
        private object _currentIConnectionLifetimeFeature;

        private int _featureRevision;

        private List<KeyValuePair<Type, object>> MaybeExtra;

        private void FastReset()
        {
            _currentIConnectionIdFeature = this;
            _currentIConnectionTransportFeature = this;
            _currentIConnectionItemsFeature = this;
            _currentIMemoryPoolFeature = this;
            _currentIConnectionLifetimeFeature = this;

        }

        // Internal for testing
        internal void ResetFeatureCollection()
        {
            FastReset();
            MaybeExtra?.Clear();
            _featureRevision++;
        }

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

        object IFeatureCollection.this[Type key]
        {
            get
            {
                object feature = null;
                if (key == typeof(IConnectionIdFeature))
                {
                    feature = _currentIConnectionIdFeature;
                }
                else if (key == typeof(IConnectionTransportFeature))
                {
                    feature = _currentIConnectionTransportFeature;
                }
                else if (key == typeof(IConnectionItemsFeature))
                {
                    feature = _currentIConnectionItemsFeature;
                }
                else if (key == typeof(IMemoryPoolFeature))
                {
                    feature = _currentIMemoryPoolFeature;
                }
                else if (key == typeof(IConnectionLifetimeFeature))
                {
                    feature = _currentIConnectionLifetimeFeature;
                }
                else if (MaybeExtra != null)
                {
                    feature = ExtraFeatureGet(key);
                }

                return feature;
            }

            set
            {
                _featureRevision++;

                if (key == typeof(IConnectionIdFeature))
                {
                    _currentIConnectionIdFeature = value;
                }
                else if (key == typeof(IConnectionTransportFeature))
                {
                    _currentIConnectionTransportFeature = value;
                }
                else if (key == typeof(IConnectionItemsFeature))
                {
                    _currentIConnectionItemsFeature = value;
                }
                else if (key == typeof(IMemoryPoolFeature))
                {
                    _currentIMemoryPoolFeature = value;
                }
                else if (key == typeof(IConnectionLifetimeFeature))
                {
                    _currentIConnectionLifetimeFeature = value;
                }
                else
                {
                    ExtraFeatureSet(key, value);
                }
            }
        }

        TFeature IFeatureCollection.Get<TFeature>()
        {
            TFeature feature = default;
            if (typeof(TFeature) == typeof(IConnectionIdFeature))
            {
                feature = (TFeature)_currentIConnectionIdFeature;
            }
            else if (typeof(TFeature) == typeof(IConnectionTransportFeature))
            {
                feature = (TFeature)_currentIConnectionTransportFeature;
            }
            else if (typeof(TFeature) == typeof(IConnectionItemsFeature))
            {
                feature = (TFeature)_currentIConnectionItemsFeature;
            }
            else if (typeof(TFeature) == typeof(IMemoryPoolFeature))
            {
                feature = (TFeature)_currentIMemoryPoolFeature;
            }
            else if (typeof(TFeature) == typeof(IConnectionLifetimeFeature))
            {
                feature = (TFeature)_currentIConnectionLifetimeFeature;
            }
            else if (MaybeExtra != null)
            {
                feature = (TFeature)(ExtraFeatureGet(typeof(TFeature)));
            }

            return feature;
        }

        void IFeatureCollection.Set<TFeature>(TFeature feature)
        {
            _featureRevision++;
            if (typeof(TFeature) == typeof(IConnectionIdFeature))
            {
                _currentIConnectionIdFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IConnectionTransportFeature))
            {
                _currentIConnectionTransportFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IConnectionItemsFeature))
            {
                _currentIConnectionItemsFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IMemoryPoolFeature))
            {
                _currentIMemoryPoolFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IConnectionLifetimeFeature))
            {
                _currentIConnectionLifetimeFeature = feature;
            }
            else
            {
                ExtraFeatureSet(typeof(TFeature), feature);
            }
        }

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {
            if (_currentIConnectionIdFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IConnectionIdFeature), _currentIConnectionIdFeature);
            }
            if (_currentIConnectionTransportFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IConnectionTransportFeature), _currentIConnectionTransportFeature);
            }
            if (_currentIConnectionItemsFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IConnectionItemsFeature), _currentIConnectionItemsFeature);
            }
            if (_currentIMemoryPoolFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IMemoryPoolFeature), _currentIMemoryPoolFeature);
            }
            if (_currentIConnectionLifetimeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IConnectionLifetimeFeature), _currentIConnectionLifetimeFeature);
            }

            if (MaybeExtra != null)
            {
                foreach (var item in MaybeExtra)
                {
                    yield return item;
                }
            }
        }

        IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator() => FastEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => FastEnumerable().GetEnumerator();
    }
}
