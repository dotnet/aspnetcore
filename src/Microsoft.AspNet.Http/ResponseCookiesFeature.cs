// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Collections;
using Microsoft.AspNet.Http.Infrastructure;

namespace Microsoft.AspNet.Http
{
    public class ResponseCookiesFeature : IResponseCookiesFeature
    {
        private readonly IFeatureCollection _features;
        private readonly FeatureReference<IHttpResponseFeature> _request = FeatureReference<IHttpResponseFeature>.Default;
        private IResponseCookies _cookiesCollection;

        public ResponseCookiesFeature(IFeatureCollection features)
        {
            _features = features;
        }

        public IResponseCookies Cookies
        {
            get
            {
                if (_cookiesCollection == null)
                {
                    var headers = _request.Fetch(_features).Headers;
                    _cookiesCollection = new ResponseCookies(new HeaderDictionary(headers));
                }

                return _cookiesCollection;
            }
        }
    }
}