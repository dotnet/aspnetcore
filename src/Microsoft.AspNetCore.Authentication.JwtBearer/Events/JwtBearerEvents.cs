// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    /// <summary>
    /// Specifies events which the <see cref="JwtBearerMiddleware"/> invokes to enable developer control over the authentication process.
    /// </summary>
    public class JwtBearerEvents : IJwtBearerEvents
    {
        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
        /// </summary>
        public Func<AuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        public Func<ReceivingTokenContext, Task> OnReceivingToken { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked with the security token that has been extracted from the protocol message.
        /// </summary>
        public Func<ReceivedTokenContext, Task> OnReceivedToken { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
        /// </summary>
        public Func<ValidatedTokenContext, Task> OnValidatedToken { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked before a challenge is sent back to the caller.
        /// </summary>
        public Func<JwtBearerChallengeContext, Task> OnChallenge { get; set; } = context => Task.FromResult(0);

        public virtual Task AuthenticationFailed(AuthenticationFailedContext context) => OnAuthenticationFailed(context);

        public virtual Task ReceivingToken(ReceivingTokenContext context) => OnReceivingToken(context);

        public virtual Task ReceivedToken(ReceivedTokenContext context) => OnReceivedToken(context);

        public virtual Task ValidatedToken(ValidatedTokenContext context) => OnValidatedToken(context);

        public virtual Task Challenge(JwtBearerChallengeContext context) => OnChallenge(context);
    }
}
