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
    /// Jwt bearer token middleware provider
    /// </summary>
    public class JwtBearerAuthenticationEvents
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JwtBearerAuthenticationProvider"/> class
        /// </summary>
        public JwtBearerAuthenticationEvents()
        {
            ApplyChallenge = context => { context.HttpContext.Response.Headers.Append("WWW-Authenticate", context.Options.Challenge); return Task.FromResult(0); };
            AuthenticationFailed = context => Task.FromResult(0);
            MessageReceived = context => Task.FromResult(0);
            SecurityTokenReceived = context => Task.FromResult(0);
            SecurityTokenValidated = context => Task.FromResult(0);
        }

        /// <summary>
        /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
        /// </summary>
        public Func<AuthenticationFailedContext, Task> AuthenticationFailed { get; set; }

        /// <summary>
        /// Invoked when a protocol message is first received.
        /// </summary>
        public Func<MessageReceivedContext, Task> MessageReceived { get; set; }

        /// <summary>
        /// Invoked with the security token that has been extracted from the protocol message.
        /// </summary>
        public Func<SecurityTokenReceivedContext, Task> SecurityTokenReceived { get; set; }

        /// <summary>
        /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
        /// </summary>
        public Func<SecurityTokenValidatedContext, Task> SecurityTokenValidated { get; set; }

        /// <summary>
        /// Invoked to apply a challenge sent back to the caller.
        /// </summary>
        public Func<AuthenticationChallengeContext, Task> ApplyChallenge { get; set; }
    }
}
