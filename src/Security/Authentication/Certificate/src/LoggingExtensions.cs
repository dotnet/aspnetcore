// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class LoggingExtensions
{
    [LoggerMessage(0, LogLevel.Debug, "No client certificate found.", EventName = "NoCertificate")]
    public static partial void NoCertificate(this ILogger logger);

    [LoggerMessage(3, LogLevel.Debug, "Not https, skipping certificate authentication.", EventName = "NotHttps")]
    public static partial void NotHttps(this ILogger logger);

    [LoggerMessage(1, LogLevel.Warning, "{CertificateType} certificate rejected, subject was {Subject}.", EventName = "CertificateRejected")]
    public static partial void CertificateRejected(this ILogger logger, string certificateType, string subject);

    [LoggerMessage(2, LogLevel.Warning, "Certificate validation failed, subject was {Subject}. {ChainErrors}", EventName = "CertificateFailedValidation")]
    public static partial void CertificateFailedValidation(this ILogger logger, string subject, IList<string> chainErrors);
}
