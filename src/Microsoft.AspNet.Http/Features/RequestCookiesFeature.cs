// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class RequestCookiesFeature : IRequestCookiesFeature, IFeatureCache
    {
        private readonly IFeatureCollection _features;
        private int _cachedFeaturesRevision = -1;

        private IHttpRequestFeature _request;

        private StringValues _original;
        private IRequestCookieCollection _parsedValues;
        
        public RequestCookiesFeature(IRequestCookieCollection cookies)
        {
            if (cookies == null)
            {
                throw new ArgumentNullException(nameof(cookies));
            }

            _parsedValues = cookies;
        }

        public RequestCookiesFeature(IFeatureCollection features)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            _features = features;
        }

        void IFeatureCache.CheckFeaturesRevision()
        {
            if (_cachedFeaturesRevision != _features.Revision)
            {
                _request = null;
                _cachedFeaturesRevision = _features.Revision;
            }
        }

        private IHttpRequestFeature HttpRequestFeature
        {
            get { return FeatureHelpers.GetAndCache(this, _features, ref _request); }
        }

        public IRequestCookieCollection Cookies
        {
            get
            {
                if (_features == null)
                {
                    if (_parsedValues == null)
                    {
                        _parsedValues = RequestCookieCollection.Empty;
                    }
                    return _parsedValues;
                }

                var headers = HttpRequestFeature.Headers;
                StringValues current;
                if (!headers.TryGetValue(HeaderNames.Cookie, out current))
                {
                    current = string.Empty;
                }

                if (_parsedValues == null || _original != current)
                {
                    _original = current;
                    _parsedValues = RequestCookieCollection.Parse(current.ToArray());
                }

                return _parsedValues;
            }
            set
            {
                _parsedValues = value;
                _original = StringValues.Empty;
                if (_features != null)
                {
                    if (_parsedValues == null || _parsedValues.Count == 0)
                    {
                        HttpRequestFeature.Headers.Remove(HeaderNames.Cookie);
                    }
                    else
                    {
                        var headers = new List<string>();
                        foreach (var pair in _parsedValues)
                        {
                            headers.Add(new CookieHeaderValue(pair.Key, pair.Value).ToString());
                        }
                        _original = headers.ToArray();
                        HttpRequestFeature.Headers[HeaderNames.Cookie] = _original;
                    }
                }
            }
        }
    }
}