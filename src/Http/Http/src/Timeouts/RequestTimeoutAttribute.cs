// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Timeouts;

/// <summary>
/// Metadata that provides endpoint-specific request timeouts.
/// </summary>
/// <remarks>
/// The default policy will be ignored with this attribute applied.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequestTimeoutAttribute : Attribute
{
    /// <summary>
    /// The timeout to apply for this endpoint.
    /// </summary>
    public TimeSpan? Timeout { get; }

    /// <summary>
    /// The name of the policy which needs to be applied.
    /// This value is case insensitve.
    /// </summary>
    public string? PolicyName { get; }

    /// <summary>
    /// Creates a new instance of <see cref="RequestTimeoutAttribute"/> using the specified timeout.
    /// </summary>
    /// <param name="milliseconds">The amount of timeout for this specific endpoint.</param>
    public RequestTimeoutAttribute(int milliseconds)
    {
        Timeout = TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <summary>
    /// Creates a new instance of <see cref="RequestTimeoutAttribute"/> using the specified policy.
    /// </summary>
    /// <param name="policyName">The case-insensitve name of the policy which needs to be applied.</param>
    public RequestTimeoutAttribute(string policyName)
    {
        PolicyName = policyName;
    }
}
