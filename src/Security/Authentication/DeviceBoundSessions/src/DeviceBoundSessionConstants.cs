// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Centralized DBSC wire-protocol constants (header names and supported signing algorithms)
/// shared across the registration-header writer, the protocol handler, and the proof validator.
/// </summary>
internal static class DeviceBoundSessionConstants
{
    /// <summary>
    /// DBSC HTTP header names (the long <c>Secure-Session-*</c> / <c>Sec-Secure-Session-Id</c> forms).
    /// </summary>
    internal static class Headers
    {
        /// <summary>Response header that triggers registration and carries the challenge.</summary>
        internal const string Registration = "Secure-Session-Registration";

        /// <summary>Response header that demands a fresh proof (emitted only on 403).</summary>
        internal const string Challenge = "Secure-Session-Challenge";

        /// <summary>Request header that carries the device's proof-of-possession JWT.</summary>
        internal const string Proof = "Secure-Session-Response";

        /// <summary>Request header that identifies the session on refresh.</summary>
        internal const string SessionId = "Sec-Secure-Session-Id";
    }

    /// <summary>
    /// ECDSA P-256 / SHA-256 proof signing algorithm.
    /// </summary>
    internal const string Es256 = "ES256";

    /// <summary>
    /// RSASSA-PKCS1-v1_5 / SHA-256 proof signing algorithm.
    /// </summary>
    internal const string Rs256 = "RS256";

    /// <summary>
    /// The supported algorithms formatted as the structured-field inner-list advertised in the
    /// <see cref="Headers.Registration"/> header (e.g. <c>(ES256 RS256)</c>). This advertised set
    /// MUST stay in lock-step with the algorithms the proof validator accepts (<see cref="Es256"/>,
    /// <see cref="Rs256"/>).
    /// </summary>
    internal const string AdvertisedAlgorithms = "(" + Es256 + " " + Rs256 + ")";
}
