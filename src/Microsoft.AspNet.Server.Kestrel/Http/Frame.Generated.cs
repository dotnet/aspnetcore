
using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Server.Kestrel.Http 
{
    public partial class Frame
    {
        private static readonly Type IHttpRequestFeatureType = typeof(global::Microsoft.AspNet.Http.Features.IHttpRequestFeature);
        private static readonly Type IHttpResponseFeatureType = typeof(global::Microsoft.AspNet.Http.Features.IHttpResponseFeature);
        private static readonly Type IHttpRequestIdentifierFeatureType = typeof(global::Microsoft.AspNet.Http.Features.IHttpRequestIdentifierFeature);
        private static readonly Type IHttpSendFileFeatureType = typeof(global::Microsoft.AspNet.Http.Features.IHttpSendFileFeature);
        private static readonly Type IServiceProvidersFeatureType = typeof(global::Microsoft.AspNet.Http.Features.Internal.IServiceProvidersFeature);
        private static readonly Type IHttpAuthenticationFeatureType = typeof(global::Microsoft.AspNet.Http.Features.Authentication.IHttpAuthenticationFeature);
        private static readonly Type IHttpRequestLifetimeFeatureType = typeof(global::Microsoft.AspNet.Http.Features.IHttpRequestLifetimeFeature);
        private static readonly Type IQueryFeatureType = typeof(global::Microsoft.AspNet.Http.Features.Internal.IQueryFeature);
        private static readonly Type IFormFeatureType = typeof(global::Microsoft.AspNet.Http.Features.Internal.IFormFeature);
        private static readonly Type IResponseCookiesFeatureType = typeof(global::Microsoft.AspNet.Http.Features.Internal.IResponseCookiesFeature);
        private static readonly Type IItemsFeatureType = typeof(global::Microsoft.AspNet.Http.Features.Internal.IItemsFeature);
        private static readonly Type IHttpConnectionFeatureType = typeof(global::Microsoft.AspNet.Http.Features.IHttpConnectionFeature);
        private static readonly Type ITlsConnectionFeatureType = typeof(global::Microsoft.AspNet.Http.Features.ITlsConnectionFeature);
        private static readonly Type IHttpUpgradeFeatureType = typeof(global::Microsoft.AspNet.Http.Features.IHttpUpgradeFeature);
        private static readonly Type IHttpWebSocketFeatureType = typeof(global::Microsoft.AspNet.Http.Features.IHttpWebSocketFeature);
        private static readonly Type ISessionFeatureType = typeof(global::Microsoft.AspNet.Http.Features.ISessionFeature);
        private static readonly Type IHttpSendFileFeatureType = typeof(global::Microsoft.AspNet.Http.Features.IHttpSendFileFeature);
        private static readonly Type IHttpUpgradeFeatureType = typeof(global::Microsoft.AspNet.Http.Features.IHttpUpgradeFeature);
        private static readonly Type IResponseCookiesFeatureType = typeof(global::Microsoft.AspNet.Http.Features.Internal.IResponseCookiesFeature);
        private static readonly Type IItemsFeatureType = typeof(global::Microsoft.AspNet.Http.Features.Internal.IItemsFeature);
        private static readonly Type ITlsConnectionFeatureType = typeof(global::Microsoft.AspNet.Http.Features.ITlsConnectionFeature);
        private static readonly Type IHttpWebSocketFeatureType = typeof(global::Microsoft.AspNet.Http.Features.IHttpWebSocketFeature);
        private static readonly Type ISessionFeatureType = typeof(global::Microsoft.AspNet.Http.Features.ISessionFeature);
        private static readonly Type IHttpSendFileFeatureType = typeof(global::Microsoft.AspNet.Http.Features.IHttpSendFileFeature);

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
        private object _currentIResponseCookiesFeature;
        private object _currentIItemsFeature;
        private object _currentITlsConnectionFeature;
        private object _currentIHttpWebSocketFeature;
        private object _currentISessionFeature;
        private object _currentIHttpSendFileFeature;

        private void FastReset()
        {
            _currentIHttpRequestFeature = this;
            _currentIHttpResponseFeature = this;
            _currentIHttpUpgradeFeature = this;
            
            _currentIHttpRequestIdentifierFeature = null;
            _currentIServiceProvidersFeature = null;
            _currentIHttpRequestLifetimeFeature = null;
            _currentIHttpConnectionFeature = null;
            _currentIHttpAuthenticationFeature = null;
            _currentIQueryFeature = null;
            _currentIFormFeature = null;
            _currentIResponseCookiesFeature = null;
            _currentIItemsFeature = null;
            _currentITlsConnectionFeature = null;
            _currentIHttpWebSocketFeature = null;
            _currentISessionFeature = null;
            _currentIHttpSendFileFeature = null;
        }

        private object FastFeatureGet(Type key)
        {
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpRequestFeature))
            {

                return _currentIHttpRequestFeature;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpResponseFeature))
            {
                return _currentIHttpResponseFeature;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpRequestIdentifierFeature))
            {
                if ((_featureOverridenFlags & flagIHttpSendFileFeature) == 0L)
                {
                    return this;
                }
                return SlowFeatureGet(key);
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.Internal.IServiceProvidersFeature))
            {
                if ((_featureOverridenFlags & flagIServiceProvidersFeature) == 0L)
                {
                    return this;
                }
                return SlowFeatureGet(key);
            }
            if (key == IHttpAuthenticationFeatureType)
            {
                if ((_featureOverridenFlags & flagIHttpAuthenticationFeature) == 0L)
                {
                    return this;
                }
                return SlowFeatureGet(key);
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpRequestLifetimeFeature))
            {
                if ((_featureOverridenFlags & flagIHttpRequestLifetimeFeature) == 0L)
                {
                    return this;
                }
                return SlowFeatureGet(key);
            }
            if (key == IQueryFeatureType)
            {
                if ((_featureOverridenFlags & flagIQueryFeature) == 0L)
                {
                    return this;
                }
                return SlowFeatureGet(key);
            }
            if (key == IFormFeatureType)
            {
                if ((_featureOverridenFlags & flagIFormFeature) == 0L)
                {
                    return this;
                }
                return SlowFeatureGet(key);
            }
            if (key == IResponseCookiesFeatureType)
            {
                if ((_featureOverridenFlags & flagIResponseCookiesFeature) == 0L)
                {
                    return this;
                }
                return SlowFeatureGet(key);
            }
            if (key == IItemsFeatureType)
            {
                if ((_featureOverridenFlags & flagIItemsFeature) == 0L)
                {
                    return this;
                }
                return SlowFeatureGet(key);
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpUpgradeFeature))
            {
                return _currentIHttpUpgradeFeature;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.Internal.IResponseCookiesFeature))
            {
                return _currentIResponseCookiesFeature;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.Internal.IItemsFeature))
            {
                return _currentIItemsFeature;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.ITlsConnectionFeature))
            {
                return _currentITlsConnectionFeature;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpWebSocketFeature))
            {
                return _currentIHttpWebSocketFeature;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.ISessionFeature))
            {
                return _currentISessionFeature;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpSendFileFeature))
            {
                return _currentIHttpSendFileFeature;
            }

            return ExtraFeatureGet(key);
        }


        private void FastFeatureSet(Type key, object feature)
        {
            if (key == IHttpRequestFeatureType)
            {
                _currentIHttpRequestFeature = feature;
                return;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpResponseFeature))
            {
                _currentIHttpResponseFeature = feature;
                return;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpRequestIdentifierFeature))
            {
                FastFeatureSetInner(flagIHttpSendFileFeature, key, feature);
                return;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.Internal.IServiceProvidersFeature))
            {
                FastFeatureSetInner(flagIServiceProvidersFeature, key, feature);
                return;
            }
            if (key == IHttpAuthenticationFeatureType)
            {
                FastFeatureSetInner(flagIHttpAuthenticationFeature, key, feature);
                return;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpRequestLifetimeFeature))
            {
                FastFeatureSetInner(flagIHttpRequestLifetimeFeature, key, feature);
                return;
            }
            if (key == IQueryFeatureType)
            {
                FastFeatureSetInner(flagIQueryFeature, key, feature);
                return;
            }
            if (key == IFormFeatureType)
            {
                FastFeatureSetInner(flagIFormFeature, key, feature);
                return;
            }
            if (key == IResponseCookiesFeatureType)
            {
                FastFeatureSetInner(flagIResponseCookiesFeature, key, feature);
                return;
            }
            if (key == IItemsFeatureType)
            {
                FastFeatureSetInner(flagIItemsFeature, key, feature);
                return;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpConnectionFeature))
            {
                FastFeatureSetInner(flagIHttpConnectionFeature, key, feature);
                return;
            }
            if (key == ITlsConnectionFeatureType)
            {
                FastFeatureSetInner(flagITlsConnectionFeature, key, feature);
                return;
            }
            if (key == IHttpUpgradeFeatureType)
            {
                FastFeatureSetInner(flagIHttpUpgradeFeature, key, feature);
                return;
            }
            if (key == IHttpWebSocketFeatureType)
            {
                FastFeatureSetInner(flagIHttpWebSocketFeature, key, feature);
                return;
            }
            if (key == ISessionFeatureType)
            {
                FastFeatureSetInner(flagISessionFeature, key, feature);
                return;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpUpgradeFeature))
            {
                _currentIHttpUpgradeFeature = feature;
                return;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.Internal.IResponseCookiesFeature))
            {
                _currentIResponseCookiesFeature = feature;
                return;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.Internal.IItemsFeature))
            {
                _currentIItemsFeature = feature;
                return;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.ITlsConnectionFeature))
            {
                _currentITlsConnectionFeature = feature;
                return;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpWebSocketFeature))
            {
                _currentIHttpWebSocketFeature = feature;
                return;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.ISessionFeature))
            {
                _currentISessionFeature = feature;
                return;
            }
            if (key == typeof(global::Microsoft.AspNet.Http.Features.IHttpSendFileFeature))
            {
                _currentIHttpSendFileFeature = feature;
                return;
            };
            ExtraFeatureSet(key, feature);
        }

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {
            if (_currentIHttpRequestFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpRequestFeatureType, _currentIHttpRequestFeature as global::Microsoft.AspNet.Http.Features.IHttpRequestFeature);
            }
            if (_currentIHttpResponseFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpResponseFeatureType, _currentIHttpResponseFeature as global::Microsoft.AspNet.Http.Features.IHttpResponseFeature);
            }
            if (_currentIHttpRequestIdentifierFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpSendFileFeatureType, this as global::Microsoft.AspNet.Http.Features.IHttpSendFileFeature);
            }
            if ((_featureOverridenFlags & flagIServiceProvidersFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IServiceProvidersFeatureType, this as global::Microsoft.AspNet.Http.Features.Internal.IServiceProvidersFeature);
            }
            if ((_featureOverridenFlags & flagIHttpAuthenticationFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IHttpAuthenticationFeatureType, this as global::Microsoft.AspNet.Http.Features.Authentication.IHttpAuthenticationFeature);
            }
            if ((_featureOverridenFlags & flagIHttpRequestLifetimeFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IHttpRequestLifetimeFeatureType, this as global::Microsoft.AspNet.Http.Features.IHttpRequestLifetimeFeature);
            }
            if ((_featureOverridenFlags & flagIQueryFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IQueryFeatureType, this as global::Microsoft.AspNet.Http.Features.Internal.IQueryFeature);
            }
            if ((_featureOverridenFlags & flagIFormFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IFormFeatureType, this as global::Microsoft.AspNet.Http.Features.Internal.IFormFeature);
            }
            if ((_featureOverridenFlags & flagIResponseCookiesFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IResponseCookiesFeatureType, this as global::Microsoft.AspNet.Http.Features.Internal.IResponseCookiesFeature);
            }
            if ((_featureOverridenFlags & flagIItemsFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IItemsFeatureType, this as global::Microsoft.AspNet.Http.Features.Internal.IItemsFeature);
            }
            if ((_featureOverridenFlags & flagIHttpConnectionFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IHttpConnectionFeatureType, this as global::Microsoft.AspNet.Http.Features.IHttpConnectionFeature);
            }
            if ((_featureOverridenFlags & flagITlsConnectionFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(ITlsConnectionFeatureType, this as global::Microsoft.AspNet.Http.Features.ITlsConnectionFeature);
            }
            if ((_featureOverridenFlags & flagIHttpUpgradeFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IHttpUpgradeFeatureType, this as global::Microsoft.AspNet.Http.Features.IHttpUpgradeFeature);
            }
            if ((_featureOverridenFlags & flagIHttpWebSocketFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IHttpWebSocketFeatureType, this as global::Microsoft.AspNet.Http.Features.IHttpWebSocketFeature);
            }
            if ((_featureOverridenFlags & flagISessionFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(ISessionFeatureType, this as global::Microsoft.AspNet.Http.Features.ISessionFeature);
            }
            if (_currentIHttpUpgradeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpUpgradeFeatureType, _currentIHttpUpgradeFeature as global::Microsoft.AspNet.Http.Features.IHttpUpgradeFeature);
            }
            if (_currentIResponseCookiesFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IResponseCookiesFeatureType, _currentIResponseCookiesFeature as global::Microsoft.AspNet.Http.Features.Internal.IResponseCookiesFeature);
            }
            if (_currentIItemsFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IItemsFeatureType, _currentIItemsFeature as global::Microsoft.AspNet.Http.Features.Internal.IItemsFeature);
            }
            if (_currentITlsConnectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(ITlsConnectionFeatureType, _currentITlsConnectionFeature as global::Microsoft.AspNet.Http.Features.ITlsConnectionFeature);
            }
            if (_currentIHttpWebSocketFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpWebSocketFeatureType, _currentIHttpWebSocketFeature as global::Microsoft.AspNet.Http.Features.IHttpWebSocketFeature);
            }
            if (_currentISessionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(ISessionFeatureType, _currentISessionFeature as global::Microsoft.AspNet.Http.Features.ISessionFeature);
            }
            if (_currentIHttpSendFileFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpSendFileFeatureType, _currentIHttpSendFileFeature as global::Microsoft.AspNet.Http.Features.IHttpSendFileFeature);
            }

            if (MaybeExtra != null)
            {
                foreach(var item in MaybeExtra)
                {
                    yield return item;
                }
            }
        }
    }
}
