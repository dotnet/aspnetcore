// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.RequestDecompression;

internal static partial class RequestDecompressionLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Trace, "The Content-Encoding header is missing or empty. Skipping request decompression.", EventName = "NoContentEncoding")]
    public static partial void NoContentEncoding(this ILogger logger);

    [LoggerMessage(2, LogLevel.Debug, "Request decompression is not supported for multiple Content-Encodings.", EventName = "MultipleContentEncodingsSpecified")]
    public static partial void MultipleContentEncodingsSpecified(this ILogger logger);

    [LoggerMessage(3, LogLevel.Debug, "No matching request decompression provider found.", EventName = "NoDecompressionProvider")]
    public static partial void NoDecompressionProvider(this ILogger logger);

    [LoggerMessage(4, LogLevel.Debug, "The request will be decompressed with '{Provider}'.", EventName = "DecompressingWith")]
    public static partial void DecompressingWith(this ILogger logger, string provider);

    [LoggerMessage(5, LogLevel.Warning, $"A request body size limit could not be applied. This server does not support the {nameof(IHttpMaxRequestBodySizeFeature)}.", EventName = "FeatureNotFound")]
    public static partial void FeatureNotFound(this ILogger logger);

    [LoggerMessage(6, LogLevel.Warning, $"A request body size limit could not be applied. The {nameof(IHttpMaxRequestBodySizeFeature)} for the server is read-only.", EventName = "FeatureIsReadOnly")]
    public static partial void FeatureIsReadOnly(this ILogger logger);

    [LoggerMessage(7, LogLevel.Debug, "The maximum request body size has been set to {RequestSize}.", EventName = "MaxRequestBodySizeSet")]
    public static partial void MaxRequestBodySizeSet(this ILogger logger, long? requestSize);
}
