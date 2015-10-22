// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.WebUtilities;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class QueryFeature : IQueryFeature, IFeatureCache
    {
        private readonly IFeatureCollection _features;
        private int _cachedFeaturesRevision = -1;

        private IHttpRequestFeature _request;

        private string _original;
        private IQueryCollection _parsedValues;

        public QueryFeature(IQueryCollection query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            _parsedValues = query;
        }

        public QueryFeature(IFeatureCollection features)
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

        public IQueryCollection Query
        {
            get
            {
                if (_features == null)
                {
                    if (_parsedValues == null)
                    {
                        _parsedValues = QueryCollection.Empty;
                    }
                    return _parsedValues;
                }

                var current = HttpRequestFeature.QueryString;
                if (_parsedValues == null || !string.Equals(_original, current, StringComparison.Ordinal))
                {
                    _original = current;

                    var result = QueryHelpers.ParseNullableQuery(current);

                    if (result == null)
                    {
                        _parsedValues = QueryCollection.Empty;
                    }
                    else
                    {
                        _parsedValues = new QueryCollection(result);
                    }
                }
                return _parsedValues;
            }
            set
            {
                _parsedValues = value;
                if (_features != null)
                {
                    if (value == null)
                    {
                        _original = string.Empty;
                        HttpRequestFeature.QueryString = string.Empty;
                    }
                    else
                    {
                        _original = QueryString.Create(_parsedValues).ToString();
                        HttpRequestFeature.QueryString = _original;
                    }
                }
            }
        }
    }
}