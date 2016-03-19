// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Http.Features.Internal
{
    /// <summary>
    /// Default implementation of <see cref="IResponseCookiesFeature"/>.
    /// </summary>
    public class ResponseCookiesFeature : IResponseCookiesFeature
    {
        // Object pool will be null only in test scenarios e.g. if code news up a DefaultHttpContext.
        private readonly ObjectPool<StringBuilder> _builderPool;

        private FeatureReferences<IHttpResponseFeature> _features;
        private IResponseCookies _cookiesCollection;

        /// <summary>
        /// Initializes a new <see cref="ResponseCookiesFeature"/> instance.
        /// </summary>
        /// <param name="features">
        /// <see cref="IFeatureCollection"/> containing all defined features, including this
        /// <see cref="IResponseCookiesFeature"/> and the <see cref="IHttpResponseFeature"/>.
        /// </param>
        public ResponseCookiesFeature(IFeatureCollection features)
            : this(features, builderPool: null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ResponseCookiesFeature"/> instance.
        /// </summary>
        /// <param name="features">
        /// <see cref="IFeatureCollection"/> containing all defined features, including this
        /// <see cref="IResponseCookiesFeature"/> and the <see cref="IHttpResponseFeature"/>.
        /// </param>
        /// <param name="builderPool">The <see cref="ObjectPool{T}"/>, if available.</param>
        public ResponseCookiesFeature(IFeatureCollection features, ObjectPool<StringBuilder> builderPool)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            _features = new FeatureReferences<IHttpResponseFeature>(features);
            _builderPool = builderPool;
        }

        private IHttpResponseFeature HttpResponseFeature => _features.Fetch(ref _features.Cache, f => null);

        /// <inheritdoc />
        public IResponseCookies Cookies
        {
            get
            {
                if (_cookiesCollection == null)
                {
                    var headers = HttpResponseFeature.Headers;
                    _cookiesCollection = new ResponseCookies(headers, _builderPool);
                }

                return _cookiesCollection;
            }
        }
    }
}