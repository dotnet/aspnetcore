// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    /// <summary>
    /// Specifies events which the <see cref="NegotiateHandler"/> invokes to enable developer control over the authentication process.
    /// </summary>
    public class NegotiateEvents
    {
        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
        /// </summary>
        public Func<AuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = context => Task.CompletedTask;

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        public Func<MessageReceivedContext, Task> OnMessageReceived { get; set; } = context => Task.CompletedTask;

        /// <summary>
        /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
        /// </summary>
        public Func<TokenValidatedContext, Task> OnTokenValidated { get; set; } = context => Task.CompletedTask;

        /// <summary>
        /// Invoked before a challenge is sent back to the caller.
        /// </summary>
        public Func<NegotiateChallengeContext, Task> OnChallenge { get; set; } = context => Task.CompletedTask;

        public virtual Task AuthenticationFailed(AuthenticationFailedContext context) => OnAuthenticationFailed(context);

        public virtual Task MessageReceived(MessageReceivedContext context) => OnMessageReceived(context);

        public virtual Task TokenValidated(TokenValidatedContext context) => OnTokenValidated(context);

        public virtual Task Challenge(NegotiateChallengeContext context) => OnChallenge(context);
    }
}
