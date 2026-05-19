// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class NegotiateLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Incomplete Negotiate handshake, sending an additional 401 Negotiate challenge.", EventName = "IncompleteNegotiateChallenge")]
    public static partial void IncompleteNegotiateChallenge(this ILogger logger);

    [LoggerMessage(2, LogLevel.Debug, "Completed Negotiate authentication.", EventName = "NegotiateComplete")]
    public static partial void NegotiateComplete(this ILogger logger);

    [LoggerMessage(3, LogLevel.Debug, "Enabling credential persistence for a complete Kerberos handshake.", EventName = "EnablingCredentialPersistence")]
    public static partial void EnablingCredentialPersistence(this ILogger logger);

    [LoggerMessage(4, LogLevel.Debug, "Disabling credential persistence for a complete {protocol} handshake.", EventName = "DisablingCredentialPersistence")]
    public static partial void DisablingCredentialPersistence(this ILogger logger, string protocol);

    [LoggerMessage(5, LogLevel.Error, "An exception occurred while processing the authentication request.", EventName = "ExceptionProcessingAuth")]
    public static partial void ExceptionProcessingAuth(this ILogger logger, Exception ex);

    [LoggerMessage(6, LogLevel.Debug, "Challenged 401 Negotiate.", EventName = "ChallengeNegotiate")]
    public static partial void ChallengeNegotiate(this ILogger logger);

    [LoggerMessage(7, LogLevel.Debug, "Negotiate data received for an already authenticated connection, Re-authenticating.", EventName = "Reauthenticating")]
    public static partial void Reauthenticating(this ILogger logger);

    [LoggerMessage(8, LogLevel.Information, "Deferring to the server's implementation of Windows Authentication.", EventName = "Deferring")]
    public static partial void Deferring(this ILogger logger);

    [LoggerMessage(9, LogLevel.Debug, "There was a problem with the users credentials.", EventName = "CredentialError")]
    public static partial void CredentialError(this ILogger logger, Exception ex);

    [LoggerMessage(10, LogLevel.Debug, "The users authentication request was invalid.", EventName = "ClientError")]
    public static partial void ClientError(this ILogger logger, Exception ex);

    [LoggerMessage(11, LogLevel.Debug, "Negotiate error code: {error}.", EventName = "NegotiateError")]
    public static partial void NegotiateError(this ILogger logger, string error);

    [LoggerMessage(12, LogLevel.Debug, "Negotiate is not supported with {protocol}.", EventName = "ProtocolNotSupported")]
    public static partial void ProtocolNotSupported(this ILogger logger, string protocol);
}
