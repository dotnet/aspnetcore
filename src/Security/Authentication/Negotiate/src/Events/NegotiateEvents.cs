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
        /// Invoked after the authentication is complete and a ClaimsIdentity has been generated.
        /// </summary>
        public Func<AuthenticatedContext, Task> OnAuthenticated { get; set; } = context => Task.CompletedTask;

        /// <summary>
        /// Invoked before a challenge is sent back to the caller.
        /// </summary>
        public Func<ChallengeContext, Task> OnChallenge { get; set; } = context => Task.CompletedTask;

        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
        /// </summary>
        public virtual Task AuthenticationFailed(AuthenticationFailedContext context) => OnAuthenticationFailed(context);

        /// <summary>
        /// Invoked after the authentication is complete and a ClaimsIdentity has been generated.
        /// </summary>
        public virtual Task Authenticated(AuthenticatedContext context) => OnAuthenticated(context);

        /// <summary>
        /// Invoked before a challenge is sent back to the caller.
        /// </summary>
        public virtual Task Challenge(ChallengeContext context) => OnChallenge(context);
    }
}
