// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// A context for view components.
/// </summary>
public class ViewComponentContext
{
    /// <summary>
    /// Creates a new <see cref="ViewComponentContext"/>.
    /// </summary>
    /// <remarks>
    /// The default constructor is provided for unit test purposes only.
    /// </remarks>
    public ViewComponentContext()
    {
        ViewComponentDescriptor = new ViewComponentDescriptor();
        ViewContext = new ViewContext();
    }

    /// <summary>
    /// Creates a new <see cref="ViewComponentContext"/>.
    /// </summary>
    /// <param name="viewComponentDescriptor">
    /// The <see cref="ViewComponentContext"/> for the view component being invoked.
    /// </param>
    /// <param name="arguments">The view component arguments.</param>
    /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/> to use.</param>
    /// <param name="viewContext">The <see cref="ViewContext"/>.</param>
    /// <param name="writer">The <see cref="TextWriter"/> for writing output.</param>
    public ViewComponentContext(
        ViewComponentDescriptor viewComponentDescriptor,
        IDictionary<string, object?> arguments,
        HtmlEncoder htmlEncoder,
        ViewContext viewContext,
        TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(viewComponentDescriptor);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(htmlEncoder);
        ArgumentNullException.ThrowIfNull(viewContext);
        ArgumentNullException.ThrowIfNull(writer);

        ViewComponentDescriptor = viewComponentDescriptor;
        Arguments = arguments;
        HtmlEncoder = htmlEncoder;

        // We want to create a defensive copy of the VDD here so that changes done in the VC
        // aren't visible in the calling view.
        ViewContext = new ViewContext(
            viewContext,
            viewContext.View,
            new ViewDataDictionary<object>(viewContext.ViewData),
            writer);
    }

    /// <summary>
    /// Gets or sets the view component arguments.
    /// </summary>
    /// <remarks>
    /// The property setter is provided for unit test purposes only.
    /// </remarks>
    public IDictionary<string, object?> Arguments { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="System.Text.Encodings.Web.HtmlEncoder"/>.
    /// </summary>
    /// <remarks>
    /// The property setter is provided for unit test purposes only.
    /// </remarks>
    public HtmlEncoder HtmlEncoder { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="ViewComponents.ViewComponentDescriptor"/> for the view component being invoked.
    /// </summary>
    /// <remarks>
    /// The property setter is provided for unit test purposes only.
    /// </remarks>
    public ViewComponentDescriptor ViewComponentDescriptor { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Rendering.ViewContext"/>.
    /// </summary>
    /// <remarks>
    /// The property setter is provided for unit test purposes only.
    /// </remarks>
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Gets the <see cref="ViewDataDictionary"/>.
    /// </summary>
    /// <remarks>
    /// This is an alias for <c>ViewContext.ViewData</c>.
    /// </remarks>
    public ViewDataDictionary ViewData => ViewContext.ViewData;

    /// <summary>
    /// Gets the <see cref="ITempDataDictionary"/>.
    /// </summary>
    /// <remarks>
    /// This is an alias for <c>ViewContext.TempData</c>.
    /// </remarks>
    public ITempDataDictionary TempData => ViewContext.TempData;

    /// <summary>
    /// Gets the <see cref="TextWriter"/> for output.
    /// </summary>
    /// <remarks>
    /// This is an alias for <c>ViewContext.Writer</c>.
    /// </remarks>
    public TextWriter Writer => ViewContext.Writer;
}
