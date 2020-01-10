// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly HashSet<string> _excludedMimeTypes;
        private readonly bool _enableForHttps;
        private readonly ILogger _logger;

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

            var responseCompressionOptions = options.Value;

            _providers = responseCompressionOptions.Providers.ToArray();
            if (_providers.Length == 0)
            {
                // Use the factory so it can resolve IOptions<GzipCompressionProviderOptions> from DI.
                _providers = new ICompressionProvider[]
                {
                    new CompressionProviderFactory(typeof(BrotliCompressionProvider)),
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

            var mimeTypes = responseCompressionOptions.MimeTypes;
            if (mimeTypes == null || !mimeTypes.Any())
            {
                mimeTypes = ResponseCompressionDefaults.MimeTypes;
            }

            _mimeTypes = new HashSet<string>(mimeTypes, StringComparer.OrdinalIgnoreCase);

            _excludedMimeTypes = new HashSet<string>(
                responseCompressionOptions.ExcludedMimeTypes ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase
            );

            _enableForHttps = responseCompressionOptions.EnableForHttps;

            _logger = services.GetRequiredService<ILogger<ResponseCompressionProvider>>();
        }

        /// <inheritdoc />
        public virtual ICompressionProvider GetCompressionProvider(HttpContext context)
        {
            // e.g. Accept-Encoding: gzip, deflate, sdch
            var accept = context.Request.Headers[HeaderNames.AcceptEncoding];

            // Note this is already checked in CheckRequestAcceptsCompression which _should_ prevent any of these other methods from being called.
            if (StringValues.IsNullOrEmpty(accept))
            {
                Debug.Assert(false, "Duplicate check failed.");
                _logger.NoAcceptEncoding();
                return null;
            }

            if (!StringWithQualityHeaderValue.TryParseList(accept, out var encodings) || encodings.Count == 0)
            {
                _logger.NoAcceptEncoding();
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

            ICompressionProvider selectedProvider = null;
            if (candidates.Count <= 1)
            {
                selectedProvider = candidates.FirstOrDefault().Provider;
            }
            else
            {
                selectedProvider = candidates
                    .OrderByDescending(x => x.Quality)
                    .ThenBy(x => x.Priority)
                    .First().Provider;
            }

            if (selectedProvider == null)
            {
                // "identity" would match as a candidate but not have a provider implementation
                _logger.NoCompressionProvider();
                return null;
            }

            _logger.CompressingWith(selectedProvider.EncodingName);
            return selectedProvider;
        }

        /// <inheritdoc />
        public virtual bool ShouldCompressResponse(HttpContext context)
        {
            var httpsMode = context.Features.Get<IHttpsCompressionFeature>()?.Mode ?? HttpsCompressionMode.Default;

            // Check if the app has opted into or out of compression over HTTPS
            if (context.Request.IsHttps
                && (httpsMode == HttpsCompressionMode.DoNotCompress
                    || !(_enableForHttps || httpsMode == HttpsCompressionMode.Compress)))
            {
                _logger.NoCompressionForHttps();
                return false;
            }

            if (context.Response.Headers.ContainsKey(HeaderNames.ContentRange))
            {
                _logger.NoCompressionDueToHeader(HeaderNames.ContentRange);
                return false;
            }

            if (context.Response.Headers.ContainsKey(HeaderNames.ContentEncoding))
            {
                _logger.NoCompressionDueToHeader(HeaderNames.ContentEncoding);
                return false;
            }

            var mimeType = context.Response.ContentType;

            if (string.IsNullOrEmpty(mimeType))
            {
                _logger.NoCompressionForContentType(mimeType);
                return false;
            }

            var separator = mimeType.IndexOf(';');
            if (separator >= 0)
            {
                // Remove the content-type optional parameters
                mimeType = mimeType.Substring(0, separator);
                mimeType = mimeType.Trim();
            }

            var shouldCompress = ShouldCompressExact(mimeType) //check exact match type/subtype
                ?? ShouldCompressPartial(mimeType) //check partial match type/*
                ?? _mimeTypes.Contains("*/*"); //check wildcard */*

            if (shouldCompress)
            {
                _logger.ShouldCompressResponse();  // Trace, there will be more logs
                return true;
            }

            _logger.NoCompressionForContentType(mimeType);
            return false;
        }

        /// <inheritdoc />
        public bool CheckRequestAcceptsCompression(HttpContext context)
        {
            if (string.IsNullOrEmpty(context.Request.Headers[HeaderNames.AcceptEncoding]))
            {
                _logger.NoAcceptEncoding();
                return false;
            }

            _logger.RequestAcceptsCompression(); // Trace, there will be more logs
            return true;
        }

        private bool? ShouldCompressExact(string mimeType)
        {
            //Check excluded MIME types first, then included
            if (_excludedMimeTypes.Contains(mimeType))
            {
                return false;
            }

            if (_mimeTypes.Contains(mimeType))
            {
                return true;
            }

            return null;
        }

        private bool? ShouldCompressPartial(string mimeType)
        {
            int? slashPos = mimeType?.IndexOf('/');

            if (slashPos >= 0)
            {
                string partialMimeType = mimeType.Substring(0, slashPos.Value) + "/*";
                return ShouldCompressExact(partialMimeType);
            }

            return null;
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
