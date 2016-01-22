// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Provides information regarding TLS token binding parameters.
    /// </summary>
    /// <remarks>
    /// TLS token bindings help mitigate the risk of impersonation by an attacker in the
    /// event an authenticated client's bearer tokens are somehow exfiltrated from the
    /// client's machine. See https://datatracker.ietf.org/doc/draft-popov-token-binding/
    /// for more information.
    /// </remarks>
    public interface ITlsTokenBindingFeature
    {
        /// <summary>
        /// Gets the 'provided' token binding identifier associated with the request.
        /// </summary>
        /// <returns>The token binding identifier, or null if the client did not
        /// supply a 'provided' token binding or valid proof of possession of the
        /// associated private key. The caller should treat this identifier as an
        /// opaque blob and should not try to parse it.</returns>
        byte[] GetProvidedTokenBindingId();

        /// <summary>
        /// Gets the 'referred' token binding identifier associated with the request.
        /// </summary>
        /// <returns>The token binding identifier, or null if the client did not
        /// supply a 'referred' token binding or valid proof of possession of the
        /// associated private key. The caller should treat this identifier as an
        /// opaque blob and should not try to parse it.</returns>
        byte[] GetReferredTokenBindingId();
    }
}
