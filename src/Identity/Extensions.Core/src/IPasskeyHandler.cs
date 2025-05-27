// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents a handler for passkey assertion and attestation.
/// </summary>
public interface IPasskeyHandler<TUser>
    where TUser : class
{
    /// <summary>
    /// Performs passkey attestation using the provided credential JSON and original options JSON.
    /// </summary>
    /// <param name="credentialJson">The credentials obtained by JSON-serializing the result of the <c>navigator.credentials.create()</c> JavaScript function.</param>
    /// <param name="originalOptionsJson">The JSON representation of the original passkey creation options provided to the browser.</param>
    /// <param name="userManager">The <see cref="UserManager{TUser}"/> to retrieve user information from.</param>
    /// <returns>A task object representing the asynchronous operation containing the <see cref="PasskeyAttestationResult"/>.</returns>
    Task<PasskeyAttestationResult> PerformAttestationAsync(string credentialJson, string originalOptionsJson, UserManager<TUser> userManager);

    /// <summary>
    /// Performs passkey assertion using the provided credential JSON, original options JSON, and optional user.
    /// </summary>
    /// <param name="user">The user associated with the passkey, if known.</param>
    /// <param name="credentialJson">The credentials obtained by JSON-serializing the result of the <c>navigator.credentials.get()</c> JavaScript function.</param>
    /// <param name="originalOptionsJson">The JSON representation of the original passkey creation options provided to the browser.</param>
    /// <param name="userManager">The <see cref="UserManager{TUser}"/> to retrieve user information from.</param>
    /// <returns>A task object representing the asynchronous operation containing the <see cref="PasskeyAssertionResult{TUser}"/>.</returns>
    Task<PasskeyAssertionResult<TUser>> PerformAssertionAsync(TUser? user, string credentialJson, string originalOptionsJson, UserManager<TUser> userManager);
}
