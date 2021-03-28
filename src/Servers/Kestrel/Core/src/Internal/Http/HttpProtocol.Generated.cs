// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

#nullable enable

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal partial class HttpProtocol : IFeatureCollection
    {
        internal protected IHttpRequestFeature? _currentIHttpRequestFeature;
        internal protected IHttpRequestBodyDetectionFeature? _currentIHttpRequestBodyDetectionFeature;
        internal protected IHttpResponseFeature? _currentIHttpResponseFeature;
        internal protected IHttpResponseBodyFeature? _currentIHttpResponseBodyFeature;
        internal protected IRequestBodyPipeFeature? _currentIRequestBodyPipeFeature;
        internal protected IHttpRequestIdentifierFeature? _currentIHttpRequestIdentifierFeature;
        internal protected IServiceProvidersFeature? _currentIServiceProvidersFeature;
        internal protected IHttpRequestLifetimeFeature? _currentIHttpRequestLifetimeFeature;
        internal protected IHttpConnectionFeature? _currentIHttpConnectionFeature;
        internal protected IRouteValuesFeature? _currentIRouteValuesFeature;
        internal protected IEndpointFeature? _currentIEndpointFeature;
        internal protected IHttpAuthenticationFeature? _currentIHttpAuthenticationFeature;
        internal protected IHttpRequestTrailersFeature? _currentIHttpRequestTrailersFeature;
        internal protected IQueryFeature? _currentIQueryFeature;
        internal protected IFormFeature? _currentIFormFeature;
        internal protected IHttpUpgradeFeature? _currentIHttpUpgradeFeature;
        internal protected IHttp2StreamIdFeature? _currentIHttp2StreamIdFeature;
        internal protected IHttpResponseTrailersFeature? _currentIHttpResponseTrailersFeature;
        internal protected IResponseCookiesFeature? _currentIResponseCookiesFeature;
        internal protected IItemsFeature? _currentIItemsFeature;
        internal protected ITlsConnectionFeature? _currentITlsConnectionFeature;
        internal protected IHttpWebSocketFeature? _currentIHttpWebSocketFeature;
        internal protected ISessionFeature? _currentISessionFeature;
        internal protected IHttpMaxRequestBodySizeFeature? _currentIHttpMaxRequestBodySizeFeature;
        internal protected IHttpMinRequestBodyDataRateFeature? _currentIHttpMinRequestBodyDataRateFeature;
        internal protected IHttpMinResponseDataRateFeature? _currentIHttpMinResponseDataRateFeature;
        internal protected IHttpBodyControlFeature? _currentIHttpBodyControlFeature;
        internal protected IHttpResetFeature? _currentIHttpResetFeature;

        private int _featureRevision;

        private List<KeyValuePair<Type, object>>? MaybeExtra;

        private void FastReset()
        {
            _currentIHttpRequestFeature = this;
            _currentIHttpRequestBodyDetectionFeature = this;
            _currentIHttpResponseFeature = this;
            _currentIHttpResponseBodyFeature = this;
            _currentIRequestBodyPipeFeature = this;
            _currentIHttpUpgradeFeature = this;
            _currentIHttpRequestIdentifierFeature = this;
            _currentIHttpRequestLifetimeFeature = this;
            _currentIHttpRequestTrailersFeature = this;
            _currentIHttpConnectionFeature = this;
            _currentIHttpMaxRequestBodySizeFeature = this;
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
                if (key == typeof(IHttpRequestFeature))
                {
                    feature = _currentIHttpRequestFeature;
                }
                else if (key == typeof(IHttpRequestBodyDetectionFeature))
                {
                    feature = _currentIHttpRequestBodyDetectionFeature;
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
                    _currentIHttpRequestFeature = (IHttpRequestFeature?)value;
                }
                else if (key == typeof(IHttpRequestBodyDetectionFeature))
                {
                    _currentIHttpRequestBodyDetectionFeature = (IHttpRequestBodyDetectionFeature?)value;
                }
                else if (key == typeof(IHttpResponseFeature))
                {
                    _currentIHttpResponseFeature = (IHttpResponseFeature?)value;
                }
                else if (key == typeof(IHttpResponseBodyFeature))
                {
                    _currentIHttpResponseBodyFeature = (IHttpResponseBodyFeature?)value;
                }
                else if (key == typeof(IRequestBodyPipeFeature))
                {
                    _currentIRequestBodyPipeFeature = (IRequestBodyPipeFeature?)value;
                }
                else if (key == typeof(IHttpRequestIdentifierFeature))
                {
                    _currentIHttpRequestIdentifierFeature = (IHttpRequestIdentifierFeature?)value;
                }
                else if (key == typeof(IServiceProvidersFeature))
                {
                    _currentIServiceProvidersFeature = (IServiceProvidersFeature?)value;
                }
                else if (key == typeof(IHttpRequestLifetimeFeature))
                {
                    _currentIHttpRequestLifetimeFeature = (IHttpRequestLifetimeFeature?)value;
                }
                else if (key == typeof(IHttpConnectionFeature))
                {
                    _currentIHttpConnectionFeature = (IHttpConnectionFeature?)value;
                }
                else if (key == typeof(IRouteValuesFeature))
                {
                    _currentIRouteValuesFeature = (IRouteValuesFeature?)value;
                }
                else if (key == typeof(IEndpointFeature))
                {
                    _currentIEndpointFeature = (IEndpointFeature?)value;
                }
                else if (key == typeof(IHttpAuthenticationFeature))
                {
                    _currentIHttpAuthenticationFeature = (IHttpAuthenticationFeature?)value;
                }
                else if (key == typeof(IHttpRequestTrailersFeature))
                {
                    _currentIHttpRequestTrailersFeature = (IHttpRequestTrailersFeature?)value;
                }
                else if (key == typeof(IQueryFeature))
                {
                    _currentIQueryFeature = (IQueryFeature?)value;
                }
                else if (key == typeof(IFormFeature))
                {
                    _currentIFormFeature = (IFormFeature?)value;
                }
                else if (key == typeof(IHttpUpgradeFeature))
                {
                    _currentIHttpUpgradeFeature = (IHttpUpgradeFeature?)value;
                }
                else if (key == typeof(IHttp2StreamIdFeature))
                {
                    _currentIHttp2StreamIdFeature = (IHttp2StreamIdFeature?)value;
                }
                else if (key == typeof(IHttpResponseTrailersFeature))
                {
                    _currentIHttpResponseTrailersFeature = (IHttpResponseTrailersFeature?)value;
                }
                else if (key == typeof(IResponseCookiesFeature))
                {
                    _currentIResponseCookiesFeature = (IResponseCookiesFeature?)value;
                }
                else if (key == typeof(IItemsFeature))
                {
                    _currentIItemsFeature = (IItemsFeature?)value;
                }
                else if (key == typeof(ITlsConnectionFeature))
                {
                    _currentITlsConnectionFeature = (ITlsConnectionFeature?)value;
                }
                else if (key == typeof(IHttpWebSocketFeature))
                {
                    _currentIHttpWebSocketFeature = (IHttpWebSocketFeature?)value;
                }
                else if (key == typeof(ISessionFeature))
                {
                    _currentISessionFeature = (ISessionFeature?)value;
                }
                else if (key == typeof(IHttpMaxRequestBodySizeFeature))
                {
                    _currentIHttpMaxRequestBodySizeFeature = (IHttpMaxRequestBodySizeFeature?)value;
                }
                else if (key == typeof(IHttpMinRequestBodyDataRateFeature))
                {
                    _currentIHttpMinRequestBodyDataRateFeature = (IHttpMinRequestBodyDataRateFeature?)value;
                }
                else if (key == typeof(IHttpMinResponseDataRateFeature))
                {
                    _currentIHttpMinResponseDataRateFeature = (IHttpMinResponseDataRateFeature?)value;
                }
                else if (key == typeof(IHttpBodyControlFeature))
                {
                    _currentIHttpBodyControlFeature = (IHttpBodyControlFeature?)value;
                }
                else if (key == typeof(IHttpResetFeature))
                {
                    _currentIHttpResetFeature = (IHttpResetFeature?)value;
                }
                else
                {
                    ExtraFeatureSet(key, value);
                }
            }
        }

        TFeature? IFeatureCollection.Get<TFeature>() where TFeature : default
        {
            TFeature? feature = default;
            if (typeof(TFeature) == typeof(IHttpRequestFeature))
            {
                feature = (TFeature?)_currentIHttpRequestFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestBodyDetectionFeature))
            {
                feature = (TFeature?)_currentIHttpRequestBodyDetectionFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpResponseFeature))
            {
                feature = (TFeature?)_currentIHttpResponseFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpResponseBodyFeature))
            {
                feature = (TFeature?)_currentIHttpResponseBodyFeature;
            }
            else if (typeof(TFeature) == typeof(IRequestBodyPipeFeature))
            {
                feature = (TFeature?)_currentIRequestBodyPipeFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestIdentifierFeature))
            {
                feature = (TFeature?)_currentIHttpRequestIdentifierFeature;
            }
            else if (typeof(TFeature) == typeof(IServiceProvidersFeature))
            {
                feature = (TFeature?)_currentIServiceProvidersFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestLifetimeFeature))
            {
                feature = (TFeature?)_currentIHttpRequestLifetimeFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpConnectionFeature))
            {
                feature = (TFeature?)_currentIHttpConnectionFeature;
            }
            else if (typeof(TFeature) == typeof(IRouteValuesFeature))
            {
                feature = (TFeature?)_currentIRouteValuesFeature;
            }
            else if (typeof(TFeature) == typeof(IEndpointFeature))
            {
                feature = (TFeature?)_currentIEndpointFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpAuthenticationFeature))
            {
                feature = (TFeature?)_currentIHttpAuthenticationFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestTrailersFeature))
            {
                feature = (TFeature?)_currentIHttpRequestTrailersFeature;
            }
            else if (typeof(TFeature) == typeof(IQueryFeature))
            {
                feature = (TFeature?)_currentIQueryFeature;
            }
            else if (typeof(TFeature) == typeof(IFormFeature))
            {
                feature = (TFeature?)_currentIFormFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpUpgradeFeature))
            {
                feature = (TFeature?)_currentIHttpUpgradeFeature;
            }
            else if (typeof(TFeature) == typeof(IHttp2StreamIdFeature))
            {
                feature = (TFeature?)_currentIHttp2StreamIdFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpResponseTrailersFeature))
            {
                feature = (TFeature?)_currentIHttpResponseTrailersFeature;
            }
            else if (typeof(TFeature) == typeof(IResponseCookiesFeature))
            {
                feature = (TFeature?)_currentIResponseCookiesFeature;
            }
            else if (typeof(TFeature) == typeof(IItemsFeature))
            {
                feature = (TFeature?)_currentIItemsFeature;
            }
            else if (typeof(TFeature) == typeof(ITlsConnectionFeature))
            {
                feature = (TFeature?)_currentITlsConnectionFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpWebSocketFeature))
            {
                feature = (TFeature?)_currentIHttpWebSocketFeature;
            }
            else if (typeof(TFeature) == typeof(ISessionFeature))
            {
                feature = (TFeature?)_currentISessionFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpMaxRequestBodySizeFeature))
            {
                feature = (TFeature?)_currentIHttpMaxRequestBodySizeFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpMinRequestBodyDataRateFeature))
            {
                feature = (TFeature?)_currentIHttpMinRequestBodyDataRateFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpMinResponseDataRateFeature))
            {
                feature = (TFeature?)_currentIHttpMinResponseDataRateFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpBodyControlFeature))
            {
                feature = (TFeature?)_currentIHttpBodyControlFeature;
            }
            else if (typeof(TFeature) == typeof(IHttpResetFeature))
            {
                feature = (TFeature?)_currentIHttpResetFeature;
            }
            else if (MaybeExtra != null)
            {
                feature = (TFeature?)(ExtraFeatureGet(typeof(TFeature)));
            }

            if (feature == null)
            {
                feature = ConnectionFeatures.Get<TFeature>();
            }

            return feature;
        }

        void IFeatureCollection.Set<TFeature>(TFeature? feature) where TFeature : default
        {
            _featureRevision++;
            if (typeof(TFeature) == typeof(IHttpRequestFeature))
            {
                _currentIHttpRequestFeature = (IHttpRequestFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestBodyDetectionFeature))
            {
                _currentIHttpRequestBodyDetectionFeature = (IHttpRequestBodyDetectionFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpResponseFeature))
            {
                _currentIHttpResponseFeature = (IHttpResponseFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpResponseBodyFeature))
            {
                _currentIHttpResponseBodyFeature = (IHttpResponseBodyFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IRequestBodyPipeFeature))
            {
                _currentIRequestBodyPipeFeature = (IRequestBodyPipeFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestIdentifierFeature))
            {
                _currentIHttpRequestIdentifierFeature = (IHttpRequestIdentifierFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IServiceProvidersFeature))
            {
                _currentIServiceProvidersFeature = (IServiceProvidersFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestLifetimeFeature))
            {
                _currentIHttpRequestLifetimeFeature = (IHttpRequestLifetimeFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpConnectionFeature))
            {
                _currentIHttpConnectionFeature = (IHttpConnectionFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IRouteValuesFeature))
            {
                _currentIRouteValuesFeature = (IRouteValuesFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IEndpointFeature))
            {
                _currentIEndpointFeature = (IEndpointFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpAuthenticationFeature))
            {
                _currentIHttpAuthenticationFeature = (IHttpAuthenticationFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpRequestTrailersFeature))
            {
                _currentIHttpRequestTrailersFeature = (IHttpRequestTrailersFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IQueryFeature))
            {
                _currentIQueryFeature = (IQueryFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IFormFeature))
            {
                _currentIFormFeature = (IFormFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpUpgradeFeature))
            {
                _currentIHttpUpgradeFeature = (IHttpUpgradeFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttp2StreamIdFeature))
            {
                _currentIHttp2StreamIdFeature = (IHttp2StreamIdFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpResponseTrailersFeature))
            {
                _currentIHttpResponseTrailersFeature = (IHttpResponseTrailersFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IResponseCookiesFeature))
            {
                _currentIResponseCookiesFeature = (IResponseCookiesFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IItemsFeature))
            {
                _currentIItemsFeature = (IItemsFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(ITlsConnectionFeature))
            {
                _currentITlsConnectionFeature = (ITlsConnectionFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpWebSocketFeature))
            {
                _currentIHttpWebSocketFeature = (IHttpWebSocketFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(ISessionFeature))
            {
                _currentISessionFeature = (ISessionFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpMaxRequestBodySizeFeature))
            {
                _currentIHttpMaxRequestBodySizeFeature = (IHttpMaxRequestBodySizeFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpMinRequestBodyDataRateFeature))
            {
                _currentIHttpMinRequestBodyDataRateFeature = (IHttpMinRequestBodyDataRateFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpMinResponseDataRateFeature))
            {
                _currentIHttpMinResponseDataRateFeature = (IHttpMinResponseDataRateFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpBodyControlFeature))
            {
                _currentIHttpBodyControlFeature = (IHttpBodyControlFeature?)feature;
            }
            else if (typeof(TFeature) == typeof(IHttpResetFeature))
            {
                _currentIHttpResetFeature = (IHttpResetFeature?)feature;
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
            if (_currentIHttpRequestBodyDetectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpRequestBodyDetectionFeature), _currentIHttpRequestBodyDetectionFeature);
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
