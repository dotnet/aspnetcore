// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Specifies that HTTP request duration metrics is disabled for an endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DisableHttpMetricsAttribute : Attribute, IDisableHttpMetricsMetadata
{
    /// <inheritdoc/>
    public override string ToString()
    {
        return "DisableHttpMetrics";
    }
}
