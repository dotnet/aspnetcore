// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Default implementation of <see cref="IResponseCookiesFeature"/>.
    /// </summary>
    public class ResponseCookiesFeature : IResponseCookiesFeature
    {
        // Lambda hoisted to static readonly field to improve inlining https://github.com/dotnet/roslyn/issues/13624
        private readonly static Func<IFeatureCollection, IHttpResponseFeature?> _nullResponseFeature = f => null;

        private readonly IFeatureCollection _features;
        private IResponseCookies? _cookiesCollection;

        /// <summary>
        /// Initializes a new <see cref="ResponseCookiesFeature"/> instance.
        /// </summary>
        /// <param name="features">
        /// <see cref="IFeatureCollection"/> containing all defined features, including this
        /// <see cref="IResponseCookiesFeature"/> and the <see cref="IHttpResponseFeature"/>.
        /// </param>
        public ResponseCookiesFeature(IFeatureCollection features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        /// <summary>
        /// Initializes a new <see cref="ResponseCookiesFeature"/> instance.
        /// </summary>
        /// <param name="features">
        /// <see cref="IFeatureCollection"/> containing all defined features, including this
        /// <see cref="IResponseCookiesFeature"/> and the <see cref="IHttpResponseFeature"/>.
        /// </param>
        /// <param name="builderPool">The <see cref="ObjectPool{T}"/>, if available.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version.")]
        public ResponseCookiesFeature(IFeatureCollection features, ObjectPool<StringBuilder>? builderPool)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        /// <inheritdoc />
        public IResponseCookies Cookies
        {
            get
            {
                if (_cookiesCollection == null)
                {
                    _cookiesCollection = new ResponseCookies(_features);
                }

                return _cookiesCollection;
            }
        }
    }
}
