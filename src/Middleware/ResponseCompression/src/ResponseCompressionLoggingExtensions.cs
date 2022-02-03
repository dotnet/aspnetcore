// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.ResponseCompression;

internal static partial class ResponseCompressionLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "No response compression available, the Accept-Encoding header is missing or invalid.", EventName = "NoAcceptEncoding")]
    public static partial void NoAcceptEncoding(this ILogger logger);

    [LoggerMessage(2, LogLevel.Debug, "No response compression available for HTTPS requests. See ResponseCompressionOptions.EnableForHttps.", EventName = "NoCompressionForHttps")]
    public static partial void NoCompressionForHttps(this ILogger logger);

    [LoggerMessage(3, LogLevel.Trace, "This request accepts compression.", EventName = "RequestAcceptsCompression")]
    public static partial void RequestAcceptsCompression(this ILogger logger);

    [LoggerMessage(4, LogLevel.Debug, "Response compression disabled due to the {header} header.", EventName = "NoCompressionDueToHeader")]
    public static partial void NoCompressionDueToHeader(this ILogger logger, string header);

    [LoggerMessage(5, LogLevel.Debug, "Response compression is not enabled for the Content-Type '{header}'.", EventName = "NoCompressionForContentType")]
    public static partial void NoCompressionForContentType(this ILogger logger, string header);

    [LoggerMessage(6, LogLevel.Trace, "Response compression is available for this Content-Type.", EventName = "ShouldCompressResponse")]
    public static partial void ShouldCompressResponse(this ILogger logger);

    [LoggerMessage(7, LogLevel.Debug, "No matching response compression provider found.", EventName = "NoCompressionProvider")]
    public static partial void NoCompressionProvider(this ILogger logger);

    [LoggerMessage(8, LogLevel.Debug, "The response will be compressed with '{provider}'.", EventName = "CompressWith")]
    public static partial void CompressingWith(this ILogger logger, string provider);
}
