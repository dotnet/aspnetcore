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
    private readonly IReadOnlyDictionary<string, IDecompressionProvider> _providers;
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

        var registeredProviders = requestDecompressionOptions.Providers.ToArray();
        if (registeredProviders.Length == 0)
        {
            registeredProviders = new IDecompressionProvider[]
            {
                new BrotliDecompressionProvider(),
                new DeflateDecompressionProvider(),
                new GZipDecompressionProvider()
            };
        }

        var providers = new Dictionary<string, IDecompressionProvider>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in registeredProviders)
        {
            if (provider is DecompressionProviderFactory factory)
            {
                var providerInstance = factory.CreateInstance(services);
                providers[providerInstance.EncodingName] = providerInstance;
            }
            else
            {
                providers[provider.EncodingName] = provider;
            }
        }

        _providers = providers;
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

        if (_providers.TryGetValue(encodingName, out var matchingProvider))
        {
            _logger.DecompressingWith(matchingProvider.EncodingName);
            return matchingProvider;
        }

        _logger.NoDecompressionProvider();
        return null;
    }
}
