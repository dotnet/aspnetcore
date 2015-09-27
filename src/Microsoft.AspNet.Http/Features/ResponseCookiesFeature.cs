// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Internal;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class ResponseCookiesFeature : IResponseCookiesFeature, IFeatureCache
    {
        private readonly IFeatureCollection _features;
        private int _cachedFeaturesRevision = -1;

        private IHttpResponseFeature _response;
        private IResponseCookies _cookiesCollection;

        public ResponseCookiesFeature(IFeatureCollection features)
        {
            _features = features;
        }

        void IFeatureCache.CheckFeaturesRevision()
        {
            if (_cachedFeaturesRevision != _features.Revision)
            {
                _response = null;
                _cachedFeaturesRevision = _features.Revision;
            }
        }

        private IHttpResponseFeature HttpResponseFeature
        {
            get { return FeatureHelpers.GetAndCache(this, _features, ref _response); }
        }

        public IResponseCookies Cookies
        {
            get
            {
                if (_cookiesCollection == null)
                {
                    var headers = HttpResponseFeature.Headers;
                    _cookiesCollection = new ResponseCookies(new HeaderDictionary(headers));
                }
                return _cookiesCollection;
            }
        }
    }
}