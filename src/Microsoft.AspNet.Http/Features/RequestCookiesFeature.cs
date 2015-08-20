// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class RequestCookiesFeature : IRequestCookiesFeature
    {
        private readonly IFeatureCollection _features;
        private readonly FeatureReference<IHttpRequestFeature> _request = FeatureReference<IHttpRequestFeature>.Default;

        private StringValues _original;
        private IReadableStringCollection _parsedValues;

        public RequestCookiesFeature([NotNull] IDictionary<string, StringValues> cookies)
            : this(new ReadableStringCollection(cookies))
        {
        }

        public RequestCookiesFeature([NotNull] IReadableStringCollection cookies)
        {
            _parsedValues = cookies;
        }

        public RequestCookiesFeature([NotNull] IFeatureCollection features)
        {
            _features = features;
        }

        public IReadableStringCollection Cookies
        {
            get
            {
                if (_features == null)
                {
                    return _parsedValues ?? ReadableStringCollection.Empty;
                }

                var headers = _request.Fetch(_features).Headers;
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
                        _request.Fetch(_features).Headers.Remove(HeaderNames.Cookie);
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
                        _request.Fetch(_features).Headers[HeaderNames.Cookie] = _original;
                    }
                }
            }
        }
    }
}