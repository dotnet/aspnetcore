// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Metadata that disables request rate limiting on an endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DisableRateLimitingAttribute : Attribute
{
    internal static DisableRateLimitingAttribute Instance { get; } = new DisableRateLimitingAttribute();
}
