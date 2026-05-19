// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging;

internal static partial class HttpLoggingExtensions
{
    public static void RequestLog(this ILogger logger, HttpLog requestLog) => logger.Log(
        LogLevel.Information,
        new EventId(1, "RequestLog"),
        requestLog,
        exception: null,
        formatter: HttpLog.Callback);
    public static void ResponseLog(this ILogger logger, HttpLog responseLog) => logger.Log(
        LogLevel.Information,
        new EventId(2, "ResponseLog"),
        responseLog,
        exception: null,
        formatter: HttpLog.Callback);

    [LoggerMessage(3, LogLevel.Information, "RequestBody: {Body}{Status}", EventName = "RequestBody")]
    public static partial void RequestBody(this ILogger logger, string body, string status);

    [LoggerMessage(4, LogLevel.Information, "ResponseBody: {Body}", EventName = "ResponseBody")]
    public static partial void ResponseBody(this ILogger logger, string body);

    [LoggerMessage(5, LogLevel.Debug, "Decode failure while converting body.", EventName = "DecodeFailure")]
    public static partial void DecodeFailure(this ILogger logger, Exception ex);

    [LoggerMessage(6, LogLevel.Debug, "Unrecognized Content-Type for {Name} body.", EventName = "UnrecognizedMediaType")]
    public static partial void UnrecognizedMediaType(this ILogger logger, string name);

    [LoggerMessage(7, LogLevel.Debug, "No Content-Type header for {Name} body.", EventName = "NoMediaType")]
    public static partial void NoMediaType(this ILogger logger, string name);

    [LoggerMessage(8, LogLevel.Information, "Duration: {Duration}ms", EventName = "Duration")]
    public static partial void Duration(this ILogger logger, double duration);

    public static void RequestResponseLog(this ILogger logger, HttpLog log) => logger.Log(
        LogLevel.Information,
        new EventId(9, "RequestResponseLog"),
        log,
        exception: null,
        formatter: HttpLog.Callback);
}
