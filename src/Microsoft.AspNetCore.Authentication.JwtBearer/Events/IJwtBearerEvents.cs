// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    /// <summary>
    /// Specifies events which the <see cref="JwtBearerMiddleware"/> invokes to enable developer control over the authentication process.
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
        Task MessageReceived(MessageReceivedContext context);

        /// <summary>
        /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
        /// </summary>
        Task TokenValidated(TokenValidatedContext context);

        /// <summary>
        /// Invoked to apply a challenge sent back to the caller.
        /// </summary>
        Task Challenge(JwtBearerChallengeContext context);
    }
}
