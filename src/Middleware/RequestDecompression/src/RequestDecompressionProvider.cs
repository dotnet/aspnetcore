// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <inheritdoc />
internal sealed class RequestDecompressionProvider : IRequestDecompressionProvider
{
    private readonly IDecompressionProvider[] _providers;
    private readonly ILogger _logger;

    /// <summary>
    /// If no decompression providers are specified then all default providers will be registered.
    /// </summary>
    /// <param name="services">Services to use when instantiating decompression providers.</param>
    /// <param name="logger">An instance of <see cref="ILogger"/>.</param>
    /// <param name="options">The options for this instance.</param>
    public RequestDecompressionProvider(
        IServiceProvider services,
        ILogger<RequestDecompressionProvider> logger,
        IOptions<RequestDecompressionOptions> options)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _logger = logger;

        var requestDecompressionOptions = options.Value;

        _providers = requestDecompressionOptions.Providers.ToArray();
        if (_providers.Length == 0)
        {
            _providers = new IDecompressionProvider[]
            {
                new DecompressionProviderFactory(typeof(BrotliDecompressionProvider)),
                new DecompressionProviderFactory(typeof(DeflateDecompressionProvider)),
                new DecompressionProviderFactory(typeof(GzipDecompressionProvider))
            };
        }

        for (var i = 0; i < _providers.Length; i++)
        {
            var factory = _providers[i] as DecompressionProviderFactory;
            if (factory != null)
            {
                _providers[i] = factory.CreateInstance(services);
            }
        }
    }

    /// <inheritdoc />
    public IDecompressionProvider? GetDecompressionProvider(HttpContext context)
    {
        // e.g. Content-Encoding: br, deflate, gzip
        var encodings = context.Request.Headers.ContentEncoding;

        if (StringValues.IsNullOrEmpty(encodings))
        {
            Debug.Assert(false, "Duplicate check failed.");
            _logger.NoContentEncoding();
            return null;
        }

        if (encodings.Count > 1)
        {
            Debug.Assert(false, "Duplicate check failed.");
            _logger.MultipleContentEncodingsSpecified();
            return null;
        }

        var encodingName = encodings.Single();

        var selectedProvider =
            _providers.FirstOrDefault(x =>
                StringSegment.Equals(x.EncodingName, encodingName, StringComparison.OrdinalIgnoreCase));

        if (selectedProvider == null)
        {
            Debug.Assert(false, "Duplicate check failed.");
            _logger.NoDecompressionProvider();
            return null;
        }

        _logger.DecompressingWith(selectedProvider.EncodingName);
        return selectedProvider;
    }

    /// <inheritdoc />
    public bool ShouldDecompressRequest(HttpContext context)
    {
        var encodings = context.Request.Headers.ContentEncoding;

        if (StringValues.IsNullOrEmpty(encodings))
        {
            _logger.NoContentEncoding();
            return false;
        }

        _logger.ContentEncodingSpecified();
        return true;
    }

    /// <inheritdoc />
    public bool IsContentEncodingSupported(HttpContext context)
    {
        var encodings = context.Request.Headers.ContentEncoding;

        if (StringValues.IsNullOrEmpty(encodings))
        {
            Debug.Assert(false, "Duplicate check failed.");
            _logger.NoContentEncoding();
            return false;
        }

        if (encodings.Count > 1)
        {
            _logger.MultipleContentEncodingsSpecified();
            return false;
        }

        var encoding = encodings.Single();
        var supportedEncodings = _providers.Select(x => x.EncodingName);

        if (supportedEncodings.Any(x => string.Equals(x, encoding, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.ContentEncodingSupported(encoding);
            return true;
        }

        _logger.ContentEncodingUnsupported(encoding);
        return false;
    }
}
