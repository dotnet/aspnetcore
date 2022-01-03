// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.JwtBearer;

/// <summary>
/// Specifies events which the <see cref="JwtBearerHandler"/> invokes to enable developer control over the authentication process.
/// </summary>
public class JwtBearerEvents
{
    /// <summary>
    /// Invoked if authentication fails during request processing. The exceptions will be re-thrown after this event unless suppressed.
    /// </summary>
    public Func<AuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked if Authorization fails and results in a Forbidden response.
    /// </summary>
    public Func<ForbiddenContext, Task> OnForbidden { get; set; } = context => Task.CompletedTask;

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
    public Func<JwtBearerChallengeContext, Task> OnChallenge { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
    /// </summary>
    public virtual Task AuthenticationFailed(AuthenticationFailedContext context) => OnAuthenticationFailed(context);

    /// <summary>
    /// Invoked if Authorization fails and results in a Forbidden response
    /// </summary>
    public virtual Task Forbidden(ForbiddenContext context) => OnForbidden(context);

    /// <summary>
    /// Invoked when a protocol message is first received.
    /// </summary>
    public virtual Task MessageReceived(MessageReceivedContext context) => OnMessageReceived(context);

    /// <summary>
    /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
    /// </summary>
    public virtual Task TokenValidated(TokenValidatedContext context) => OnTokenValidated(context);

    /// <summary>
    /// Invoked before a challenge is sent back to the caller.
    /// </summary>
    public virtual Task Challenge(JwtBearerChallengeContext context) => OnChallenge(context);
}
