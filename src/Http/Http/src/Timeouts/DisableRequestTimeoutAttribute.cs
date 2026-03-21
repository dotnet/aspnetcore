// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Http.Timeouts;

/// <summary>
/// Metadata that disables request timeouts on an endpoint.
/// </summary>
/// <remarks>
/// Completely disables the request timeouts middleware from applying to this endpoint.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DisableRequestTimeoutAttribute : Attribute
{
    /// <inheritdoc/>
    public override string ToString()
    {
        return "DisableRequestTimeout";
    }
}
