// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HeaderPropagation;

/// <summary>
/// Provides configuration for the <see cref="HeaderPropagationMiddleware"/>.
/// </summary>
public class HeaderPropagationOptions
{
    /// <summary>
    /// Gets or sets the headers to be captured by the <see cref="HeaderPropagationMiddleware"/>
    /// and to be propagated by the <see cref="HeaderPropagationMessageHandler"/>.
    /// </summary>
    /// <remarks>
    /// Entries in <see cref="Headers"/> are processed in order while capturing headers inside
    /// <see cref="HeaderPropagationMiddleware"/>. This can cause an earlier entry to take precedence
    /// over a later entry if they have the same <see cref="HeaderPropagationEntry.CapturedHeaderName"/>.
    /// </remarks>
    public HeaderPropagationEntryCollection Headers { get; set; } = new HeaderPropagationEntryCollection();
}
