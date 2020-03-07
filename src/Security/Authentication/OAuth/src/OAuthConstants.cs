// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    /// <summary>
    /// Constants used in the OAuth protocol
    /// </summary>
    public static class OAuthConstants
    {
        /// <summary>
        /// code_verifier defined in https://tools.ietf.org/html/rfc7636
        /// </summary>
        public static readonly string CodeVerifierKey = "code_verifier";

        /// <summary>
        /// code_challenge defined in https://tools.ietf.org/html/rfc7636
        /// </summary>
        public static readonly string CodeChallengeKey = "code_challenge";

        /// <summary>
        /// code_challenge_method defined in https://tools.ietf.org/html/rfc7636
        /// </summary>
        public static readonly string CodeChallengeMethodKey = "code_challenge_method";

        /// <summary>
        /// S256 defined in https://tools.ietf.org/html/rfc7636
        /// </summary>
        public static readonly string CodeChallengeMethodS256 = "S256";
    }
}
