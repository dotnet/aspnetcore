// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Describes one single-argument indexer of a form model type, so that a binding expression can
/// traverse it without compiling a delegate.
/// </summary>
/// <remarks>
/// <para>
/// Reached through <see cref="BindableTypeDescriptor.Indexers"/>. The declaring type is the containing
/// <see cref="BindableTypeDescriptor.Type"/>, and the framework selects an indexer by matching
/// <see cref="IndexType"/> against the static type of the index expression.
/// </para>
/// <para>
/// This API is experimental, unsupported, and subject to change or removal in any release.
/// </para>
/// </remarks>
/// <example>
/// The descriptor for <c>List&lt;Order&gt;.this[int]</c>:
/// <code>
/// new BindableIndexerDescriptor
/// {
///     IndexType = typeof(int),
///     ValueType = typeof(Order),
///     GetValue = static (target, index) =&gt; ((List&lt;Order&gt;)target)[(int)index!],
/// }
/// </code>
/// </example>
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class BindableIndexerDescriptor
{
    /// <summary>
    /// Gets the type of the indexer's single argument.
    /// </summary>
    public required Type IndexType { get; init; }

    /// <summary>
    /// Gets the type the indexer returns, which is the type of the next hop in the chain.
    /// </summary>
    public required Type ValueType { get; init; }

    /// <summary>
    /// Gets the delegate that reads an indexed value from an instance of the declaring type.
    /// </summary>
    public required Func<object, object?, object?> GetValue { get; init; }
}
