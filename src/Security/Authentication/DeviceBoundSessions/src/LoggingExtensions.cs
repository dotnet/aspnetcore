// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Source-generated log messages for Device Bound Session Credentials. Messages intentionally avoid
/// logging sensitive material (challenge values, claim values); the opaque session identifier is used
/// only as a correlation key.
/// </summary>
internal static partial class DeviceBoundSessionsLoggingExtensions
{
    // Refresh challenge validation (DeviceBoundSessionChallengeProtector).

    [LoggerMessage(1, LogLevel.Debug, "DBSC refresh challenge for session {SessionId} could not be decrypted; it is expired, tampered, or was minted for a different flow or version.", EventName = "RefreshChallengeUndecryptable")]
    public static partial void RefreshChallengeUndecryptable(this ILogger logger, string sessionId);

    [LoggerMessage(2, LogLevel.Debug, "DBSC refresh challenge for session {SessionId} decrypted but was not in the expected shape.", EventName = "RefreshChallengeMalformed")]
    public static partial void RefreshChallengeMalformed(this ILogger logger, string sessionId);

    [LoggerMessage(3, LogLevel.Warning, "DBSC refresh challenge for session {SessionId} is bound to a different principal than the request; this should not happen for a browser-originated request and may indicate a client or server bug.", EventName = "RefreshChallengePrincipalMismatch")]
    public static partial void RefreshChallengePrincipalMismatch(this ILogger logger, string sessionId);

    [LoggerMessage(4, LogLevel.Warning, "DBSC refresh challenge is bound to a different session than the request ({SessionId}); this should not happen for a browser-originated request and may indicate a client or server bug.", EventName = "RefreshChallengeSessionMismatch")]
    public static partial void RefreshChallengeSessionMismatch(this ILogger logger, string sessionId);

    // Registration endpoint (DeviceBoundSessionHandler).

    [LoggerMessage(6, LogLevel.Warning, "DBSC registration: no valid authentication from the source scheme.", EventName = "RegistrationNoSourceAuthentication")]
    public static partial void RegistrationNoSourceAuthentication(this ILogger logger);

    [LoggerMessage(7, LogLevel.Debug, "DBSC registration: the proof did not contain a challenge (jti).", EventName = "RegistrationChallengeMissing")]
    public static partial void RegistrationChallengeMissing(this ILogger logger);

    [LoggerMessage(10, LogLevel.Debug, "DBSC registration challenge could not be decrypted; it is expired, tampered, or was minted for a different flow or version.", EventName = "RegistrationChallengeUndecryptable")]
    public static partial void RegistrationChallengeUndecryptable(this ILogger logger);

    [LoggerMessage(11, LogLevel.Debug, "DBSC registration challenge decrypted but was not in the expected shape.", EventName = "RegistrationChallengeMalformed")]
    public static partial void RegistrationChallengeMalformed(this ILogger logger);

    [LoggerMessage(12, LogLevel.Warning, "DBSC registration challenge is bound to a different principal than the request; this should not happen for a browser-originated request and may indicate a client or server bug.", EventName = "RegistrationChallengePrincipalMismatch")]
    public static partial void RegistrationChallengePrincipalMismatch(this ILogger logger);

    // Refresh endpoint (DeviceBoundSessionHandler).

    [LoggerMessage(8, LogLevel.Warning, "DBSC refresh: no valid refresh cookie for session {SessionId}.", EventName = "RefreshNoCookie")]
    public static partial void RefreshNoCookie(this ILogger logger, string sessionId);

    // Proof JWT validation (DeviceBoundSessionJwtValidator). All are routine rejections of a bad or
    // unsupported proof and are logged at Debug.

    [LoggerMessage(13, LogLevel.Debug, "DBSC proof rejected: the token is not a well-formed JWT.", EventName = "ProofMalformed")]
    public static partial void ProofMalformed(this ILogger logger);

    [LoggerMessage(14, LogLevel.Debug, "DBSC proof rejected: the 'typ' header is not 'dbsc+jwt'.", EventName = "ProofWrongType")]
    public static partial void ProofWrongType(this ILogger logger);

    [LoggerMessage(15, LogLevel.Debug, "DBSC proof rejected: the 'alg' header is missing.", EventName = "ProofMissingAlgorithm")]
    public static partial void ProofMissingAlgorithm(this ILogger logger);

    [LoggerMessage(16, LogLevel.Debug, "DBSC proof rejected: no 'jwk' header and no stored key to validate against.", EventName = "ProofMissingKey")]
    public static partial void ProofMissingKey(this ILogger logger);

    [LoggerMessage(17, LogLevel.Debug, "DBSC proof rejected: could not build a signing key (malformed JWK, unsupported algorithm, or key-type mismatch).", EventName = "ProofUnsupportedKey")]
    public static partial void ProofUnsupportedKey(this ILogger logger);

    [LoggerMessage(18, LogLevel.Debug, "DBSC proof rejected: signature validation failed.", EventName = "ProofSignatureInvalid")]
    public static partial void ProofSignatureInvalid(this ILogger logger);

    [LoggerMessage(19, LogLevel.Debug, "DBSC proof rejected: the challenge (jti) did not match the expected value.", EventName = "ProofChallengeMismatch")]
    public static partial void ProofChallengeMismatch(this ILogger logger);
}
