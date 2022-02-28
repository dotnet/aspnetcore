// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <inheritdoc />
internal sealed partial class DefaultRequestDecompressionProvider : IRequestDecompressionProvider
{
    private readonly ILogger _logger;
    private readonly IDictionary<string, IDecompressionProvider> _providers;

    public DefaultRequestDecompressionProvider(
        ILogger<DefaultRequestDecompressionProvider> logger,
        IOptions<RequestDecompressionOptions> options)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _logger = logger;
        _providers = options.Value.DecompressionProviders;
    }

    /// <inheritdoc />
    public IDecompressionProvider? GetDecompressionProvider(HttpContext context)
    {
        var encodings = context.Request.Headers.ContentEncoding;

        if (StringValues.IsNullOrEmpty(encodings))
        {
            Log.NoContentEncoding(_logger);
            return null;
        }

        if (encodings.Count > 1)
        {
            Log.MultipleContentEncodingsSpecified(_logger);
            return null;
        }

        string encodingName = encodings!;

        if (_providers.TryGetValue(encodingName, out var matchingProvider))
        {
            context.Request.Headers.Remove(HeaderNames.ContentEncoding);

            Log.DecompressingWith(_logger, encodingName.ToLowerInvariant());
            return matchingProvider;
        }

        Log.NoDecompressionProvider(_logger);
        return null;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Trace, "The Content-Encoding header is empty or not specified. Skipping request decompression.", EventName = "NoContentEncoding")]
        public static partial void NoContentEncoding(ILogger logger);

        [LoggerMessage(2, LogLevel.Debug, "Request decompression is not supported for multiple Content-Encodings.", EventName = "MultipleContentEncodingsSpecified")]
        public static partial void MultipleContentEncodingsSpecified(ILogger logger);

        [LoggerMessage(3, LogLevel.Debug, "No matching request decompression provider found.", EventName = "NoDecompressionProvider")]
        public static partial void NoDecompressionProvider(ILogger logger);

        [LoggerMessage(4, LogLevel.Debug, "The request will be decompressed with '{ContentEncoding}'.", EventName = "DecompressingWith")]
        public static partial void DecompressingWith(ILogger logger, string contentEncoding);
    }
}
