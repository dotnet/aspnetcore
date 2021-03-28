// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

#nullable enable

namespace Microsoft.AspNetCore.Connections
{
    internal partial class TransportConnection : IFeatureCollection
    {
        internal protected IConnectionIdFeature? _currentIConnectionIdFeature;
        internal protected IConnectionTransportFeature? _currentIConnectionTransportFeature;
        internal protected IConnectionItemsFeature? _currentIConnectionItemsFeature;
        internal protected IMemoryPoolFeature? _currentIMemoryPoolFeature;
        internal protected IConnectionLifetimeFeature? _currentIConnectionLifetimeFeature;

        private int _featureRevision;

        private List<KeyValuePair<Type, object>>? MaybeExtra;

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

        private object? ExtraFeatureGet(Type key)
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

        private void ExtraFeatureSet(Type key, object? value)
        {
            if (value == null)
            {
                if (MaybeExtra == null)
                {
                    return;
                }
                for (var i = 0; i < MaybeExtra.Count; i++)
                {
                    if (MaybeExtra[i].Key == key)
                    {
                        MaybeExtra.RemoveAt(i);
                        return;
                    }
                }
            }
            else
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
        }

        bool IFeatureCollection.IsReadOnly => false;

        int IFeatureCollection.Revision => _featureRevision;

        object? IFeatureCollection.this[Type key]
        {
            get
            {
                object? feature = null;
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
                    _currentIConnectionIdFeature = (IConnectionIdFeature?)value;
                }
                else if (key == typeof(IConnectionTransportFeature))
                {
                    _currentIConnectionTransportFeature = (IConnectionTransportFeature?)value;
                }
                else if (key == typeof(IConnectionItemsFeature))
                {
                    _currentIConnectionItemsFeature = (IConnectionItemsFeature?)value;
                }
                else if (key == typeof(IMemoryPoolFeature))
                {
                    _currentIMemoryPoolFeature = (IMemoryPoolFeature?)value;
                }
                else if (key == typeof(IConnectionLifetimeFeature))
                {
                    _currentIConnectionLifetimeFeature = (IConnectionLifetimeFeature?)value;
                }
                else
                {
                    ExtraFeatureSet(key, value);
                }
            }
        }

        TFeature? IFeatureCollection.Get<TFeature>() where TFeature : default
        {
            // Using Unsafe.As for the cast due to https://github.com/dotnet/runtime/issues/49614
            // The type of TFeature is confirmed by the typeof() check and the As cast only accepts
            // that type; however the Jit does not eliminate a regular cast in a shared generic.

            TFeature? feature = default;
            if (typeof(TFeature) == typeof(IConnectionIdFeature))
            {
                feature = Unsafe.As<IConnectionIdFeature?, TFeature?>(ref _currentIConnectionIdFeature);
            }
            else if (typeof(TFeature) == typeof(IConnectionTransportFeature))
            {
                feature = Unsafe.As<IConnectionTransportFeature?, TFeature?>(ref _currentIConnectionTransportFeature);
            }
            else if (typeof(TFeature) == typeof(IConnectionItemsFeature))
            {
                feature = Unsafe.As<IConnectionItemsFeature?, TFeature?>(ref _currentIConnectionItemsFeature);
            }
            else if (typeof(TFeature) == typeof(IMemoryPoolFeature))
            {
                feature = Unsafe.As<IMemoryPoolFeature?, TFeature?>(ref _currentIMemoryPoolFeature);
            }
            else if (typeof(TFeature) == typeof(IConnectionLifetimeFeature))
            {
                feature = Unsafe.As<IConnectionLifetimeFeature?, TFeature?>(ref _currentIConnectionLifetimeFeature);
            }
            else if (MaybeExtra != null)
            {
                feature = (TFeature?)(ExtraFeatureGet(typeof(TFeature)));
            }

            return feature;
        }

        void IFeatureCollection.Set<TFeature>(TFeature? feature) where TFeature : default
        {
            _featureRevision++;
            if (typeof(TFeature) == typeof(IConnectionIdFeature))
            {
                _currentIConnectionIdFeature = (IConnectionIdFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IConnectionTransportFeature))
            {
                _currentIConnectionTransportFeature = (IConnectionTransportFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IConnectionItemsFeature))
            {
                _currentIConnectionItemsFeature = (IConnectionItemsFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IMemoryPoolFeature))
            {
                _currentIMemoryPoolFeature = (IMemoryPoolFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IConnectionLifetimeFeature))
            {
                _currentIConnectionLifetimeFeature = (IConnectionLifetimeFeature?)feature;
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
