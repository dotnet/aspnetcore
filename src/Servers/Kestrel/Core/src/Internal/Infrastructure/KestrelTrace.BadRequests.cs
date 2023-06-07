// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed partial class KestrelTrace : ILogger
{
    public void ConnectionBadRequest([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, AspNetCore.Http.BadHttpRequestException ex)
    {
        BadRequestsLog.ConnectionBadRequest(_badRequestsLogger, connectionId, ex.Message, ex);
    }

    public void RequestProcessingError([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, Exception ex)
    {
        BadRequestsLog.RequestProcessingError(_badRequestsLogger, connectionId, ex);
    }

    public void RequestBodyMinimumDataRateNotSatisfied([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string? traceIdentifier, double rate)
    {
        BadRequestsLog.RequestBodyMinimumDataRateNotSatisfied(_badRequestsLogger, connectionId, traceIdentifier, rate);
    }

    public void ResponseMinimumDataRateNotSatisfied([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string? traceIdentifier)
    {
        BadRequestsLog.ResponseMinimumDataRateNotSatisfied(_badRequestsLogger, connectionId, traceIdentifier);
    }

    public void PossibleInvalidHttpVersionDetected([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, HttpVersion expectedHttpVersion, HttpVersion detectedHttpVersion)
    {
        if (_generalLogger.IsEnabled(LogLevel.Debug))
        {
            BadRequestsLog.PossibleInvalidHttpVersionDetected(_badRequestsLogger, connectionId, HttpUtilities.VersionToString(expectedHttpVersion), HttpUtilities.VersionToString(detectedHttpVersion));
        }
    }

    private static partial class BadRequestsLog
    {
        [LoggerMessage(17, LogLevel.Debug, @"Connection id ""{ConnectionId}"" bad request data: ""{message}""", EventName = "ConnectionBadRequest")]
        public static partial void ConnectionBadRequest(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string message, Microsoft.AspNetCore.Http.BadHttpRequestException ex);

        [LoggerMessage(20, LogLevel.Debug, @"Connection id ""{ConnectionId}"" request processing ended abnormally.", EventName = "RequestProcessingError")]
        public static partial void RequestProcessingError(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, Exception ex);

        [LoggerMessage(27, LogLevel.Debug, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the request timed out because it was not sent by the client at a minimum of {Rate} bytes/second.", EventName = "RequestBodyMinimumDataRateNotSatisfied")]
        public static partial void RequestBodyMinimumDataRateNotSatisfied(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string? traceIdentifier, double rate);

        [LoggerMessage(28, LogLevel.Debug, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the connection was closed because the response was not read by the client at the specified minimum data rate.", EventName = "ResponseMinimumDataRateNotSatisfied")]
        public static partial void ResponseMinimumDataRateNotSatisfied(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string? traceIdentifier);

        [LoggerMessage(54, LogLevel.Debug, @"Connection id ""{ConnectionId}"": Invalid content received on connection. Possible incorrect HTTP version detected. Expected {ExpectedHttpVersion} but received {DetectedHttpVersion}.", EventName = "PossibleInvalidHttpVersionDetected", SkipEnabledCheck = true)]
        public static partial void PossibleInvalidHttpVersionDetected(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string expectedHttpVersion, string detectedHttpVersion);

        // Highest shared ID is 63. New consecutive IDs start at 64
    }
}
