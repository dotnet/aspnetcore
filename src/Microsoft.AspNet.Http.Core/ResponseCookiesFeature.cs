// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Interfaces;
using Microsoft.AspNet.Http.Core.Collections;
using Microsoft.AspNet.Http.Core.Infrastructure;

namespace Microsoft.AspNet.Http.Core
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