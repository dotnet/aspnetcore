// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation;

/// <summary>
/// Represents a compiled Razor View or Page.
/// </summary>
public class CompiledViewDescriptor
{
    /// <summary>
    /// Creates a new <see cref="CompiledViewDescriptor"/>.
    /// </summary>
    public CompiledViewDescriptor()
    {
    }

    /// <summary>
    /// Creates a new <see cref="CompiledViewDescriptor"/>.
    /// </summary>
    /// <param name="item">The <see cref="RazorCompiledItem"/>.</param>
    public CompiledViewDescriptor(RazorCompiledItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        Item = item;
        RelativePath = ViewPath.NormalizePath(item.Identifier);
    }

#pragma warning disable CS0618// Type or member is obsolete
    /// <summary>
    /// Creates a new <see cref="CompiledViewDescriptor"/>. At least one of <paramref name="attribute"/> or
    /// <paramref name="item"/> must be non-<c>null</c>.
    /// </summary>
    /// <param name="item">The <see cref="RazorCompiledItem"/>.</param>
    /// <param name="attribute">The <see cref="RazorViewAttribute"/>.</param>
    public CompiledViewDescriptor(RazorCompiledItem item, RazorViewAttribute attribute)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        if (item == null && attribute == null)
        {
            // We require at least one of these to be specified.
            throw new ArgumentException(Resources.FormatCompiledViewDescriptor_NoData(nameof(item), nameof(attribute)));
        }

        Item = item;

        //
        // For now we expect that MVC views and pages will still have either:
        // [RazorView(...)] or
        // [RazorPage(...)].
        //
        // In theory we could look at the 'Item.Kind' to determine what kind of thing we're dealing
        // with, but for compat reasons we're basing it on ViewAttribute since that's what 2.0 had.
#pragma warning disable CS0618 // Type or member is obsolete
        ViewAttribute = attribute;
#pragma warning restore CS0618 // Type or member is obsolete

        // We don't have access to the file provider here so we can't check if the files
        // even exist or what their checksums are. For now leave this empty, it will be updated
        // later.
        RelativePath = ViewPath.NormalizePath(item?.Identifier ?? attribute.Path);
    }

    /// <summary>
    /// The normalized application relative path of the view.
    /// </summary>
    public string RelativePath { get; set; } = default!;

#pragma warning disable CS0618
    // Type or member is obsolete
    /// <summary>
    /// Gets or sets the <see cref="RazorViewAttribute"/> decorating the view.
    /// </summary>
    /// <remarks>
    /// May be <c>null</c>.
    /// </remarks>
    [Obsolete("Use Item instead. RazorViewAttribute has been superseded by RazorCompiledItem and will not be used by the runtime.")]
    public RazorViewAttribute? ViewAttribute { get; set; }
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// <see cref="IChangeToken"/> instances that indicate when this result has expired.
    /// </summary>
    public IList<IChangeToken>? ExpirationTokens { get; set; }

    /// <summary>
    /// Gets the <see cref="RazorCompiledItem"/> descriptor for this view.
    /// </summary>
    public RazorCompiledItem? Item { get; set; }

    /// <summary>
    /// Gets the type of the compiled item.
    /// </summary>
    public Type? Type => Item?.Type;
}
