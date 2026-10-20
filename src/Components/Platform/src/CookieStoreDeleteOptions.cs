// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the name and attributes of a cookie to delete.
/// </summary>
public sealed class CookieStoreDeleteOptions
{
    /// <summary>
    /// Gets or sets the name of the cookie.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the domain of the cookie, or <see langword="null"/> for a cookie restricted to the host of the current page.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the path of the cookie. The default is <c>"/"</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Path { get; set; } = "/";

    /// <summary>
    /// Gets or sets a value that indicates whether the cookie to delete is partitioned. The default is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// A browser that does not support partitioned cookies ignores this setting and deletes the cookie that is not partitioned.
    /// </remarks>
    [Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
    public bool Partitioned { get; set; }
}
