// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Specifies a summary in <see cref="Endpoint.Metadata"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
[DebuggerDisplay("{ToString(),nq}")]
public sealed class EndpointSummaryAttribute : Attribute, IEndpointSummaryMetadata
{
    /// <summary>
    /// Initializes an instance of the <see cref="EndpointSummaryAttribute"/>.
    /// </summary>
    /// <param name="summary">The summary associated with the endpoint or parameter.</param>
    public EndpointSummaryAttribute(string summary)
    {
        Summary = summary;
    }

    /// <inheritdoc />
    public string Summary { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return DebuggerHelpers.GetDebugText(nameof(Summary), Summary);
    }
}
