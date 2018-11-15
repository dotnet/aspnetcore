// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public partial class HttpProtocol : IFeatureCollection
    {
        private static readonly Type IHttpRequestFeatureType = typeof(IHttpRequestFeature);
        private static readonly Type IHttpResponseFeatureType = typeof(IHttpResponseFeature);
        private static readonly Type IHttpRequestIdentifierFeatureType = typeof(IHttpRequestIdentifierFeature);
        private static readonly Type IServiceProvidersFeatureType = typeof(IServiceProvidersFeature);
        private static readonly Type IHttpRequestLifetimeFeatureType = typeof(IHttpRequestLifetimeFeature);
        private static readonly Type IHttpConnectionFeatureType = typeof(IHttpConnectionFeature);
        private static readonly Type IHttpAuthenticationFeatureType = typeof(IHttpAuthenticationFeature);
        private static readonly Type IQueryFeatureType = typeof(IQueryFeature);
        private static readonly Type IFormFeatureType = typeof(IFormFeature);
        private static readonly Type IHttpUpgradeFeatureType = typeof(IHttpUpgradeFeature);
        private static readonly Type IHttp2StreamIdFeatureType = typeof(IHttp2StreamIdFeature);
        private static readonly Type IHttpResponseTrailersFeatureType = typeof(IHttpResponseTrailersFeature);
        private static readonly Type IResponseCookiesFeatureType = typeof(IResponseCookiesFeature);
        private static readonly Type IItemsFeatureType = typeof(IItemsFeature);
        private static readonly Type ITlsConnectionFeatureType = typeof(ITlsConnectionFeature);
        private static readonly Type IHttpWebSocketFeatureType = typeof(IHttpWebSocketFeature);
        private static readonly Type ISessionFeatureType = typeof(ISessionFeature);
        private static readonly Type IHttpMaxRequestBodySizeFeatureType = typeof(IHttpMaxRequestBodySizeFeature);
        private static readonly Type IHttpMinRequestBodyDataRateFeatureType = typeof(IHttpMinRequestBodyDataRateFeature);
        private static readonly Type IHttpMinResponseDataRateFeatureType = typeof(IHttpMinResponseDataRateFeature);
        private static readonly Type IHttpBodyControlFeatureType = typeof(IHttpBodyControlFeature);
        private static readonly Type IHttpSendFileFeatureType = typeof(IHttpSendFileFeature);

        private object _currentIHttpRequestFeature;
        private object _currentIHttpResponseFeature;
        private object _currentIHttpRequestIdentifierFeature;
        private object _currentIServiceProvidersFeature;
        private object _currentIHttpRequestLifetimeFeature;
        private object _currentIHttpConnectionFeature;
        private object _currentIHttpAuthenticationFeature;
        private object _currentIQueryFeature;
        private object _currentIFormFeature;
        private object _currentIHttpUpgradeFeature;
        private object _currentIHttp2StreamIdFeature;
        private object _currentIHttpResponseTrailersFeature;
        private object _currentIResponseCookiesFeature;
        private object _currentIItemsFeature;
        private object _currentITlsConnectionFeature;
        private object _currentIHttpWebSocketFeature;
        private object _currentISessionFeature;
        private object _currentIHttpMaxRequestBodySizeFeature;
        private object _currentIHttpMinRequestBodyDataRateFeature;
        private object _currentIHttpMinResponseDataRateFeature;
        private object _currentIHttpBodyControlFeature;
        private object _currentIHttpSendFileFeature;

        private int _featureRevision;

        private List<KeyValuePair<Type, object>> MaybeExtra;

        private void FastReset()
        {
            _currentIHttpRequestFeature = this;
            _currentIHttpResponseFeature = this;
            _currentIHttpUpgradeFeature = this;
            _currentIHttpRequestIdentifierFeature = this;
            _currentIHttpRequestLifetimeFeature = this;
            _currentIHttpConnectionFeature = this;
            _currentIHttpMaxRequestBodySizeFeature = this;
            _currentIHttpBodyControlFeature = this;

            _currentIServiceProvidersFeature = null;
            _currentIHttpAuthenticationFeature = null;
            _currentIQueryFeature = null;
            _currentIFormFeature = null;
            _currentIHttp2StreamIdFeature = null;
            _currentIHttpResponseTrailersFeature = null;
            _currentIResponseCookiesFeature = null;
            _currentIItemsFeature = null;
            _currentITlsConnectionFeature = null;
            _currentIHttpWebSocketFeature = null;
            _currentISessionFeature = null;
            _currentIHttpMinRequestBodyDataRateFeature = null;
            _currentIHttpMinResponseDataRateFeature = null;
            _currentIHttpSendFileFeature = null;
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
                if (key == IHttpRequestFeatureType)
                {
                    feature = _currentIHttpRequestFeature;
                }
                else if (key == IHttpResponseFeatureType)
                {
                    feature = _currentIHttpResponseFeature;
                }
                else if (key == IHttpRequestIdentifierFeatureType)
                {
                    feature = _currentIHttpRequestIdentifierFeature;
                }
                else if (key == IServiceProvidersFeatureType)
                {
                    feature = _currentIServiceProvidersFeature;
                }
                else if (key == IHttpRequestLifetimeFeatureType)
                {
                    feature = _currentIHttpRequestLifetimeFeature;
                }
                else if (key == IHttpConnectionFeatureType)
                {
                    feature = _currentIHttpConnectionFeature;
                }
                else if (key == IHttpAuthenticationFeatureType)
                {
                    feature = _currentIHttpAuthenticationFeature;
                }
                else if (key == IQueryFeatureType)
                {
                    feature = _currentIQueryFeature;
                }
                else if (key == IFormFeatureType)
                {
                    feature = _currentIFormFeature;
                }
                else if (key == IHttpUpgradeFeatureType)
                {
                    feature = _currentIHttpUpgradeFeature;
                }
                else if (key == IHttp2StreamIdFeatureType)
                {
                    feature = _currentIHttp2StreamIdFeature;
                }
                else if (key == IHttpResponseTrailersFeatureType)
                {
                    feature = _currentIHttpResponseTrailersFeature;
                }
                else if (key == IResponseCookiesFeatureType)
                {
                    feature = _currentIResponseCookiesFeature;
                }
                else if (key == IItemsFeatureType)
                {
                    feature = _currentIItemsFeature;
                }
                else if (key == ITlsConnectionFeatureType)
                {
                    feature = _currentITlsConnectionFeature;
                }
                else if (key == IHttpWebSocketFeatureType)
                {
                    feature = _currentIHttpWebSocketFeature;
                }
                else if (key == ISessionFeatureType)
                {
                    feature = _currentISessionFeature;
                }
                else if (key == IHttpMaxRequestBodySizeFeatureType)
                {
                    feature = _currentIHttpMaxRequestBodySizeFeature;
                }
                else if (key == IHttpMinRequestBodyDataRateFeatureType)
                {
                    feature = _currentIHttpMinRequestBodyDataRateFeature;
                }
                else if (key == IHttpMinResponseDataRateFeatureType)
                {
                    feature = _currentIHttpMinResponseDataRateFeature;
                }
                else if (key == IHttpBodyControlFeatureType)
                {
                    feature = _currentIHttpBodyControlFeature;
                }
                else if (key == IHttpSendFileFeatureType)
                {
                    feature = _currentIHttpSendFileFeature;
                }
                else if (MaybeExtra != null)
                {
                    feature = ExtraFeatureGet(key);
                }

                return feature ?? ConnectionFeatures[key];
            }

            set
            {
                _featureRevision++;

                if (key == IHttpRequestFeatureType)
                {
                    _currentIHttpRequestFeature = value;
                }
                else if (key == IHttpResponseFeatureType)
                {
                    _currentIHttpResponseFeature = value;
                }
                else if (key == IHttpRequestIdentifierFeatureType)
                {
                    _currentIHttpRequestIdentifierFeature = value;
                }
                else if (key == IServiceProvidersFeatureType)
                {
                    _currentIServiceProvidersFeature = value;
                }
                else if (key == IHttpRequestLifetimeFeatureType)
                {
                    _currentIHttpRequestLifetimeFeature = value;
                }
                else if (key == IHttpConnectionFeatureType)
                {
                    _currentIHttpConnectionFeature = value;
                }
                else if (key == IHttpAuthenticationFeatureType)
                {
                    _currentIHttpAuthenticationFeature = value;
                }
                else if (key == IQueryFeatureType)
                {
                    _currentIQueryFeature = value;
                }
                else if (key == IFormFeatureType)
                {
                    _currentIFormFeature = value;
                }
                else if (key == IHttpUpgradeFeatureType)
                {
                    _currentIHttpUpgradeFeature = value;
                }
                else if (key == IHttp2StreamIdFeatureType)
                {
                    _currentIHttp2StreamIdFeature = value;
                }
                else if (key == IHttpResponseTrailersFeatureType)
                {
                    _currentIHttpResponseTrailersFeature = value;
                }
                else if (key == IResponseCookiesFeatureType)
                {
                    _currentIResponseCookiesFeature = value;
                }
                else if (key == IItemsFeatureType)
                {
                    _currentIItemsFeature = value;
                }
                else if (key == ITlsConnectionFeatureType)
                {
                    _currentITlsConnectionFeature = value;
                }
                else if (key == IHttpWebSocketFeatureType)
                {
                    _currentIHttpWebSocketFeature = value;
                }
                else if (key == ISessionFeatureType)
                {
                    _currentISessionFeature = value;
                }
                else if (key == IHttpMaxRequestBodySizeFeatureType)
                {
                    _currentIHttpMaxRequestBodySizeFeature = value;
                }
                else if (key == IHttpMinRequestBodyDataRateFeatureType)
                {
                    _currentIHttpMinRequestBodyDataRateFeature = value;
                }
                else if (key == IHttpMinResponseDataRateFeatureType)
                {
                    _currentIHttpMinResponseDataRateFeature = value;
                }
                else if (key == IHttpBodyControlFeatureType)
                {
                    _currentIHttpBodyControlFeature = value;
                }
                else if (key == IHttpSendFileFeatureType)
                {
                    _currentIHttpSendFileFeature = value;
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
            if (typeof(TFeature) == typeof(IHttpRequestFeature))
            {
                feature = (TFeature)_currentIHttpRequestFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpResponseFeature))
            {
                feature = (TFeature)_currentIHttpResponseFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestIdentifierFeature))
            {
                feature = (TFeature)_currentIHttpRequestIdentifierFeature;
            }
            else if (typeof(TFeature) == typeof(IServiceProvidersFeature))
            {
                feature = (TFeature)_currentIServiceProvidersFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestLifetimeFeature))
            {
                feature = (TFeature)_currentIHttpRequestLifetimeFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpConnectionFeature))
            {
                feature = (TFeature)_currentIHttpConnectionFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpAuthenticationFeature))
            {
                feature = (TFeature)_currentIHttpAuthenticationFeature;
            }
            else if (typeof(TFeature) == typeof(IQueryFeature))
            {
                feature = (TFeature)_currentIQueryFeature;
            }
            else if (typeof(TFeature) == typeof(IFormFeature))
            {
                feature = (TFeature)_currentIFormFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpUpgradeFeature))
            {
                feature = (TFeature)_currentIHttpUpgradeFeature;
            }
            else if (typeof(TFeature) == typeof(IHttp2StreamIdFeature))
            {
                feature = (TFeature)_currentIHttp2StreamIdFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpResponseTrailersFeature))
            {
                feature = (TFeature)_currentIHttpResponseTrailersFeature;
            }
            else if (typeof(TFeature) == typeof(IResponseCookiesFeature))
            {
                feature = (TFeature)_currentIResponseCookiesFeature;
            }
            else if (typeof(TFeature) == typeof(IItemsFeature))
            {
                feature = (TFeature)_currentIItemsFeature;
            }
            else if (typeof(TFeature) == typeof(ITlsConnectionFeature))
            {
                feature = (TFeature)_currentITlsConnectionFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpWebSocketFeature))
            {
                feature = (TFeature)_currentIHttpWebSocketFeature;
            }
            else if (typeof(TFeature) == typeof(ISessionFeature))
            {
                feature = (TFeature)_currentISessionFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpMaxRequestBodySizeFeature))
            {
                feature = (TFeature)_currentIHttpMaxRequestBodySizeFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpMinRequestBodyDataRateFeature))
            {
                feature = (TFeature)_currentIHttpMinRequestBodyDataRateFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpMinResponseDataRateFeature))
            {
                feature = (TFeature)_currentIHttpMinResponseDataRateFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpBodyControlFeature))
            {
                feature = (TFeature)_currentIHttpBodyControlFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpSendFileFeature))
            {
                feature = (TFeature)_currentIHttpSendFileFeature;
            }
            else if (MaybeExtra != null)
            {
                feature = (TFeature)(ExtraFeatureGet(typeof(TFeature)));
            }

            if (feature == null)
            {
                feature = ConnectionFeatures.Get<TFeature>();
            }

            return feature;
        }

        void IFeatureCollection.Set<TFeature>(TFeature feature)
        {
            _featureRevision++;
            if (typeof(TFeature) == typeof(IHttpRequestFeature))
            {
                _currentIHttpRequestFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpResponseFeature))
            {
                _currentIHttpResponseFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestIdentifierFeature))
            {
                _currentIHttpRequestIdentifierFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IServiceProvidersFeature))
            {
                _currentIServiceProvidersFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestLifetimeFeature))
            {
                _currentIHttpRequestLifetimeFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpConnectionFeature))
            {
                _currentIHttpConnectionFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpAuthenticationFeature))
            {
                _currentIHttpAuthenticationFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IQueryFeature))
            {
                _currentIQueryFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IFormFeature))
            {
                _currentIFormFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpUpgradeFeature))
            {
                _currentIHttpUpgradeFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttp2StreamIdFeature))
            {
                _currentIHttp2StreamIdFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpResponseTrailersFeature))
            {
                _currentIHttpResponseTrailersFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IResponseCookiesFeature))
            {
                _currentIResponseCookiesFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IItemsFeature))
            {
                _currentIItemsFeature = feature;
            }
            else if (typeof(TFeature) == typeof(ITlsConnectionFeature))
            {
                _currentITlsConnectionFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpWebSocketFeature))
            {
                _currentIHttpWebSocketFeature = feature;
            }
            else if (typeof(TFeature) == typeof(ISessionFeature))
            {
                _currentISessionFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpMaxRequestBodySizeFeature))
            {
                _currentIHttpMaxRequestBodySizeFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpMinRequestBodyDataRateFeature))
            {
                _currentIHttpMinRequestBodyDataRateFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpMinResponseDataRateFeature))
            {
                _currentIHttpMinResponseDataRateFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpBodyControlFeature))
            {
                _currentIHttpBodyControlFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpSendFileFeature))
            {
                _currentIHttpSendFileFeature = feature;
            }
            else
            {
                ExtraFeatureSet(typeof(TFeature), feature);
            }
        }

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {
            if (_currentIHttpRequestFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpRequestFeatureType, _currentIHttpRequestFeature);
            }
            if (_currentIHttpResponseFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpResponseFeatureType, _currentIHttpResponseFeature);
            }
            if (_currentIHttpRequestIdentifierFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpRequestIdentifierFeatureType, _currentIHttpRequestIdentifierFeature);
            }
            if (_currentIServiceProvidersFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IServiceProvidersFeatureType, _currentIServiceProvidersFeature);
            }
            if (_currentIHttpRequestLifetimeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpRequestLifetimeFeatureType, _currentIHttpRequestLifetimeFeature);
            }
            if (_currentIHttpConnectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpConnectionFeatureType, _currentIHttpConnectionFeature);
            }
            if (_currentIHttpAuthenticationFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpAuthenticationFeatureType, _currentIHttpAuthenticationFeature);
            }
            if (_currentIQueryFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IQueryFeatureType, _currentIQueryFeature);
            }
            if (_currentIFormFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IFormFeatureType, _currentIFormFeature);
            }
            if (_currentIHttpUpgradeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpUpgradeFeatureType, _currentIHttpUpgradeFeature);
            }
            if (_currentIHttp2StreamIdFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttp2StreamIdFeatureType, _currentIHttp2StreamIdFeature);
            }
            if (_currentIHttpResponseTrailersFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpResponseTrailersFeatureType, _currentIHttpResponseTrailersFeature);
            }
            if (_currentIResponseCookiesFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IResponseCookiesFeatureType, _currentIResponseCookiesFeature);
            }
            if (_currentIItemsFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IItemsFeatureType, _currentIItemsFeature);
            }
            if (_currentITlsConnectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(ITlsConnectionFeatureType, _currentITlsConnectionFeature);
            }
            if (_currentIHttpWebSocketFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpWebSocketFeatureType, _currentIHttpWebSocketFeature);
            }
            if (_currentISessionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(ISessionFeatureType, _currentISessionFeature);
            }
            if (_currentIHttpMaxRequestBodySizeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpMaxRequestBodySizeFeatureType, _currentIHttpMaxRequestBodySizeFeature);
            }
            if (_currentIHttpMinRequestBodyDataRateFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpMinRequestBodyDataRateFeatureType, _currentIHttpMinRequestBodyDataRateFeature);
            }
            if (_currentIHttpMinResponseDataRateFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpMinResponseDataRateFeatureType, _currentIHttpMinResponseDataRateFeature);
            }
            if (_currentIHttpBodyControlFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpBodyControlFeatureType, _currentIHttpBodyControlFeature);
            }
            if (_currentIHttpSendFileFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpSendFileFeatureType, _currentIHttpSendFileFeature);
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
