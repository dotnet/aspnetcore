// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Internal;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class ResponseCookiesFeature : IResponseCookiesFeature
    {
        private FeatureReferences<IHttpResponseFeature> _features;
        private IResponseCookies _cookiesCollection;

        public ResponseCookiesFeature(IFeatureCollection features)
        {
            _features = new FeatureReferences<IHttpResponseFeature>(features);
        }

        private IHttpResponseFeature HttpResponseFeature =>
            _features.Fetch(ref _features.Cache, f => null);

        public IResponseCookies Cookies
        {
            get
            {
                if (_cookiesCollection == null)
                {
                    var headers = HttpResponseFeature.Headers;
                    _cookiesCollection = new ResponseCookies(headers);
                }
                return _cookiesCollection;
            }
        }
    }
}