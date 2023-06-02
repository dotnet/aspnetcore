// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.BearerToken;

/// <summary>
/// Specifies events which the bearer token handler invokes to enable developer control over the authentication process.
/// </summary>
public class BearerTokenEvents
{
    /// <summary>
    /// Invoked when a protocol message is first received.
    /// </summary>
    public Func<MessageReceivedContext, Task> OnMessageReceived { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked when signing in.
    /// </summary>
    public Func<SigningInContext, Task> OnSigningIn { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked when a protocol message is first received.
    /// </summary>
    /// <param name="context">The <see cref="MessageReceivedContext"/>.</param>
    public virtual Task MessageReceivedAsync(MessageReceivedContext context) => OnMessageReceived(context);

    /// <summary>
    /// Invoked when signing in.
    /// </summary>
    /// <param name="context">The <see cref="SigningInContext"/>.</param>
    public virtual Task SigningInAsync(SigningInContext context) => OnSigningIn(context);
}
