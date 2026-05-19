// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// An <see cref="IViewComponentResult"/> which writes text when executed.
/// </summary>
/// <remarks>
/// The provided content will be HTML-encoded when written. To write pre-encoded content, use an
/// <see cref="HtmlContentViewComponentResult"/>.
/// </remarks>
public class ContentViewComponentResult : IViewComponentResult
{
    /// <summary>
    /// Initializes a new <see cref="ContentViewComponentResult"/>.
    /// </summary>
    /// <param name="content">Content to write. The content will be HTML encoded when written.</param>
    public ContentViewComponentResult(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        Content = content;
    }

    /// <summary>
    /// Gets the content.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Encodes and writes the <see cref="Content"/>.
    /// </summary>
    /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
    public void Execute(ViewComponentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.HtmlEncoder.Encode(context.Writer, Content);
    }

    /// <summary>
    /// Encodes and writes the <see cref="Content"/>.
    /// </summary>
    /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public Task ExecuteAsync(ViewComponentContext context)
    {
        Execute(context);

        return Task.CompletedTask;
    }
}
