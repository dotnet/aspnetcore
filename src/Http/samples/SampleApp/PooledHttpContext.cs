// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Internal;

namespace SampleApp
{
    public class PooledHttpContext : DefaultHttpContext
    {
        DefaultHttpRequest _pooledHttpRequest;
        DefaultHttpResponse _pooledHttpResponse;

        public PooledHttpContext(IFeatureCollection featureCollection) :
            base(featureCollection)
        {
        }

        protected override HttpRequest InitializeHttpRequest()
        {
            if (_pooledHttpRequest != null)
            {
                _pooledHttpRequest.Initialize(this);
                return _pooledHttpRequest;
            }

            return new DefaultHttpRequest(this);
        }

        protected override void UninitializeHttpRequest(HttpRequest instance)
        {
            _pooledHttpRequest = instance as DefaultHttpRequest;
            _pooledHttpRequest?.Uninitialize();
        }

        protected override HttpResponse InitializeHttpResponse()
        {
            if (_pooledHttpResponse != null)
            {
                _pooledHttpResponse.Initialize(this);
                return _pooledHttpResponse;
            }

            return new DefaultHttpResponse(this);
        }

        protected override void UninitializeHttpResponse(HttpResponse instance)
        {
            _pooledHttpResponse = instance as DefaultHttpResponse;
            _pooledHttpResponse?.Uninitialize();
        }
    }
}