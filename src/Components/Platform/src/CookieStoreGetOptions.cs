// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the options that select the cookies to retrieve.
/// </summary>
public sealed class CookieStoreGetOptions
{
    /// <summary>
    /// Gets or sets the name of the cookies to retrieve, or <see langword="null"/> to match any name.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the URL to match the cookies against, or <see langword="null"/> to use the URL of the current page.
    /// The browser accepts only the URL of the current page.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }
}
