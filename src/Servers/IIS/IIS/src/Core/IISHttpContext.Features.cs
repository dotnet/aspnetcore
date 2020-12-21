// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal partial class IISHttpContext
    {
        private static readonly Type IHttpRequestFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpRequestFeature);
        private static readonly Type IHttpRequestBodyDetectionFeature = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpRequestBodyDetectionFeature);
        private static readonly Type IHttpResponseFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpResponseFeature);
        private static readonly Type IHttpResponseBodyFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature);
        private static readonly Type IHttpRequestIdentifierFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature);
        private static readonly Type IServiceProvidersFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IServiceProvidersFeature);
        private static readonly Type IHttpRequestLifetimeFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature);
        private static readonly Type IHttpConnectionFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature);
        private static readonly Type IHttpAuthenticationFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.Authentication.IHttpAuthenticationFeature);
        private static readonly Type IQueryFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IQueryFeature);
        private static readonly Type IFormFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IFormFeature);
        private static readonly Type IHttpUpgradeFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature);
        private static readonly Type IResponseCookiesFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IResponseCookiesFeature);
        private static readonly Type IItemsFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IItemsFeature);
        private static readonly Type ITlsConnectionFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature);
        private static readonly Type IHttpWebSocketFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpWebSocketFeature);
        private static readonly Type ISessionFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.ISessionFeature);
        private static readonly Type IHttpBodyControlFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature);
        private static readonly Type IISHttpContextType = typeof(IISHttpContext);
        private static readonly Type IServerVariablesFeature = typeof(global::Microsoft.AspNetCore.Http.Features.IServerVariablesFeature);
        private static readonly Type IHttpMaxRequestBodySizeFeature = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature);
        private static readonly Type IHttpResponseTrailersFeature = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpResponseTrailersFeature);
        private static readonly Type IHttpResetFeature = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpResetFeature);

        private object? _currentIHttpRequestFeature;
        private object? _currentIHttpRequestBodyDetectionFeature;
        private object? _currentIHttpResponseFeature;
        private object? _currentIHttpResponseBodyFeature;
        private object? _currentIHttpRequestIdentifierFeature;
        private object? _currentIServiceProvidersFeature;
        private object? _currentIHttpRequestLifetimeFeature;
        private object? _currentIHttpConnectionFeature;
        private object? _currentIHttpAuthenticationFeature;
        private object? _currentIQueryFeature;
        private object? _currentIFormFeature;
        private object? _currentIHttpUpgradeFeature;
        private object? _currentIResponseCookiesFeature;
        private object? _currentIItemsFeature;
        private object? _currentITlsConnectionFeature;
        private object? _currentIHttpWebSocketFeature;
        private object? _currentISessionFeature;
        private object? _currentIHttpBodyControlFeature;
        private object? _currentIServerVariablesFeature;
        private object? _currentIHttpMaxRequestBodySizeFeature;
        private object? _currentIHttpResponseTrailersFeature;
        private object? _currentIHttpResetFeature;

        private void Initialize()
        {
            _currentIHttpRequestFeature = this;
            _currentIHttpRequestBodyDetectionFeature = this;
            _currentIHttpResponseFeature = this;
            _currentIHttpResponseBodyFeature = this;
            _currentIHttpUpgradeFeature = this;
            _currentIHttpRequestIdentifierFeature = this;
            _currentIHttpRequestLifetimeFeature = this;
            _currentIHttpConnectionFeature = this;
            _currentIHttpBodyControlFeature = this;
            _currentIHttpAuthenticationFeature = this;
            _currentIServerVariablesFeature = this;
            _currentIHttpMaxRequestBodySizeFeature = this;
            _currentITlsConnectionFeature = this;
            _currentIHttpResponseTrailersFeature = GetResponseTrailersFeature();
            _currentIHttpResetFeature = GetResetFeature();
        }

        internal object? FastFeatureGet(Type key)
        {
            if (key == IHttpRequestFeatureType)
            {
                return _currentIHttpRequestFeature;
            }
            if (key == IHttpRequestBodyDetectionFeature)
            {
                return _currentIHttpRequestBodyDetectionFeature;
            }
            if (key == IHttpResponseFeatureType)
            {
                return _currentIHttpResponseFeature;
            }
            if (key == IHttpResponseBodyFeatureType)
            {
                return _currentIHttpResponseBodyFeature;
            }
            if (key == IHttpRequestIdentifierFeatureType)
            {
                return _currentIHttpRequestIdentifierFeature;
            }
            if (key == IServiceProvidersFeatureType)
            {
                return _currentIServiceProvidersFeature;
            }
            if (key == IHttpRequestLifetimeFeatureType)
            {
                return _currentIHttpRequestLifetimeFeature;
            }
            if (key == IHttpConnectionFeatureType)
            {
                return _currentIHttpConnectionFeature;
            }
            if (key == IHttpAuthenticationFeatureType)
            {
                return _currentIHttpAuthenticationFeature;
            }
            if (key == IQueryFeatureType)
            {
                return _currentIQueryFeature;
            }
            if (key == IFormFeatureType)
            {
                return _currentIFormFeature;
            }
            if (key == IHttpUpgradeFeatureType)
            {
                return _currentIHttpUpgradeFeature;
            }
            if (key == IResponseCookiesFeatureType)
            {
                return _currentIResponseCookiesFeature;
            }
            if (key == IItemsFeatureType)
            {
                return _currentIItemsFeature;
            }
            if (key == ITlsConnectionFeatureType)
            {
                return _currentITlsConnectionFeature;
            }
            if (key == IHttpWebSocketFeatureType)
            {
                return _currentIHttpWebSocketFeature;
            }
            if (key == ISessionFeatureType)
            {
                return _currentISessionFeature;
            }
            if (key == IHttpBodyControlFeatureType)
            {
                return _currentIHttpBodyControlFeature;
            }
            if (key == IISHttpContextType)
            {
                return this;
            }
            if (key == IServerVariablesFeature)
            {
                return _currentIServerVariablesFeature;
            }
            if (key == IHttpMaxRequestBodySizeFeature)
            {
                return _currentIHttpMaxRequestBodySizeFeature;
            }
            if (key == IHttpResponseTrailersFeature)
            {
                return _currentIHttpResponseTrailersFeature;
            }
            if (key == IHttpResetFeature)
            {
                return _currentIHttpResetFeature;
            }

            return ExtraFeatureGet(key);
        }

        internal void FastFeatureSet(Type key, object? feature)
        {
            _featureRevision++;

            if (key == IHttpRequestFeatureType)
            {
                _currentIHttpRequestFeature = feature;
                return;
            }
            if (key == IHttpRequestBodyDetectionFeature)
            {
                _currentIHttpRequestBodyDetectionFeature = feature;
                return;
            }
            if (key == IHttpResponseFeatureType)
            {
                _currentIHttpResponseFeature = feature;
                return;
            }
            if (key == IHttpResponseBodyFeatureType)
            {
                _currentIHttpResponseBodyFeature = feature;
                return;
            }
            if (key == IHttpRequestIdentifierFeatureType)
            {
                _currentIHttpRequestIdentifierFeature = feature;
                return;
            }
            if (key == IServiceProvidersFeatureType)
            {
                _currentIServiceProvidersFeature = feature;
                return;
            }
            if (key == IHttpRequestLifetimeFeatureType)
            {
                _currentIHttpRequestLifetimeFeature = feature;
                return;
            }
            if (key == IHttpConnectionFeatureType)
            {
                _currentIHttpConnectionFeature = feature;
                return;
            }
            if (key == IHttpAuthenticationFeatureType)
            {
                _currentIHttpAuthenticationFeature = feature;
                return;
            }
            if (key == IQueryFeatureType)
            {
                _currentIQueryFeature = feature;
                return;
            }
            if (key == IFormFeatureType)
            {
                _currentIFormFeature = feature;
                return;
            }
            if (key == IHttpUpgradeFeatureType)
            {
                _currentIHttpUpgradeFeature = feature;
                return;
            }
            if (key == IResponseCookiesFeatureType)
            {
                _currentIResponseCookiesFeature = feature;
                return;
            }
            if (key == IItemsFeatureType)
            {
                _currentIItemsFeature = feature;
                return;
            }
            if (key == ITlsConnectionFeatureType)
            {
                _currentITlsConnectionFeature = feature;
                return;
            }
            if (key == IHttpWebSocketFeatureType)
            {
                _currentIHttpWebSocketFeature = feature;
                return;
            }
            if (key == ISessionFeatureType)
            {
                _currentISessionFeature = feature;
                return;
            }
            if (key == IHttpBodyControlFeatureType)
            {
                _currentIHttpBodyControlFeature = feature;
                return;
            }
            if (key == IServerVariablesFeature)
            {
                _currentIServerVariablesFeature = feature;
                return;
            }
            if (key == IHttpMaxRequestBodySizeFeature)
            {
                _currentIHttpMaxRequestBodySizeFeature = feature;
            }
            if (key == IHttpResponseTrailersFeature)
            {
                _currentIHttpResponseTrailersFeature = feature;
            }
            if (key == IHttpResetFeature)
            {
                _currentIHttpResetFeature = feature;
            }
            if (key == IISHttpContextType)
            {
                throw new InvalidOperationException("Cannot set IISHttpContext in feature collection");
            };
            ExtraFeatureSet(key, feature!); // TODO: What happens if you set an extra feature with a null value?
        }

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {
            if (_currentIHttpRequestFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpRequestFeatureType, _currentIHttpRequestFeature);
            }
            if (_currentIHttpRequestBodyDetectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpRequestBodyDetectionFeature, _currentIHttpRequestBodyDetectionFeature);
            }
            if (_currentIHttpResponseFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpResponseFeatureType, _currentIHttpResponseFeature);
            }
            if (_currentIHttpResponseBodyFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpResponseBodyFeatureType, _currentIHttpResponseBodyFeature);
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
            if (_currentIHttpBodyControlFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpBodyControlFeatureType, _currentIHttpBodyControlFeature);
            }
            if (_currentIServerVariablesFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IServerVariablesFeature, _currentIServerVariablesFeature);
            }
            if (_currentIHttpMaxRequestBodySizeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpMaxRequestBodySizeFeature, _currentIHttpMaxRequestBodySizeFeature);
            }
            if (_currentIHttpResponseTrailersFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpResponseTrailersFeature, _currentIHttpResponseTrailersFeature);
            }
            if (_currentIHttpResetFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpResponseTrailersFeature, _currentIHttpResetFeature);
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
