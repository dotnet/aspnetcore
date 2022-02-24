// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Specifies the results for password verification.
/// </summary>
public enum PasswordVerificationResult
{
    /// <summary>
    /// Indicates password verification failed.
    /// </summary>
    Failed = 0,

    /// <summary>
    /// Indicates password verification was successful.
    /// </summary>
    Success = 1,

    /// <summary>
    /// Indicates password verification was successful however the password was encoded using a deprecated algorithm
    /// and should be rehashed and updated.
    /// </summary>
    SuccessRehashNeeded = 2
}
