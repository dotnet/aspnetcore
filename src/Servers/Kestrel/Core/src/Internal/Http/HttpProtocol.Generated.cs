// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

#pragma warning disable CA2252 // WebTransport is a preview feature

#nullable enable

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal partial class HttpProtocol : IFeatureCollection,
                                          IHttpRequestFeature,
                                          IHttpResponseFeature,
                                          IHttpResponseBodyFeature,
                                          IRouteValuesFeature,
                                          IEndpointFeature,
                                          IHttpRequestIdentifierFeature,
                                          IHttpRequestTrailersFeature,
                                          IHttpExtendedConnectFeature,
                                          IHttpUpgradeFeature,
                                          IRequestBodyPipeFeature,
                                          IHttpConnectionFeature,
                                          IHttpRequestLifetimeFeature,
                                          IHttpBodyControlFeature,
                                          IHttpMaxRequestBodySizeFeature,
                                          IHttpRequestBodyDetectionFeature,
                                          IHttpWebTransportFeature,
                                          IBadRequestExceptionFeature
    {
        // Implemented features
        internal protected IHttpRequestFeature? _currentIHttpRequestFeature;
        internal protected IHttpResponseFeature? _currentIHttpResponseFeature;
        internal protected IHttpResponseBodyFeature? _currentIHttpResponseBodyFeature;
        internal protected IRouteValuesFeature? _currentIRouteValuesFeature;
        internal protected IEndpointFeature? _currentIEndpointFeature;
        internal protected IHttpRequestIdentifierFeature? _currentIHttpRequestIdentifierFeature;
        internal protected IHttpRequestTrailersFeature? _currentIHttpRequestTrailersFeature;
        internal protected IHttpExtendedConnectFeature? _currentIHttpExtendedConnectFeature;
        internal protected IHttpUpgradeFeature? _currentIHttpUpgradeFeature;
        internal protected IRequestBodyPipeFeature? _currentIRequestBodyPipeFeature;
        internal protected IHttpConnectionFeature? _currentIHttpConnectionFeature;
        internal protected IHttpRequestLifetimeFeature? _currentIHttpRequestLifetimeFeature;
        internal protected IHttpBodyControlFeature? _currentIHttpBodyControlFeature;
        internal protected IHttpMaxRequestBodySizeFeature? _currentIHttpMaxRequestBodySizeFeature;
        internal protected IHttpRequestBodyDetectionFeature? _currentIHttpRequestBodyDetectionFeature;
        internal protected IHttpWebTransportFeature? _currentIHttpWebTransportFeature;
        internal protected IBadRequestExceptionFeature? _currentIBadRequestExceptionFeature;

        // Other reserved feature slots
        internal protected IServiceProvidersFeature? _currentIServiceProvidersFeature;
        internal protected IHttpActivityFeature? _currentIHttpActivityFeature;
        internal protected IHttpMetricsTagsFeature? _currentIHttpMetricsTagsFeature;
        internal protected IItemsFeature? _currentIItemsFeature;
        internal protected IQueryFeature? _currentIQueryFeature;
        internal protected IFormFeature? _currentIFormFeature;
        internal protected IHttpAuthenticationFeature? _currentIHttpAuthenticationFeature;
        internal protected ISessionFeature? _currentISessionFeature;
        internal protected IResponseCookiesFeature? _currentIResponseCookiesFeature;
        internal protected IHttpResponseTrailersFeature? _currentIHttpResponseTrailersFeature;
        internal protected ITlsConnectionFeature? _currentITlsConnectionFeature;
        internal protected IHttpWebSocketFeature? _currentIHttpWebSocketFeature;
        internal protected IHttpRequestTimeoutFeature? _currentIHttpRequestTimeoutFeature;
        internal protected IHttp2StreamIdFeature? _currentIHttp2StreamIdFeature;
        internal protected IHttpMinRequestBodyDataRateFeature? _currentIHttpMinRequestBodyDataRateFeature;
        internal protected IHttpMinResponseDataRateFeature? _currentIHttpMinResponseDataRateFeature;
        internal protected IHttpResetFeature? _currentIHttpResetFeature;
        internal protected IPersistentStateFeature? _currentIPersistentStateFeature;

        private int _featureRevision;

        private List<KeyValuePair<Type, object>>? MaybeExtra;

        private void FastReset()
        {
            _currentIHttpRequestFeature = this;
            _currentIHttpResponseFeature = this;
            _currentIHttpResponseBodyFeature = this;
            _currentIRouteValuesFeature = this;
            _currentIEndpointFeature = this;
            _currentIHttpRequestIdentifierFeature = this;
            _currentIHttpRequestTrailersFeature = this;
            _currentIHttpExtendedConnectFeature = this;
            _currentIHttpUpgradeFeature = this;
            _currentIRequestBodyPipeFeature = this;
            _currentIHttpConnectionFeature = this;
            _currentIHttpRequestLifetimeFeature = this;
            _currentIHttpBodyControlFeature = this;
            _currentIHttpMaxRequestBodySizeFeature = this;
            _currentIHttpRequestBodyDetectionFeature = this;
            _currentIHttpWebTransportFeature = this;
            _currentIBadRequestExceptionFeature = this;

            _currentIServiceProvidersFeature = null;
            _currentIHttpActivityFeature = null;
            _currentIHttpMetricsTagsFeature = null;
            _currentIItemsFeature = null;
            _currentIQueryFeature = null;
            _currentIFormFeature = null;
            _currentIHttpAuthenticationFeature = null;
            _currentISessionFeature = null;
            _currentIResponseCookiesFeature = null;
            _currentIHttpResponseTrailersFeature = null;
            _currentITlsConnectionFeature = null;
            _currentIHttpWebSocketFeature = null;
            _currentIHttpRequestTimeoutFeature = null;
            _currentIHttp2StreamIdFeature = null;
            _currentIHttpMinRequestBodyDataRateFeature = null;
            _currentIHttpMinResponseDataRateFeature = null;
            _currentIHttpResetFeature = null;
            _currentIPersistentStateFeature = null;
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
                else if (key == typeof(IHttpResponseFeature))
                {
                    feature = _currentIHttpResponseFeature;
                }
                else if (key == typeof(IHttpResponseBodyFeature))
                {
                    feature = _currentIHttpResponseBodyFeature;
                }
                else if (key == typeof(IRouteValuesFeature))
                {
                    feature = _currentIRouteValuesFeature;
                }
                else if (key == typeof(IEndpointFeature))
                {
                    feature = _currentIEndpointFeature;
                }
                else if (key == typeof(IServiceProvidersFeature))
                {
                    feature = _currentIServiceProvidersFeature;
                }
                else if (key == typeof(IHttpActivityFeature))
                {
                    feature = _currentIHttpActivityFeature;
                }
                else if (key == typeof(IHttpMetricsTagsFeature))
                {
                    feature = _currentIHttpMetricsTagsFeature;
                }
                else if (key == typeof(IItemsFeature))
                {
                    feature = _currentIItemsFeature;
                }
                else if (key == typeof(IQueryFeature))
                {
                    feature = _currentIQueryFeature;
                }
                else if (key == typeof(IRequestBodyPipeFeature))
                {
                    feature = _currentIRequestBodyPipeFeature;
                }
                else if (key == typeof(IFormFeature))
                {
                    feature = _currentIFormFeature;
                }
                else if (key == typeof(IHttpAuthenticationFeature))
                {
                    feature = _currentIHttpAuthenticationFeature;
                }
                else if (key == typeof(IHttpRequestIdentifierFeature))
                {
                    feature = _currentIHttpRequestIdentifierFeature;
                }
                else if (key == typeof(IHttpConnectionFeature))
                {
                    feature = _currentIHttpConnectionFeature;
                }
                else if (key == typeof(ISessionFeature))
                {
                    feature = _currentISessionFeature;
                }
                else if (key == typeof(IResponseCookiesFeature))
                {
                    feature = _currentIResponseCookiesFeature;
                }
                else if (key == typeof(IHttpRequestTrailersFeature))
                {
                    feature = _currentIHttpRequestTrailersFeature;
                }
                else if (key == typeof(IHttpResponseTrailersFeature))
                {
                    feature = _currentIHttpResponseTrailersFeature;
                }
                else if (key == typeof(ITlsConnectionFeature))
                {
                    feature = _currentITlsConnectionFeature;
                }
                else if (key == typeof(IHttpExtendedConnectFeature))
                {
                    feature = _currentIHttpExtendedConnectFeature;
                }
                else if (key == typeof(IHttpUpgradeFeature))
                {
                    feature = _currentIHttpUpgradeFeature;
                }
                else if (key == typeof(IHttpWebSocketFeature))
                {
                    feature = _currentIHttpWebSocketFeature;
                }
                else if (key == typeof(IHttpWebTransportFeature))
                {
                    feature = _currentIHttpWebTransportFeature;
                }
                else if (key == typeof(IBadRequestExceptionFeature))
                {
                    feature = _currentIBadRequestExceptionFeature;
                }
                else if (key == typeof(IHttpRequestTimeoutFeature))
                {
                    feature = _currentIHttpRequestTimeoutFeature;
                }
                else if (key == typeof(IHttp2StreamIdFeature))
                {
                    feature = _currentIHttp2StreamIdFeature;
                }
                else if (key == typeof(IHttpRequestLifetimeFeature))
                {
                    feature = _currentIHttpRequestLifetimeFeature;
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
                else if (key == typeof(IHttpRequestBodyDetectionFeature))
                {
                    feature = _currentIHttpRequestBodyDetectionFeature;
                }
                else if (key == typeof(IHttpResetFeature))
                {
                    feature = _currentIHttpResetFeature;
                }
                else if (key == typeof(IPersistentStateFeature))
                {
                    feature = _currentIPersistentStateFeature;
                }
                else if (MaybeExtra != null)
                {
                    feature = ExtraFeatureGet(key);
                }

                return feature ?? ConnectionFeatures?[key];
            }

            set
            {
                _featureRevision++;

                if (key == typeof(IHttpRequestFeature))
                {
                    _currentIHttpRequestFeature = (IHttpRequestFeature?)value;
                }
                else if (key == typeof(IHttpResponseFeature))
                {
                    _currentIHttpResponseFeature = (IHttpResponseFeature?)value;
                }
                else if (key == typeof(IHttpResponseBodyFeature))
                {
                    _currentIHttpResponseBodyFeature = (IHttpResponseBodyFeature?)value;
                }
                else if (key == typeof(IRouteValuesFeature))
                {
                    _currentIRouteValuesFeature = (IRouteValuesFeature?)value;
                }
                else if (key == typeof(IEndpointFeature))
                {
                    _currentIEndpointFeature = (IEndpointFeature?)value;
                }
                else if (key == typeof(IServiceProvidersFeature))
                {
                    _currentIServiceProvidersFeature = (IServiceProvidersFeature?)value;
                }
                else if (key == typeof(IHttpActivityFeature))
                {
                    _currentIHttpActivityFeature = (IHttpActivityFeature?)value;
                }
                else if (key == typeof(IHttpMetricsTagsFeature))
                {
                    _currentIHttpMetricsTagsFeature = (IHttpMetricsTagsFeature?)value;
                }
                else if (key == typeof(IItemsFeature))
                {
                    _currentIItemsFeature = (IItemsFeature?)value;
                }
                else if (key == typeof(IQueryFeature))
                {
                    _currentIQueryFeature = (IQueryFeature?)value;
                }
                else if (key == typeof(IRequestBodyPipeFeature))
                {
                    _currentIRequestBodyPipeFeature = (IRequestBodyPipeFeature?)value;
                }
                else if (key == typeof(IFormFeature))
                {
                    _currentIFormFeature = (IFormFeature?)value;
                }
                else if (key == typeof(IHttpAuthenticationFeature))
                {
                    _currentIHttpAuthenticationFeature = (IHttpAuthenticationFeature?)value;
                }
                else if (key == typeof(IHttpRequestIdentifierFeature))
                {
                    _currentIHttpRequestIdentifierFeature = (IHttpRequestIdentifierFeature?)value;
                }
                else if (key == typeof(IHttpConnectionFeature))
                {
                    _currentIHttpConnectionFeature = (IHttpConnectionFeature?)value;
                }
                else if (key == typeof(ISessionFeature))
                {
                    _currentISessionFeature = (ISessionFeature?)value;
                }
                else if (key == typeof(IResponseCookiesFeature))
                {
                    _currentIResponseCookiesFeature = (IResponseCookiesFeature?)value;
                }
                else if (key == typeof(IHttpRequestTrailersFeature))
                {
                    _currentIHttpRequestTrailersFeature = (IHttpRequestTrailersFeature?)value;
                }
                else if (key == typeof(IHttpResponseTrailersFeature))
                {
                    _currentIHttpResponseTrailersFeature = (IHttpResponseTrailersFeature?)value;
                }
                else if (key == typeof(ITlsConnectionFeature))
                {
                    _currentITlsConnectionFeature = (ITlsConnectionFeature?)value;
                }
                else if (key == typeof(IHttpExtendedConnectFeature))
                {
                    _currentIHttpExtendedConnectFeature = (IHttpExtendedConnectFeature?)value;
                }
                else if (key == typeof(IHttpUpgradeFeature))
                {
                    _currentIHttpUpgradeFeature = (IHttpUpgradeFeature?)value;
                }
                else if (key == typeof(IHttpWebSocketFeature))
                {
                    _currentIHttpWebSocketFeature = (IHttpWebSocketFeature?)value;
                }
                else if (key == typeof(IHttpWebTransportFeature))
                {
                    _currentIHttpWebTransportFeature = (IHttpWebTransportFeature?)value;
                }
                else if (key == typeof(IBadRequestExceptionFeature))
                {
                    _currentIBadRequestExceptionFeature = (IBadRequestExceptionFeature?)value;
                }
                else if (key == typeof(IHttpRequestTimeoutFeature))
                {
                    _currentIHttpRequestTimeoutFeature = (IHttpRequestTimeoutFeature?)value;
                }
                else if (key == typeof(IHttp2StreamIdFeature))
                {
                    _currentIHttp2StreamIdFeature = (IHttp2StreamIdFeature?)value;
                }
                else if (key == typeof(IHttpRequestLifetimeFeature))
                {
                    _currentIHttpRequestLifetimeFeature = (IHttpRequestLifetimeFeature?)value;
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
                else if (key == typeof(IHttpRequestBodyDetectionFeature))
                {
                    _currentIHttpRequestBodyDetectionFeature = (IHttpRequestBodyDetectionFeature?)value;
                }
                else if (key == typeof(IHttpResetFeature))
                {
                    _currentIHttpResetFeature = (IHttpResetFeature?)value;
                }
                else if (key == typeof(IPersistentStateFeature))
                {
                    _currentIPersistentStateFeature = (IPersistentStateFeature?)value;
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
            if (typeof(TFeature) == typeof(IHttpRequestFeature))
            {
                feature = Unsafe.As<IHttpRequestFeature?, TFeature?>(ref _currentIHttpRequestFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpResponseFeature))
            {
                feature = Unsafe.As<IHttpResponseFeature?, TFeature?>(ref _currentIHttpResponseFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpResponseBodyFeature))
            {
                feature = Unsafe.As<IHttpResponseBodyFeature?, TFeature?>(ref _currentIHttpResponseBodyFeature);
            }
            else if (typeof(TFeature) == typeof(IRouteValuesFeature))
            {
                feature = Unsafe.As<IRouteValuesFeature?, TFeature?>(ref _currentIRouteValuesFeature);
            }
            else if (typeof(TFeature) == typeof(IEndpointFeature))
            {
                feature = Unsafe.As<IEndpointFeature?, TFeature?>(ref _currentIEndpointFeature);
            }
            else if (typeof(TFeature) == typeof(IServiceProvidersFeature))
            {
                feature = Unsafe.As<IServiceProvidersFeature?, TFeature?>(ref _currentIServiceProvidersFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpActivityFeature))
            {
                feature = Unsafe.As<IHttpActivityFeature?, TFeature?>(ref _currentIHttpActivityFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpMetricsTagsFeature))
            {
                feature = Unsafe.As<IHttpMetricsTagsFeature?, TFeature?>(ref _currentIHttpMetricsTagsFeature);
            }
            else if (typeof(TFeature) == typeof(IItemsFeature))
            {
                feature = Unsafe.As<IItemsFeature?, TFeature?>(ref _currentIItemsFeature);
            }
            else if (typeof(TFeature) == typeof(IQueryFeature))
            {
                feature = Unsafe.As<IQueryFeature?, TFeature?>(ref _currentIQueryFeature);
            }
            else if (typeof(TFeature) == typeof(IRequestBodyPipeFeature))
            {
                feature = Unsafe.As<IRequestBodyPipeFeature?, TFeature?>(ref _currentIRequestBodyPipeFeature);
            }
            else if (typeof(TFeature) == typeof(IFormFeature))
            {
                feature = Unsafe.As<IFormFeature?, TFeature?>(ref _currentIFormFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpAuthenticationFeature))
            {
                feature = Unsafe.As<IHttpAuthenticationFeature?, TFeature?>(ref _currentIHttpAuthenticationFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpRequestIdentifierFeature))
            {
                feature = Unsafe.As<IHttpRequestIdentifierFeature?, TFeature?>(ref _currentIHttpRequestIdentifierFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpConnectionFeature))
            {
                feature = Unsafe.As<IHttpConnectionFeature?, TFeature?>(ref _currentIHttpConnectionFeature);
            }
            else if (typeof(TFeature) == typeof(ISessionFeature))
            {
                feature = Unsafe.As<ISessionFeature?, TFeature?>(ref _currentISessionFeature);
            }
            else if (typeof(TFeature) == typeof(IResponseCookiesFeature))
            {
                feature = Unsafe.As<IResponseCookiesFeature?, TFeature?>(ref _currentIResponseCookiesFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpRequestTrailersFeature))
            {
                feature = Unsafe.As<IHttpRequestTrailersFeature?, TFeature?>(ref _currentIHttpRequestTrailersFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpResponseTrailersFeature))
            {
                feature = Unsafe.As<IHttpResponseTrailersFeature?, TFeature?>(ref _currentIHttpResponseTrailersFeature);
            }
            else if (typeof(TFeature) == typeof(ITlsConnectionFeature))
            {
                feature = Unsafe.As<ITlsConnectionFeature?, TFeature?>(ref _currentITlsConnectionFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpExtendedConnectFeature))
            {
                feature = Unsafe.As<IHttpExtendedConnectFeature?, TFeature?>(ref _currentIHttpExtendedConnectFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpUpgradeFeature))
            {
                feature = Unsafe.As<IHttpUpgradeFeature?, TFeature?>(ref _currentIHttpUpgradeFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpWebSocketFeature))
            {
                feature = Unsafe.As<IHttpWebSocketFeature?, TFeature?>(ref _currentIHttpWebSocketFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpWebTransportFeature))
            {
                feature = Unsafe.As<IHttpWebTransportFeature?, TFeature?>(ref _currentIHttpWebTransportFeature);
            }
            else if (typeof(TFeature) == typeof(IBadRequestExceptionFeature))
            {
                feature = Unsafe.As<IBadRequestExceptionFeature?, TFeature?>(ref _currentIBadRequestExceptionFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpRequestTimeoutFeature))
            {
                feature = Unsafe.As<IHttpRequestTimeoutFeature?, TFeature?>(ref _currentIHttpRequestTimeoutFeature);
            }
            else if (typeof(TFeature) == typeof(IHttp2StreamIdFeature))
            {
                feature = Unsafe.As<IHttp2StreamIdFeature?, TFeature?>(ref _currentIHttp2StreamIdFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpRequestLifetimeFeature))
            {
                feature = Unsafe.As<IHttpRequestLifetimeFeature?, TFeature?>(ref _currentIHttpRequestLifetimeFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpMaxRequestBodySizeFeature))
            {
                feature = Unsafe.As<IHttpMaxRequestBodySizeFeature?, TFeature?>(ref _currentIHttpMaxRequestBodySizeFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpMinRequestBodyDataRateFeature))
            {
                feature = Unsafe.As<IHttpMinRequestBodyDataRateFeature?, TFeature?>(ref _currentIHttpMinRequestBodyDataRateFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpMinResponseDataRateFeature))
            {
                feature = Unsafe.As<IHttpMinResponseDataRateFeature?, TFeature?>(ref _currentIHttpMinResponseDataRateFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpBodyControlFeature))
            {
                feature = Unsafe.As<IHttpBodyControlFeature?, TFeature?>(ref _currentIHttpBodyControlFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpRequestBodyDetectionFeature))
            {
                feature = Unsafe.As<IHttpRequestBodyDetectionFeature?, TFeature?>(ref _currentIHttpRequestBodyDetectionFeature);
            }
            else if (typeof(TFeature) == typeof(IHttpResetFeature))
            {
                feature = Unsafe.As<IHttpResetFeature?, TFeature?>(ref _currentIHttpResetFeature);
            }
            else if (typeof(TFeature) == typeof(IPersistentStateFeature))
            {
                feature = Unsafe.As<IPersistentStateFeature?, TFeature?>(ref _currentIPersistentStateFeature);
            }
            else if (MaybeExtra != null)
            {
                feature = (TFeature?)(ExtraFeatureGet(typeof(TFeature)));
            }

            if (feature == null && ConnectionFeatures != null)
            {
                feature = ConnectionFeatures.Get<TFeature>();
            }

            return feature;
        }

        void IFeatureCollection.Set<TFeature>(TFeature? feature) where TFeature : default
        {
            // Using Unsafe.As for the cast due to https://github.com/dotnet/runtime/issues/49614
            // The type of TFeature is confirmed by the typeof() check and the As cast only accepts
            // that type; however the Jit does not eliminate a regular cast in a shared generic.

            _featureRevision++;
            if (typeof(TFeature) == typeof(IHttpRequestFeature))
            {
                _currentIHttpRequestFeature = Unsafe.As<TFeature?, IHttpRequestFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpResponseFeature))
            {
                _currentIHttpResponseFeature = Unsafe.As<TFeature?, IHttpResponseFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpResponseBodyFeature))
            {
                _currentIHttpResponseBodyFeature = Unsafe.As<TFeature?, IHttpResponseBodyFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IRouteValuesFeature))
            {
                _currentIRouteValuesFeature = Unsafe.As<TFeature?, IRouteValuesFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IEndpointFeature))
            {
                _currentIEndpointFeature = Unsafe.As<TFeature?, IEndpointFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IServiceProvidersFeature))
            {
                _currentIServiceProvidersFeature = Unsafe.As<TFeature?, IServiceProvidersFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpActivityFeature))
            {
                _currentIHttpActivityFeature = Unsafe.As<TFeature?, IHttpActivityFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpMetricsTagsFeature))
            {
                _currentIHttpMetricsTagsFeature = Unsafe.As<TFeature?, IHttpMetricsTagsFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IItemsFeature))
            {
                _currentIItemsFeature = Unsafe.As<TFeature?, IItemsFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IQueryFeature))
            {
                _currentIQueryFeature = Unsafe.As<TFeature?, IQueryFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IRequestBodyPipeFeature))
            {
                _currentIRequestBodyPipeFeature = Unsafe.As<TFeature?, IRequestBodyPipeFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IFormFeature))
            {
                _currentIFormFeature = Unsafe.As<TFeature?, IFormFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpAuthenticationFeature))
            {
                _currentIHttpAuthenticationFeature = Unsafe.As<TFeature?, IHttpAuthenticationFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpRequestIdentifierFeature))
            {
                _currentIHttpRequestIdentifierFeature = Unsafe.As<TFeature?, IHttpRequestIdentifierFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpConnectionFeature))
            {
                _currentIHttpConnectionFeature = Unsafe.As<TFeature?, IHttpConnectionFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(ISessionFeature))
            {
                _currentISessionFeature = Unsafe.As<TFeature?, ISessionFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IResponseCookiesFeature))
            {
                _currentIResponseCookiesFeature = Unsafe.As<TFeature?, IResponseCookiesFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpRequestTrailersFeature))
            {
                _currentIHttpRequestTrailersFeature = Unsafe.As<TFeature?, IHttpRequestTrailersFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpResponseTrailersFeature))
            {
                _currentIHttpResponseTrailersFeature = Unsafe.As<TFeature?, IHttpResponseTrailersFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(ITlsConnectionFeature))
            {
                _currentITlsConnectionFeature = Unsafe.As<TFeature?, ITlsConnectionFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpExtendedConnectFeature))
            {
                _currentIHttpExtendedConnectFeature = Unsafe.As<TFeature?, IHttpExtendedConnectFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpUpgradeFeature))
            {
                _currentIHttpUpgradeFeature = Unsafe.As<TFeature?, IHttpUpgradeFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpWebSocketFeature))
            {
                _currentIHttpWebSocketFeature = Unsafe.As<TFeature?, IHttpWebSocketFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpWebTransportFeature))
            {
                _currentIHttpWebTransportFeature = Unsafe.As<TFeature?, IHttpWebTransportFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IBadRequestExceptionFeature))
            {
                _currentIBadRequestExceptionFeature = Unsafe.As<TFeature?, IBadRequestExceptionFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpRequestTimeoutFeature))
            {
                _currentIHttpRequestTimeoutFeature = Unsafe.As<TFeature?, IHttpRequestTimeoutFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttp2StreamIdFeature))
            {
                _currentIHttp2StreamIdFeature = Unsafe.As<TFeature?, IHttp2StreamIdFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpRequestLifetimeFeature))
            {
                _currentIHttpRequestLifetimeFeature = Unsafe.As<TFeature?, IHttpRequestLifetimeFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpMaxRequestBodySizeFeature))
            {
                _currentIHttpMaxRequestBodySizeFeature = Unsafe.As<TFeature?, IHttpMaxRequestBodySizeFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpMinRequestBodyDataRateFeature))
            {
                _currentIHttpMinRequestBodyDataRateFeature = Unsafe.As<TFeature?, IHttpMinRequestBodyDataRateFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpMinResponseDataRateFeature))
            {
                _currentIHttpMinResponseDataRateFeature = Unsafe.As<TFeature?, IHttpMinResponseDataRateFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpBodyControlFeature))
            {
                _currentIHttpBodyControlFeature = Unsafe.As<TFeature?, IHttpBodyControlFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpRequestBodyDetectionFeature))
            {
                _currentIHttpRequestBodyDetectionFeature = Unsafe.As<TFeature?, IHttpRequestBodyDetectionFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IHttpResetFeature))
            {
                _currentIHttpResetFeature = Unsafe.As<TFeature?, IHttpResetFeature?>(ref feature);
            }
            else if (typeof(TFeature) == typeof(IPersistentStateFeature))
            {
                _currentIPersistentStateFeature = Unsafe.As<TFeature?, IPersistentStateFeature?>(ref feature);
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
            if (_currentIRouteValuesFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IRouteValuesFeature), _currentIRouteValuesFeature);
            }
            if (_currentIEndpointFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IEndpointFeature), _currentIEndpointFeature);
            }
            if (_currentIServiceProvidersFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IServiceProvidersFeature), _currentIServiceProvidersFeature);
            }
            if (_currentIHttpActivityFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpActivityFeature), _currentIHttpActivityFeature);
            }
            if (_currentIHttpMetricsTagsFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpMetricsTagsFeature), _currentIHttpMetricsTagsFeature);
            }
            if (_currentIItemsFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IItemsFeature), _currentIItemsFeature);
            }
            if (_currentIQueryFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IQueryFeature), _currentIQueryFeature);
            }
            if (_currentIRequestBodyPipeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IRequestBodyPipeFeature), _currentIRequestBodyPipeFeature);
            }
            if (_currentIFormFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IFormFeature), _currentIFormFeature);
            }
            if (_currentIHttpAuthenticationFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpAuthenticationFeature), _currentIHttpAuthenticationFeature);
            }
            if (_currentIHttpRequestIdentifierFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpRequestIdentifierFeature), _currentIHttpRequestIdentifierFeature);
            }
            if (_currentIHttpConnectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpConnectionFeature), _currentIHttpConnectionFeature);
            }
            if (_currentISessionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(ISessionFeature), _currentISessionFeature);
            }
            if (_currentIResponseCookiesFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IResponseCookiesFeature), _currentIResponseCookiesFeature);
            }
            if (_currentIHttpRequestTrailersFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpRequestTrailersFeature), _currentIHttpRequestTrailersFeature);
            }
            if (_currentIHttpResponseTrailersFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpResponseTrailersFeature), _currentIHttpResponseTrailersFeature);
            }
            if (_currentITlsConnectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(ITlsConnectionFeature), _currentITlsConnectionFeature);
            }
            if (_currentIHttpExtendedConnectFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpExtendedConnectFeature), _currentIHttpExtendedConnectFeature);
            }
            if (_currentIHttpUpgradeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpUpgradeFeature), _currentIHttpUpgradeFeature);
            }
            if (_currentIHttpWebSocketFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpWebSocketFeature), _currentIHttpWebSocketFeature);
            }
            if (_currentIHttpWebTransportFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpWebTransportFeature), _currentIHttpWebTransportFeature);
            }
            if (_currentIBadRequestExceptionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IBadRequestExceptionFeature), _currentIBadRequestExceptionFeature);
            }
            if (_currentIHttpRequestTimeoutFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpRequestTimeoutFeature), _currentIHttpRequestTimeoutFeature);
            }
            if (_currentIHttp2StreamIdFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttp2StreamIdFeature), _currentIHttp2StreamIdFeature);
            }
            if (_currentIHttpRequestLifetimeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpRequestLifetimeFeature), _currentIHttpRequestLifetimeFeature);
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
            if (_currentIHttpRequestBodyDetectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpRequestBodyDetectionFeature), _currentIHttpRequestBodyDetectionFeature);
            }
            if (_currentIHttpResetFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpResetFeature), _currentIHttpResetFeature);
            }
            if (_currentIPersistentStateFeature != null)
            {
                yield return new KeyValuePair<Type, object>(typeof(IPersistentStateFeature), _currentIPersistentStateFeature);
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
