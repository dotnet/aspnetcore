// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        private IReadableStringCollection _parsedValues;

        public RequestCookiesFeature(IDictionary<string, StringValues> cookies)
            : this(new ReadableStringCollection(cookies))
        {
        }

        public RequestCookiesFeature(IReadableStringCollection cookies)
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

        public IReadableStringCollection Cookies
        {
            get
            {
                if (_features == null)
                {
                    return _parsedValues ?? ReadableStringCollection.Empty;
                }

                var headers = HttpRequestFeature.Headers;
                StringValues current;
                if (!headers.TryGetValue(HeaderNames.Cookie, out current))
                {
                    current = StringValues.Empty;
                }

                if (_parsedValues == null || !Enumerable.SequenceEqual(_original, current, StringComparer.Ordinal))
                {
                    _original = current;
                    var collectionParser = _parsedValues as RequestCookiesCollection;
                    if (collectionParser == null)
                    {
                        collectionParser = new RequestCookiesCollection();
                        _parsedValues = collectionParser;
                    }
                    collectionParser.Reparse(current);
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
                            foreach (var cookieValue in pair.Value)
                            {
                                headers.Add(new CookieHeaderValue(pair.Key, cookieValue).ToString());
                            }
                        }
                        _original = headers.ToArray();
                        HttpRequestFeature.Headers[HeaderNames.Cookie] = _original;
                    }
                }
            }
        }
    }
}