// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <inheritdoc />
    public class ResponseCompressionProvider : IResponseCompressionProvider
    {
        private readonly ICompressionProvider[] _providers;
        private readonly HashSet<string> _mimeTypes;
        private readonly bool _enableForHttps;

        /// <summary>
        /// If no compression providers are specified then GZip is used by default.
        /// </summary>
        /// <param name="services">Services to use when instantiating compression providers.</param>
        /// <param name="options"></param>
        public ResponseCompressionProvider(IServiceProvider services, IOptions<ResponseCompressionOptions> options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _providers = options.Value.Providers.ToArray();
            if (_providers.Length == 0)
            {
                // Use the factory so it can resolve IOptions<GzipCompressionProviderOptions> from DI.
                _providers = new ICompressionProvider[]
                {
#if NETCOREAPP2_1
                    new CompressionProviderFactory(typeof(BrotliCompressionProvider)),
#elif NET461 || NETSTANDARD2_0
                    // Brotli is only supported in .NET Core 2.1+
#else
#error Target frameworks need to be updated.
#endif
                    new CompressionProviderFactory(typeof(GzipCompressionProvider)),
                };
            }
            for (var i = 0; i < _providers.Length; i++)
            {
                var factory = _providers[i] as CompressionProviderFactory;
                if (factory != null)
                {
                    _providers[i] = factory.CreateInstance(services);
                }
            }

            var mimeTypes = options.Value.MimeTypes;
            if (mimeTypes == null || !mimeTypes.Any())
            {
                mimeTypes = ResponseCompressionDefaults.MimeTypes;
            }
            _mimeTypes = new HashSet<string>(mimeTypes, StringComparer.OrdinalIgnoreCase);

            _enableForHttps = options.Value.EnableForHttps;
        }

        /// <inheritdoc />
        public virtual ICompressionProvider GetCompressionProvider(HttpContext context)
        {
            // e.g. Accept-Encoding: gzip, deflate, sdch
            var accept = context.Request.Headers[HeaderNames.AcceptEncoding];

            if (StringValues.IsNullOrEmpty(accept))
            {
                return null;
            }

            if (StringWithQualityHeaderValue.TryParseList(accept, out var encodings))
            {
                if (encodings.Count == 0)
                {
                    return null;
                }

                var candidates = new HashSet<ProviderCandidate>();

                foreach (var encoding in encodings)
                {
                    var encodingName = encoding.Value;
                    var quality = encoding.Quality.GetValueOrDefault(1);

                    if (quality < double.Epsilon)
                    {
                        continue;
                    }

                    for (int i = 0; i < _providers.Length; i++)
                    {
                        var provider = _providers[i];

                        if (StringSegment.Equals(provider.EncodingName, encodingName, StringComparison.OrdinalIgnoreCase))
                        {
                            candidates.Add(new ProviderCandidate(provider.EncodingName, quality, i, provider));
                        }
                    }

                    // Uncommon but valid options
                    if (StringSegment.Equals("*", encodingName, StringComparison.Ordinal))
                    {
                        for (int i = 0; i < _providers.Length; i++)
                        {
                            var provider = _providers[i];
                            
                            // Any provider is a candidate.
                            candidates.Add(new ProviderCandidate(provider.EncodingName, quality, i, provider));
                        }

                        break;
                    }

                    if (StringSegment.Equals("identity", encodingName, StringComparison.OrdinalIgnoreCase))
                    {
                        // We add 'identity' to the list of "candidates" with a very low priority and no provider.
                        // This will allow it to be ordered based on its quality (and priority) later in the method.
                        candidates.Add(new ProviderCandidate(encodingName.Value, quality, priority: int.MaxValue, provider: null));
                    }
                }

                if (candidates.Count <= 1)
                {
                    return candidates.ElementAtOrDefault(0).Provider;
                }

                var accepted = candidates
                    .OrderByDescending(x => x.Quality)
                    .ThenBy(x => x.Priority)
                    .First();

                return accepted.Provider;
            }

            return null;
        }

        /// <inheritdoc />
        public virtual bool ShouldCompressResponse(HttpContext context)
        {
            if (context.Response.Headers.ContainsKey(HeaderNames.ContentRange))
            {
                return false;
            }

            if (context.Response.Headers.ContainsKey(HeaderNames.ContentEncoding))
            {
                return false;
            }

            var mimeType = context.Response.ContentType;

            if (string.IsNullOrEmpty(mimeType))
            {
                return false;
            }

            var separator = mimeType.IndexOf(';');
            if (separator >= 0)
            {
                // Remove the content-type optional parameters
                mimeType = mimeType.Substring(0, separator);
                mimeType = mimeType.Trim();
            }

            // TODO PERF: StringSegments?
            return _mimeTypes.Contains(mimeType);
        }

        /// <inheritdoc />
        public bool CheckRequestAcceptsCompression(HttpContext context)
        {
            if (context.Request.IsHttps && !_enableForHttps)
            {
                return false;
            }
            return !string.IsNullOrEmpty(context.Request.Headers[HeaderNames.AcceptEncoding]);
        }

        private readonly struct ProviderCandidate : IEquatable<ProviderCandidate>
        {
            public ProviderCandidate(string encodingName, double quality, int priority, ICompressionProvider provider)
            {
                EncodingName = encodingName;
                Quality = quality;
                Priority = priority;
                Provider = provider;
            }

            public string EncodingName { get; }

            public double Quality { get; }

            public int Priority { get; }

            public ICompressionProvider Provider { get; }

            public bool Equals(ProviderCandidate other)
            {
                return string.Equals(EncodingName, other.EncodingName, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                return obj is ProviderCandidate candidate && Equals(candidate);
            }

            public override int GetHashCode()
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(EncodingName);
            }
        }
    }
}
