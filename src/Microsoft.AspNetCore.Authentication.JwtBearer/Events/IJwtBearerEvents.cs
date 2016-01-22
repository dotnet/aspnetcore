// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

/// <summary>
/// Specifies events which the <see cref="JwtBearerAuthenticationMiddleware"></see> invokes to enable developer control over the authentication process. />
/// </summary>
namespace Microsoft.AspNet.Authentication.JwtBearer
{
    /// <summary>
    /// OpenIdConnect bearer token middleware events.
    /// </summary>
    public interface IJwtBearerEvents
    {
        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
        /// </summary>
        Task AuthenticationFailed(AuthenticationFailedContext context);

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        Task ReceivingToken(ReceivingTokenContext context);

        /// <summary>
        /// Invoked with the security token that has been extracted from the protocol message.
        /// </summary>
        Task ReceivedToken(ReceivedTokenContext context);

        /// <summary>
        /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
        /// </summary>
        Task ValidatedToken(ValidatedTokenContext context);

        /// <summary>
        /// Invoked to apply a challenge sent back to the caller.
        /// </summary>
        Task Challenge(JwtBearerChallengeContext context);
    }
}
