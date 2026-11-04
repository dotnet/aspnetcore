// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Certificate;

/// <summary>
/// This default implementation of the IBasicAuthenticationEvents may be used if the
/// application only needs to override a few of the interface methods.
/// This may be used as a base class or may be instantiated directly.
/// </summary>
public class CertificateAuthenticationEvents
{
    /// <summary>
    /// A delegate assigned to this property will be invoked when the authentication fails.
    /// </summary>
    public Func<CertificateAuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// A delegate assigned to this property will be invoked when a certificate has passed basic validation, but where custom validation may be needed.
    /// </summary>
    /// <remarks>
    /// You must provide a delegate for this property for authentication to occur.
    /// In your delegate you should construct an authentication principal from the user details,
    /// attach it to the context.Principal property and finally call context.Success();
    /// </remarks>
    public Func<CertificateValidatedContext, Task> OnCertificateValidated { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked before a challenge is sent back to the caller.
    /// </summary>
    public Func<CertificateChallengeContext, Task> OnChallenge { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked when a certificate fails authentication.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual Task AuthenticationFailed(CertificateAuthenticationFailedContext context) => OnAuthenticationFailed(context);

    /// <summary>
    /// Invoked after a certificate has been validated
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual Task CertificateValidated(CertificateValidatedContext context) => OnCertificateValidated(context);

    /// <summary>
    /// Invoked before a challenge is sent back to the caller.
    /// </summary>
    public virtual Task Challenge(CertificateChallengeContext context) => OnChallenge(context);
}
