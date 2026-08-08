// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Provides compile-time metadata for a Razor components application.
/// </summary>
/// <remarks>
/// This API is experimental, unsupported, and subject to change or removal in any release.
/// </remarks>
/// <example>
/// <code>
/// internal sealed class AppMetadata : RazorComponentsMetadataContext
/// {
///     public override IJsonTypeInfoResolver? JsonTypeInfoResolver =&gt; AppJsonContext.Default;
/// }
///
/// builder.Services.AddComponentMetadata&lt;AppMetadata&gt;();
/// </code>
/// </example>
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class RazorComponentsMetadataContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="RazorComponentsMetadataContext"/>.
    /// </summary>
    protected RazorComponentsMetadataContext()
    {
    }

    /// <summary>
    /// Gets the application's JSON contracts, or <see langword="null"/> when the application supplies none.
    /// </summary>
    public abstract IJsonTypeInfoResolver? JsonTypeInfoResolver { get; }
}
