// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HostFiltering;

internal static partial class LoggerExtensions
{
    [LoggerMessage(0, LogLevel.Debug, "Wildcard detected, all requests with hosts will be allowed.", EventName = "WildcardDetected")]
    public static partial void WildcardDetected(this ILogger logger);

    [LoggerMessage(1, LogLevel.Debug, "Allowed hosts: {Hosts}", EventName = "AllowedHosts", SkipEnabledCheck = true)]
    public static partial void AllowedHosts(this ILogger logger, string hosts);

    [LoggerMessage(2, LogLevel.Trace, "All hosts are allowed.", EventName = "AllHostsAllowed")]
    public static partial void AllHostsAllowed(this ILogger logger);

    [LoggerMessage(3, LogLevel.Information, "{Protocol} request rejected due to missing or empty host header.", EventName = "RequestRejectedMissingHost")]
    public static partial void RequestRejectedMissingHost(this ILogger logger, string protocol);

    [LoggerMessage(4, LogLevel.Debug, "{Protocol} request allowed with missing or empty host header.", EventName = "RequestAllowedMissingHost")]
    public static partial void RequestAllowedMissingHost(this ILogger logger, string protocol);

    [LoggerMessage(5, LogLevel.Trace, "The host '{Host}' matches an allowed host.", EventName = "AllowedHostMatched")]
    public static partial void AllowedHostMatched(this ILogger logger, string host);

    [LoggerMessage(6, LogLevel.Information, "The host '{Host}' does not match an allowed host.", EventName = "NoAllowedHostMatched")]
    public static partial void NoAllowedHostMatched(this ILogger logger, string host);

    [LoggerMessage(7, LogLevel.Debug, "Middleware configuration started with options: {Options}", EventName = "MiddlewareConfigurationStarted")]
    public static partial void MiddlewareConfigurationStarted(this ILogger logger, string options);
}
