// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Specifies a summary in <see cref="Endpoint.Metadata"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Delegate, Inherited = false, AllowMultiple = false)]
public sealed class SummaryAttribute : Attribute, ISummaryMetadata
{
    /// <summary>
    /// Initializes an instance of the <see cref="SummaryAttribute"/>.
    /// </summary>
    /// <param name="summary">The summary associated with the endpoint or parameter.</param>
    public SummaryAttribute(string summary)
    {
        Summary = summary;
    }

    /// <inheritdoc />
    public string Summary { get; }
}
