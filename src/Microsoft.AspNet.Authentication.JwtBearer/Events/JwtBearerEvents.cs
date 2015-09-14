// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

/// <summary>
/// Specifies events which the <see cref="JwtBearerAuthenticationMiddleware"></see> invokes to enable developer control over the authentication process. />
/// </summary>
namespace Microsoft.AspNet.Authentication.JwtBearer
{
    /// <summary>
    /// Jwt bearer token middleware events.
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
        public Func<MessageReceivedContext, Task> OnMessageReceived { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked with the security token that has been extracted from the protocol message.
        /// </summary>
        public Func<SecurityTokenReceivedContext, Task> OnSecurityTokenReceived { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
        /// </summary>
        public Func<SecurityTokenValidatedContext, Task> OnSecurityTokenValidated { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Invoked to apply a challenge sent back to the caller.
        /// </summary>
        public Func<AuthenticationChallengeContext, Task> OnApplyChallenge { get; set; } = context =>
        {
            context.HttpContext.Response.Headers.Append("WWW-Authenticate", context.Options.Challenge);
            return Task.FromResult(0);
        };

        public virtual Task AuthenticationFailed(AuthenticationFailedContext context) => OnAuthenticationFailed(context);

        public virtual Task MessageReceived(MessageReceivedContext context) => OnMessageReceived(context);

        public virtual Task SecurityTokenReceived(SecurityTokenReceivedContext context) => OnSecurityTokenReceived(context);

        public virtual Task SecurityTokenValidated(SecurityTokenValidatedContext context) => OnSecurityTokenValidated(context);

        public virtual Task ApplyChallenge(AuthenticationChallengeContext context) => OnApplyChallenge(context);
    }
}
