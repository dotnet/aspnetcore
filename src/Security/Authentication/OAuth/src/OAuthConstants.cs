// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.OAuth;

/// <summary>
/// Constants used in the OAuth protocol
/// </summary>
public static class OAuthConstants
{
    /// <summary>
    /// code_verifier defined in <see href="https://tools.ietf.org/html/rfc7636"/>.
    /// </summary>
    public static readonly string CodeVerifierKey = "code_verifier";

    /// <summary>
    /// code_challenge defined in <see href="https://tools.ietf.org/html/rfc7636"/>.
    /// </summary>
    public static readonly string CodeChallengeKey = "code_challenge";

    /// <summary>
    /// code_challenge_method defined in <see href="https://tools.ietf.org/html/rfc7636"/>.
    /// </summary>
    public static readonly string CodeChallengeMethodKey = "code_challenge_method";

    /// <summary>
    /// S256 defined in <see href="https://tools.ietf.org/html/rfc7636"/>.
    /// </summary>
    public static readonly string CodeChallengeMethodS256 = "S256";
}
