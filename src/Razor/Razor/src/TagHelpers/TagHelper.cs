// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// An abstract base class for <see cref="ITagHelper"/>.
/// </summary>
public abstract class TagHelper : ITagHelper
{
    /// <summary>
    /// When a set of <see cref="ITagHelper"/>s are executed, their <see cref="Init(TagHelperContext)"/>'s
    /// are first invoked in the specified <see cref="Order"/>; then their
    /// <see cref="ProcessAsync(TagHelperContext, TagHelperOutput)"/>'s are invoked in the specified
    /// <see cref="Order"/>. Lower values are executed first.
    /// </summary>
    /// <remarks>Default order is <c>0</c>.</remarks>
    public virtual int Order { get; }

    /// <summary>
    /// Initializes the <see cref="ITagHelper"/> with the given <paramref name="context"/>. Additions to
    /// <see cref="TagHelperContext.Items"/> should be done within this method to ensure they're added prior to
    /// executing the children.
    /// </summary>
    /// <param name="context">Contains information associated with the current HTML tag.</param>
    /// <remarks>When more than one <see cref="ITagHelper"/> runs on the same element,
    /// <see cref="M:TagHelperOutput.GetChildContentAsync"/> may be invoked prior to <see cref="ProcessAsync"/>.
    /// </remarks>
    public virtual void Init(TagHelperContext context)
    {
    }

    /// <summary>
    /// Synchronously executes the <see cref="TagHelper"/> with the given <paramref name="context"/> and
    /// <paramref name="output"/>.
    /// </summary>
    /// <param name="context">Contains information associated with the current HTML tag.</param>
    /// <param name="output">A stateful HTML element used to generate an HTML tag.</param>
    public virtual void Process(TagHelperContext context, TagHelperOutput output)
    {
    }

    /// <summary>
    /// Asynchronously executes the <see cref="TagHelper"/> with the given <paramref name="context"/> and
    /// <paramref name="output"/>.
    /// </summary>
    /// <param name="context">Contains information associated with the current HTML tag.</param>
    /// <param name="output">A stateful HTML element used to generate an HTML tag.</param>
    /// <returns>A <see cref="Task"/> that on completion updates the <paramref name="output"/>.</returns>
    /// <remarks>By default this calls into <see cref="Process"/>.</remarks>.
    public virtual Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        Process(context, output);
        return Task.CompletedTask;
    }
}
