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
    internal partial class HttpProtocol : IFeatureCollection
    {
        private object _currentIHttpRequestFeature;
        private object _currentIHttpResponseFeature;
        private object _currentIHttpResponseBodyFeature;
        private object _currentIRequestBodyPipeFeature;
        private object _currentIHttpRequestIdentifierFeature;
        private object _currentIServiceProvidersFeature;
        private object _currentIHttpRequestLifetimeFeature;
        private object _currentIHttpConnectionFeature;
        private object _currentIRouteValuesFeature;
        private object _currentIEndpointFeature;
        private object _currentIHttpAuthenticationFeature;
        private object _currentIHttpRequestTrailersFeature;
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
        private object _currentIHttpResetFeature;

        private int _featureRevision;

        private List<KeyValuePair<Type, object>> MaybeExtra;

        private void FastReset()
        {
            _currentIHttpRequestFeature = this;
            _currentIHttpResponseFeature = this;
            _currentIHttpResponseBodyFeature = this;
            _currentIRequestBodyPipeFeature = this;
            _currentIHttpUpgradeFeature = this;
            _currentIHttpRequestIdentifierFeature = this;
            _currentIHttpRequestLifetimeFeature = this;
            _currentIHttpRequestTrailersFeature = this;
            _currentIHttpConnectionFeature = this;
            _currentIHttpMaxRequestBodySizeFeature = this;
            _currentIHttpMinRequestBodyDataRateFeature = this;
            _currentIHttpBodyControlFeature = this;
            _currentIRouteValuesFeature = this;
            _currentIEndpointFeature = this;

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
            _currentIHttpMinResponseDataRateFeature = null;
            _currentIHttpResetFeature = null;
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
                if (key == typeof(IHttpRequestFeature))
                {
                    feature = _currentIHttpRequestFeature;
                }
                else if (key == typeof(IHttpResponseFeature))
                {
                    feature = _currentIHttpResponseFeature;
                }
                else if (key == typeof(IHttpResponseBodyFeature))
                {
                    feature = _currentIHttpResponseBodyFeature;
                }
                else if (key == typeof(IRequestBodyPipeFeature))
                {
                    feature = _currentIRequestBodyPipeFeature;
                }
                else if (key == typeof(IHttpRequestIdentifierFeature))
                {
                    feature = _currentIHttpRequestIdentifierFeature;
                }
                else if (key == typeof(IServiceProvidersFeature))
                {
                    feature = _currentIServiceProvidersFeature;
                }
                else if (key == typeof(IHttpRequestLifetimeFeature))
                {
                    feature = _currentIHttpRequestLifetimeFeature;
                }
                else if (key == typeof(IHttpConnectionFeature))
                {
                    feature = _currentIHttpConnectionFeature;
                }
                else if (key == typeof(IRouteValuesFeature))
                {
                    feature = _currentIRouteValuesFeature;
                }
                else if (key == typeof(IEndpointFeature))
                {
                    feature = _currentIEndpointFeature;
                }
                else if (key == typeof(IHttpAuthenticationFeature))
                {
                    feature = _currentIHttpAuthenticationFeature;
                }
                else if (key == typeof(IHttpRequestTrailersFeature))
                {
                    feature = _currentIHttpRequestTrailersFeature;
                }
                else if (key == typeof(IQueryFeature))
                {
                    feature = _currentIQueryFeature;
                }
                else if (key == typeof(IFormFeature))
                {
                    feature = _currentIFormFeature;
                }
                else if (key == typeof(IHttpUpgradeFeature))
                {
                    feature = _currentIHttpUpgradeFeature;
                }
                else if (key == typeof(IHttp2StreamIdFeature))
                {
                    feature = _currentIHttp2StreamIdFeature;
                }
                else if (key == typeof(IHttpResponseTrailersFeature))
                {
                    feature = _currentIHttpResponseTrailersFeature;
                }
                else if (key == typeof(IResponseCookiesFeature))
                {
                    feature = _currentIResponseCookiesFeature;
                }
                else if (key == typeof(IItemsFeature))
                {
                    feature = _currentIItemsFeature;
                }
                else if (key == typeof(ITlsConnectionFeature))
                {
                    feature = _currentITlsConnectionFeature;
                }
                else if (key == typeof(IHttpWebSocketFeature))
                {
                    feature = _currentIHttpWebSocketFeature;
                }
                else if (key == typeof(ISessionFeature))
                {
                    feature = _currentISessionFeature;
                }
                else if (key == typeof(IHttpMaxRequestBodySizeFeature))
                {
                    feature = _currentIHttpMaxRequestBodySizeFeature;
                }
                else if (key == typeof(IHttpMinRequestBodyDataRateFeature))
                {
                    feature = _currentIHttpMinRequestBodyDataRateFeature;
                }
                else if (key == typeof(IHttpMinResponseDataRateFeature))
                {
                    feature = _currentIHttpMinResponseDataRateFeature;
                }
                else if (key == typeof(IHttpBodyControlFeature))
                {
                    feature = _currentIHttpBodyControlFeature;
                }
                else if (key == typeof(IHttpResetFeature))
                {
                    feature = _currentIHttpResetFeature;
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

                if (key == typeof(IHttpRequestFeature))
                {
                    _currentIHttpRequestFeature = value;
                }
                else if (key == typeof(IHttpResponseFeature))
                {
                    _currentIHttpResponseFeature = value;
                }
                else if (key == typeof(IHttpResponseBodyFeature))
                {
                    _currentIHttpResponseBodyFeature = value;
                }
                else if (key == typeof(IRequestBodyPipeFeature))
                {
                    _currentIRequestBodyPipeFeature = value;
                }
                else if (key == typeof(IHttpRequestIdentifierFeature))
                {
                    _currentIHttpRequestIdentifierFeature = value;
                }
                else if (key == typeof(IServiceProvidersFeature))
                {
                    _currentIServiceProvidersFeature = value;
                }
                else if (key == typeof(IHttpRequestLifetimeFeature))
                {
                    _currentIHttpRequestLifetimeFeature = value;
                }
                else if (key == typeof(IHttpConnectionFeature))
                {
                    _currentIHttpConnectionFeature = value;
                }
                else if (key == typeof(IRouteValuesFeature))
                {
                    _currentIRouteValuesFeature = value;
                }
                else if (key == typeof(IEndpointFeature))
                {
                    _currentIEndpointFeature = value;
                }
                else if (key == typeof(IHttpAuthenticationFeature))
                {
                    _currentIHttpAuthenticationFeature = value;
                }
                else if (key == typeof(IHttpRequestTrailersFeature))
                {
                    _currentIHttpRequestTrailersFeature = value;
                }
                else if (key == typeof(IQueryFeature))
                {
                    _currentIQueryFeature = value;
                }
                else if (key == typeof(IFormFeature))
                {
                    _currentIFormFeature = value;
                }
                else if (key == typeof(IHttpUpgradeFeature))
                {
                    _currentIHttpUpgradeFeature = value;
                }
                else if (key == typeof(IHttp2StreamIdFeature))
                {
                    _currentIHttp2StreamIdFeature = value;
                }
                else if (key == typeof(IHttpResponseTrailersFeature))
                {
                    _currentIHttpResponseTrailersFeature = value;
                }
                else if (key == typeof(IResponseCookiesFeature))
                {
                    _currentIResponseCookiesFeature = value;
                }
                else if (key == typeof(IItemsFeature))
                {
                    _currentIItemsFeature = value;
                }
                else if (key == typeof(ITlsConnectionFeature))
                {
                    _currentITlsConnectionFeature = value;
                }
                else if (key == typeof(IHttpWebSocketFeature))
                {
                    _currentIHttpWebSocketFeature = value;
                }
                else if (key == typeof(ISessionFeature))
                {
                    _currentISessionFeature = value;
                }
                else if (key == typeof(IHttpMaxRequestBodySizeFeature))
                {
                    _currentIHttpMaxRequestBodySizeFeature = value;
                }
                else if (key == typeof(IHttpMinRequestBodyDataRateFeature))
                {
                    _currentIHttpMinRequestBodyDataRateFeature = value;
                }
                else if (key == typeof(IHttpMinResponseDataRateFeature))
                {
                    _currentIHttpMinResponseDataRateFeature = value;
                }
                else if (key == typeof(IHttpBodyControlFeature))
                {
                    _currentIHttpBodyControlFeature = value;
                }
                else if (key == typeof(IHttpResetFeature))
                {
                    _currentIHttpResetFeature = value;
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
            else if (typeof(TFeature) == typeof(IHttpResponseBodyFeature))
            {
                feature = (TFeature)_currentIHttpResponseBodyFeature;
            }
            else if (typeof(TFeature) == typeof(IRequestBodyPipeFeature))
            {
                feature = (TFeature)_currentIRequestBodyPipeFeature;
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
            else if (typeof(TFeature) == typeof(IRouteValuesFeature))
            {
                feature = (TFeature)_currentIRouteValuesFeature;
            }
            else if (typeof(TFeature) == typeof(IEndpointFeature))
            {
                feature = (TFeature)_currentIEndpointFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpAuthenticationFeature))
            {
                feature = (TFeature)_currentIHttpAuthenticationFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestTrailersFeature))
            {
                feature = (TFeature)_currentIHttpRequestTrailersFeature;
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
            else if (typeof(TFeature) == typeof(IHttpResetFeature))
            {
                feature = (TFeature)_currentIHttpResetFeature;
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
            else if (typeof(TFeature) == typeof(IHttpResponseBodyFeature))
            {
                _currentIHttpResponseBodyFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IRequestBodyPipeFeature))
            {
                _currentIRequestBodyPipeFeature = feature;
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
            else if (typeof(TFeature) == typeof(IRouteValuesFeature))
            {
                _currentIRouteValuesFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IEndpointFeature))
            {
                _currentIEndpointFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpAuthenticationFeature))
            {
                _currentIHttpAuthenticationFeature = feature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestTrailersFeature))
            {
                _currentIHttpRequestTrailersFeature = feature;
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
            else if (typeof(TFeature) == typeof(IHttpResetFeature))
            {
                _currentIHttpResetFeature = feature;
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
                yield return new KeyValuePair<Type, object>(typeof(IHttpRequestFeature), _currentIHttpRequestFeature);
            }
            if (_currentIHttpResponseFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpResponseFeature), _currentIHttpResponseFeature);
            }
            if (_currentIHttpResponseBodyFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpResponseBodyFeature), _currentIHttpResponseBodyFeature);
            }
            if (_currentIRequestBodyPipeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IRequestBodyPipeFeature), _currentIRequestBodyPipeFeature);
            }
            if (_currentIHttpRequestIdentifierFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpRequestIdentifierFeature), _currentIHttpRequestIdentifierFeature);
            }
            if (_currentIServiceProvidersFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IServiceProvidersFeature), _currentIServiceProvidersFeature);
            }
            if (_currentIHttpRequestLifetimeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpRequestLifetimeFeature), _currentIHttpRequestLifetimeFeature);
            }
            if (_currentIHttpConnectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpConnectionFeature), _currentIHttpConnectionFeature);
            }
            if (_currentIRouteValuesFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IRouteValuesFeature), _currentIRouteValuesFeature);
            }
            if (_currentIEndpointFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IEndpointFeature), _currentIEndpointFeature);
            }
            if (_currentIHttpAuthenticationFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpAuthenticationFeature), _currentIHttpAuthenticationFeature);
            }
            if (_currentIHttpRequestTrailersFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpRequestTrailersFeature), _currentIHttpRequestTrailersFeature);
            }
            if (_currentIQueryFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IQueryFeature), _currentIQueryFeature);
            }
            if (_currentIFormFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IFormFeature), _currentIFormFeature);
            }
            if (_currentIHttpUpgradeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpUpgradeFeature), _currentIHttpUpgradeFeature);
            }
            if (_currentIHttp2StreamIdFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttp2StreamIdFeature), _currentIHttp2StreamIdFeature);
            }
            if (_currentIHttpResponseTrailersFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpResponseTrailersFeature), _currentIHttpResponseTrailersFeature);
            }
            if (_currentIResponseCookiesFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IResponseCookiesFeature), _currentIResponseCookiesFeature);
            }
            if (_currentIItemsFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IItemsFeature), _currentIItemsFeature);
            }
            if (_currentITlsConnectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(ITlsConnectionFeature), _currentITlsConnectionFeature);
            }
            if (_currentIHttpWebSocketFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpWebSocketFeature), _currentIHttpWebSocketFeature);
            }
            if (_currentISessionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(ISessionFeature), _currentISessionFeature);
            }
            if (_currentIHttpMaxRequestBodySizeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpMaxRequestBodySizeFeature), _currentIHttpMaxRequestBodySizeFeature);
            }
            if (_currentIHttpMinRequestBodyDataRateFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpMinRequestBodyDataRateFeature), _currentIHttpMinRequestBodyDataRateFeature);
            }
            if (_currentIHttpMinResponseDataRateFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpMinResponseDataRateFeature), _currentIHttpMinResponseDataRateFeature);
            }
            if (_currentIHttpBodyControlFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpBodyControlFeature), _currentIHttpBodyControlFeature);
            }
            if (_currentIHttpResetFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpResetFeature), _currentIHttpResetFeature);
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
