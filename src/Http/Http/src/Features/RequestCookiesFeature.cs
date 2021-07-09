// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Default implementation for <see cref="IRequestCookiesFeature"/>.
    /// </summary>
    public class RequestCookiesFeature : IRequestCookiesFeature
    {
        // Lambda hoisted to static readonly field to improve inlining https://github.com/dotnet/roslyn/issues/13624
        private static readonly Func<IFeatureCollection, IHttpRequestFeature?> _nullRequestFeature = f => null;

        private FeatureReferences<IHttpRequestFeature> _features;
        private StringValues _original;
        private IRequestCookieCollection? _parsedValues;

        /// <summary>
        /// Initializes a new instance of <see cref="RequestCookiesFeature"/>.
        /// </summary>
        /// <param name="cookies">The <see cref="IRequestCookieCollection"/> to use as backing store.</param>
        public RequestCookiesFeature(IRequestCookieCollection cookies)
        {
            if (cookies == null)
            {
                throw new ArgumentNullException(nameof(cookies));
            }

            _parsedValues = cookies;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RequestCookiesFeature"/>.
        /// </summary>
        /// <param name="features">The <see cref="IFeatureCollection"/> to initialize.</param>
        public RequestCookiesFeature(IFeatureCollection features)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            _features.Initalize(features);
        }

        private IHttpRequestFeature HttpRequestFeature =>
            _features.Fetch(ref _features.Cache, _nullRequestFeature)!;

        /// <inheritdoc />
        public IRequestCookieCollection Cookies
        {
            get
            {
                if (_features.Collection == null)
                {
                    if (_parsedValues == null)
                    {
                        _parsedValues = RequestCookieCollection.Empty;
                    }
                    return _parsedValues;
                }

                var headers = HttpRequestFeature.Headers;
                var current = headers.Cookie;

                if (_parsedValues == null || _original != current)
                {
                    _original = current;
                    _parsedValues = RequestCookieCollection.Parse(current);
                }

                return _parsedValues;
            }
            set
            {
                _parsedValues = value;
                _original = StringValues.Empty;
                if (_features.Collection != null)
                {
                    if (_parsedValues == null || _parsedValues.Count == 0)
                    {
                        HttpRequestFeature.Headers.Cookie = default;
                    }
                    else
                    {
                        var headers = new List<string>(_parsedValues.Count);
                        foreach (var pair in _parsedValues)
                        {
                            headers.Add(new CookieHeaderValue(pair.Key, pair.Value).ToString());
                        }
                        _original = headers.ToArray();
                        HttpRequestFeature.Headers.Cookie = _original;
                    }
                }
            }
        }
    }
}
