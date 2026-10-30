// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCompression;

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
    /// <param name="options">The options for this instance.</param>
    public ResponseCompressionProvider(IServiceProvider services, IOptions<ResponseCompressionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        var responseCompressionOptions = options.Value;

        _providers = responseCompressionOptions.Providers.ToArray();
        if (_providers.Length == 0)
        {
            // Use the factory so the default compression providers and their options can be resolved from DI.
            _providers = new ICompressionProvider[]
            {
                new CompressionProviderFactory(typeof(ZstandardCompressionProvider)),
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
    public virtual ICompressionProvider? GetCompressionProvider(HttpContext context)
    {
        // e.g. Accept-Encoding: gzip, deflate, sdch
        var accept = context.Request.Headers.AcceptEncoding;

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

        // At most one candidate per distinct encoding name: one per provider plus "identity".
        var maxCandidates = _providers.Length + 1;
        var candidates = new CandidateSelector(maxCandidates <= 32 ? stackalloc int[32] : new int[maxCandidates], _providers);

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
                    candidates.Add(i, quality);
                }
            }

            // Uncommon but valid options
            if (StringSegment.Equals("*", encodingName, StringComparison.Ordinal))
            {
                for (int i = 0; i < _providers.Length; i++)
                {
                    // Any provider is a candidate.
                    candidates.Add(i, quality);
                }

                break;
            }

            if (StringSegment.Equals("identity", encodingName, StringComparison.OrdinalIgnoreCase))
            {
                // We add 'identity' to the list of "candidates" with a very low priority and no provider.
                // This will allow it to be ordered based on its quality (and priority) later in the method.
                candidates.Add(CandidateSelector.IdentityIndex, quality);
            }
        }

        var selectedProvider = candidates.SelectedProvider;

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
            _logger.NoCompressionForContentType(mimeType ?? "(null)");
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
        if (string.IsNullOrEmpty(context.Request.Headers.AcceptEncoding))
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

    private bool? ShouldCompressPartial(string? mimeType)
    {
        var slashPos = mimeType?.IndexOf('/');

        if (slashPos >= 0)
        {
            var partialMimeType = string.Concat(mimeType!.AsSpan(0, slashPos.Value), "/*");
            return ShouldCompressExact(partialMimeType);
        }

        return null;
    }

    // Selects the candidate with the highest quality, preferring the lowest priority (provider order, then "identity").
    // Only the first occurrence of each encoding name (case-insensitive) is considered.
    private ref struct CandidateSelector
    {
        public const int IdentityIndex = -1;

        private readonly Span<int> _seen;
        private readonly ICompressionProvider[] _providers;
        private int _seenCount;
        private bool _hasSelection;
        private double _selectedQuality;
        private int _selectedPriority;

        public CandidateSelector(Span<int> seen, ICompressionProvider[] providers)
        {
            _seen = seen;
            _providers = providers;
        }

        public ICompressionProvider? SelectedProvider { get; private set; }

        public void Add(int index, double quality)
        {
            var encodingName = GetEncodingName(index);
            for (var i = 0; i < _seenCount; i++)
            {
                if (string.Equals(GetEncodingName(_seen[i]), encodingName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            _seen[_seenCount++] = index;

            var priority = index == IdentityIndex ? int.MaxValue : index;
            if (!_hasSelection || quality > _selectedQuality || (quality == _selectedQuality && priority < _selectedPriority))
            {
                _hasSelection = true;
                _selectedQuality = quality;
                _selectedPriority = priority;
                SelectedProvider = index == IdentityIndex ? null : _providers[index];
            }
        }

        private readonly string GetEncodingName(int index)
            => index == IdentityIndex ? "identity" : _providers[index].EncodingName;
    }
}
