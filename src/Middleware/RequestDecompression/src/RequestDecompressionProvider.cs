// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                new BrotliDecompressionProvider(),
                new DeflateDecompressionProvider(),
                new GZipDecompressionProvider()
            };
        }

        for (var i = 0; i < _providers.Length; i++)
        {
            if (_providers[i] is DecompressionProviderFactory factory)
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
            _logger.NoContentEncoding();
            return null;
        }

        if (encodings.Count > 1)
        {
            _logger.MultipleContentEncodingsSpecified();
            return null;
        }

        string encodingName = encodings!;

        var selectedProvider =
            _providers.FirstOrDefault(x =>
                StringSegment.Equals(x.EncodingName, encodingName, StringComparison.OrdinalIgnoreCase));

        if (selectedProvider == null)
        {
            _logger.NoDecompressionProvider();
            return null;
        }

        _logger.DecompressingWith(selectedProvider.EncodingName);
        return selectedProvider;
    }
}
