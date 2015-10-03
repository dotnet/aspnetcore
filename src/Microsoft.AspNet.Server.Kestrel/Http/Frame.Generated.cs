
using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Server.Kestrel.Http 
{
    public partial class Frame
    {
        
        private const long flagIHttpRequestFeature = 1;
        private const long flagIHttpResponseFeature = 2;
        private const long flagIHttpRequestIdentifierFeature = 4;
        private const long flagIHttpSendFileFeature = 8;
        private const long flagIServiceProvidersFeature = 16;
        private const long flagIHttpAuthenticationFeature = 32;
        private const long flagIHttpRequestLifetimeFeature = 64;
        private const long flagIQueryFeature = 128;
        private const long flagIFormFeature = 256;
        private const long flagIResponseCookiesFeature = 512;
        private const long flagIItemsFeature = 1024;
        private const long flagIHttpConnectionFeature = 2048;
        private const long flagITlsConnectionFeature = 4096;
        private const long flagIHttpUpgradeFeature = 8192;
        private const long flagIHttpWebSocketFeature = 16384;
        private const long flagISessionFeature = 32768;

        
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

        private long _featureOverridenFlags = 0L;

        private void FastReset()
        {
            _featureOverridenFlags = 0L;
        }

        private object FastFeatureGet(Type key)
        {
            if (key == IHttpRequestFeatureType)
            {
                if ((_featureOverridenFlags & flagIHttpRequestFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.IHttpRequestFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IHttpResponseFeatureType)
            {
                if ((_featureOverridenFlags & flagIHttpResponseFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.IHttpResponseFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IHttpRequestIdentifierFeatureType)
            {
                if ((_featureOverridenFlags & flagIHttpRequestIdentifierFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.IHttpRequestIdentifierFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IHttpSendFileFeatureType)
            {
                if ((_featureOverridenFlags & flagIHttpSendFileFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.IHttpSendFileFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IServiceProvidersFeatureType)
            {
                if ((_featureOverridenFlags & flagIServiceProvidersFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.Internal.IServiceProvidersFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IHttpAuthenticationFeatureType)
            {
                if ((_featureOverridenFlags & flagIHttpAuthenticationFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.Authentication.IHttpAuthenticationFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IHttpRequestLifetimeFeatureType)
            {
                if ((_featureOverridenFlags & flagIHttpRequestLifetimeFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.IHttpRequestLifetimeFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IQueryFeatureType)
            {
                if ((_featureOverridenFlags & flagIQueryFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.Internal.IQueryFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IFormFeatureType)
            {
                if ((_featureOverridenFlags & flagIFormFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.Internal.IFormFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IResponseCookiesFeatureType)
            {
                if ((_featureOverridenFlags & flagIResponseCookiesFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.Internal.IResponseCookiesFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IItemsFeatureType)
            {
                if ((_featureOverridenFlags & flagIItemsFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.Internal.IItemsFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IHttpConnectionFeatureType)
            {
                if ((_featureOverridenFlags & flagIHttpConnectionFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.IHttpConnectionFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == ITlsConnectionFeatureType)
            {
                if ((_featureOverridenFlags & flagITlsConnectionFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.ITlsConnectionFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IHttpUpgradeFeatureType)
            {
                if ((_featureOverridenFlags & flagIHttpUpgradeFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.IHttpUpgradeFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == IHttpWebSocketFeatureType)
            {
                if ((_featureOverridenFlags & flagIHttpWebSocketFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.IHttpWebSocketFeature;
                }
                return SlowFeatureGet(key);
            }
            if (key == ISessionFeatureType)
            {
                if ((_featureOverridenFlags & flagISessionFeature) == 0L)
                {
                    return this as global::Microsoft.AspNet.Http.Features.ISessionFeature;
                }
                return SlowFeatureGet(key);
            }
            return  SlowFeatureGet(key);
        }

        private object SlowFeatureGet(Type key)
        {
            object feature = null;
            if (MaybeExtra?.TryGetValue(key, out feature) ?? false) 
            {
                return feature;
            }
            return null;
        }

        private void FastFeatureSetInner(long flag, Type key, object feature)
        {
            Extra[key] = feature;

            // Altering only an individual bit of the long
            // so need to make sure other concurrent changes are not overridden
            // in a lock-free manner

            long currentFeatureFlags;
            long updatedFeatureFlags;
            do
            {
                currentFeatureFlags = _featureOverridenFlags;
                updatedFeatureFlags = currentFeatureFlags | flag;
            } while (System.Threading.Interlocked.CompareExchange(ref _featureOverridenFlags, updatedFeatureFlags, currentFeatureFlags) != currentFeatureFlags);

            System.Threading.Interlocked.Increment(ref _featureRevision);
        }

        private void FastFeatureSet(Type key, object feature)
        {
            if (key == IHttpRequestFeatureType)
            {
                FastFeatureSetInner(flagIHttpRequestFeature, key, feature);
                return;
            }
            if (key == IHttpResponseFeatureType)
            {
                FastFeatureSetInner(flagIHttpResponseFeature, key, feature);
                return;
            }
            if (key == IHttpRequestIdentifierFeatureType)
            {
                FastFeatureSetInner(flagIHttpRequestIdentifierFeature, key, feature);
                return;
            }
            if (key == IHttpSendFileFeatureType)
            {
                FastFeatureSetInner(flagIHttpSendFileFeature, key, feature);
                return;
            }
            if (key == IServiceProvidersFeatureType)
            {
                FastFeatureSetInner(flagIServiceProvidersFeature, key, feature);
                return;
            }
            if (key == IHttpAuthenticationFeatureType)
            {
                FastFeatureSetInner(flagIHttpAuthenticationFeature, key, feature);
                return;
            }
            if (key == IHttpRequestLifetimeFeatureType)
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
            if (key == IHttpConnectionFeatureType)
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
            Extra[key] = feature;
        }

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {
            if ((_featureOverridenFlags & flagIHttpRequestFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IHttpRequestFeatureType, this as global::Microsoft.AspNet.Http.Features.IHttpRequestFeature);
            }
            if ((_featureOverridenFlags & flagIHttpResponseFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IHttpResponseFeatureType, this as global::Microsoft.AspNet.Http.Features.IHttpResponseFeature);
            }
            if ((_featureOverridenFlags & flagIHttpRequestIdentifierFeature) == 0L)
            {
                yield return new KeyValuePair<Type, object>(IHttpRequestIdentifierFeatureType, this as global::Microsoft.AspNet.Http.Features.IHttpRequestIdentifierFeature);
            }
            if ((_featureOverridenFlags & flagIHttpSendFileFeature) == 0L)
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
