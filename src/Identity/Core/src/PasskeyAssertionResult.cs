// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the result of a passkey assertion operation.
/// </summary>
public sealed class PasskeyAssertionResult<TUser>
    where TUser : class
{
    /// <summary>
    /// Gets whether the assertion was successful.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Passkey))]
    [MemberNotNullWhen(true, nameof(User))]
    [MemberNotNullWhen(false, nameof(Failure))]
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the updated passkey information when assertion succeeds.
    /// </summary>
    public UserPasskeyInfo? Passkey { get; }

    /// <summary>
    /// Gets the user associated with the passkey when assertion succeeds.
    /// </summary>
    public TUser? User { get; }

    /// <summary>
    /// Gets the error that occurred during assertion.
    /// </summary>
    public PasskeyException? Failure { get; }

    internal PasskeyAssertionResult(UserPasskeyInfo passkey, TUser user)
    {
        Succeeded = true;
        Passkey = passkey;
        User = user;
    }

    internal PasskeyAssertionResult(PasskeyException failure)
    {
        Succeeded = false;
        Failure = failure;
    }
}

/// <summary>
/// A factory class for creating instances of <see cref="PasskeyAssertionResult{TUser}"/>.
/// </summary>
public static class PasskeyAssertionResult
{
    /// <summary>
    /// Creates a successful result for a passkey assertion operation.
    /// </summary>
    /// <param name="passkey">The passkey information associated with the assertion.</param>
    /// <param name="user">The user associated with the passkey.</param>
    /// <returns>A <see cref="PasskeyAssertionResult{TUser}"/> instance representing a successful assertion.</returns>
    public static PasskeyAssertionResult<TUser> Success<TUser>(UserPasskeyInfo passkey, TUser user)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(passkey);
        ArgumentNullException.ThrowIfNull(user);
        return new PasskeyAssertionResult<TUser>(passkey, user);
    }

    /// <summary>
    /// Creates a failed result for a passkey assertion operation.
    /// </summary>
    /// <param name="failure">The exception that describes the reason for the failure.</param>
    /// <returns>A <see cref="PasskeyAssertionResult{TUser}"/> instance representing the failure.</returns>
    public static PasskeyAssertionResult<TUser> Fail<TUser>(PasskeyException failure)
        where TUser : class
    {
        return new PasskeyAssertionResult<TUser>(failure);
    }
}
