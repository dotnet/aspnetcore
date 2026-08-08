// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// The application's compile-time metadata for a Razor components application. An application declares
/// a partial class deriving from this type and the Blazor Native AOT metadata source generator
/// implements its members.
/// </summary>
/// <remarks>
/// <para>
/// This type is data only. Lookup semantics — how a base-type chain is walked for an instance interop
/// call, how two contexts describing the same type reconcile, and how the indexes are keyed — are
/// framework concerns, so putting them here would make each of them something a generator has to
/// reimplement identically. Flat lists also mean multiple contexts compose by concatenation, so a
/// component library shipping its own metadata needs no coordination.
/// </para>
/// <para>
/// This API is experimental, unsupported, and subject to change or removal in any release.
/// </para>
/// </remarks>
/// <example>
/// The only file an application writes:
/// <code>
/// [BindableModel(ModelType = typeof(LoginModel))]
/// internal sealed partial class AppMetadata : RazorComponentsMetadataContext
/// {
///     public override IJsonTypeInfoResolver? JsonTypeInfoResolver =&gt; AppJsonContext.Default;
/// }
/// </code>
/// It is registered with a single call:
/// <code>
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
    /// Gets the described components, which supply discovery, routing, activation and parameter binding.
    /// </summary>
    public abstract IReadOnlyList<ComponentDescriptor> Components { get; }

    /// <summary>
    /// Gets the described form model types, which supply the accessors a binding expression walks.
    /// </summary>
    public abstract IReadOnlyList<BindableTypeDescriptor> BindableTypes { get; }

    /// <summary>
    /// Gets the described <see cref="Microsoft.JSInterop.JSInvokableAttribute"/> methods.
    /// </summary>
    public abstract IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods { get; }

    /// <summary>
    /// Gets the application's JSON contracts, used wherever the framework serializes an application
    /// type, or <see langword="null"/> when the application supplies none.
    /// </summary>
    /// <remarks>
    /// A single resolver rather than a list because an application has one
    /// <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>; composing across contexts is
    /// the framework's job.
    /// </remarks>
    public abstract IJsonTypeInfoResolver? JsonTypeInfoResolver { get; }
}
