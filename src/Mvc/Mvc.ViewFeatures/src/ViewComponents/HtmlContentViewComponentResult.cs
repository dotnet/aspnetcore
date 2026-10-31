// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// An <see cref="IViewComponentResult"/> which writes an <see cref="IHtmlContent"/> when executed.
/// </summary>
/// <remarks>
/// The provided content will be HTML-encoded as specified when the content was created. To encoded and write
/// text, use a <see cref="ContentViewComponentResult"/>.
/// </remarks>
public class HtmlContentViewComponentResult : IViewComponentResult
{
    /// <summary>
    /// Initializes a new <see cref="HtmlContentViewComponentResult"/>.
    /// </summary>
    public HtmlContentViewComponentResult(IHtmlContent encodedContent)
    {
        ArgumentNullException.ThrowIfNull(encodedContent);

        EncodedContent = encodedContent;
    }

    /// <summary>
    /// Gets the encoded content.
    /// </summary>
    public IHtmlContent EncodedContent { get; }

    /// <summary>
    /// Writes the <see cref="EncodedContent"/>.
    /// </summary>
    /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
    public void Execute(ViewComponentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Writer.Write(EncodedContent);
    }

    /// <summary>
    /// Writes the <see cref="EncodedContent"/>.
    /// </summary>
    /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public Task ExecuteAsync(ViewComponentContext context)
    {
        Execute(context);

        return Task.CompletedTask;
    }
}
