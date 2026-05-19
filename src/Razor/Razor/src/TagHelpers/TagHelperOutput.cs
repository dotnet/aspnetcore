// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Class used to represent the output of an <see cref="ITagHelper"/>.
/// </summary>
public class TagHelperOutput : IHtmlContentContainer
{
    private readonly Func<bool, HtmlEncoder, Task<TagHelperContent>> _getChildContentAsync;
    private TagHelperContent _preElement;
    private TagHelperContent _preContent;
    private TagHelperContent _content;
    private TagHelperContent _postContent;
    private TagHelperContent _postElement;
    private bool _wasSuppressOutputCalled;

    // Internal for testing
    internal TagHelperOutput(string tagName)
        : this(
            tagName,
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()))
    {
    }

    /// <summary>
    /// Instantiates a new instance of <see cref="TagHelperOutput"/>.
    /// </summary>
    /// <param name="tagName">The HTML element's tag name.</param>
    /// <param name="attributes">The HTML attributes.</param>
    /// <param name="getChildContentAsync">
    /// A delegate used to execute children asynchronously with the given <see cref="HtmlEncoder"/> in scope and
    /// return their rendered content.
    /// </param>
    public TagHelperOutput(
        string tagName,
        TagHelperAttributeList attributes,
        Func<bool, HtmlEncoder, Task<TagHelperContent>> getChildContentAsync)
    {
        ArgumentNullException.ThrowIfNull(getChildContentAsync);
        ArgumentNullException.ThrowIfNull(attributes);

        TagName = tagName;
        _getChildContentAsync = getChildContentAsync;
        Attributes = attributes;
    }

    /// <summary>
    /// The HTML element's tag name.
    /// </summary>
    /// <remarks>
    /// A whitespace or <c>null</c> value results in no start or end tag being rendered.
    /// </remarks>
    public string TagName { get; set; }

    /// <summary>
    /// Content that precedes the HTML element.
    /// </summary>
    /// <remarks>Value is rendered before the HTML element.</remarks>
    public TagHelperContent PreElement
    {
        get
        {
            if (_preElement == null)
            {
                _preElement = new DefaultTagHelperContent();
            }

            return _preElement;
        }
    }

    /// <summary>
    /// The HTML element's pre content.
    /// </summary>
    /// <remarks>Value is prepended to the <see cref="ITagHelper"/>'s final output.</remarks>
    public TagHelperContent PreContent
    {
        get
        {
            if (_preContent == null)
            {
                _preContent = new DefaultTagHelperContent();
            }

            return _preContent;
        }
    }

    /// <summary>
    /// Get or set the HTML element's main content.
    /// </summary>
    /// <remarks>Value occurs in the <see cref="ITagHelper"/>'s final output after <see cref="PreContent"/> and
    /// before <see cref="PostContent"/></remarks>
    public TagHelperContent Content
    {
        get
        {
            if (_content == null)
            {
                _content = new DefaultTagHelperContent();
            }

            return _content;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _content = value;
        }
    }

    /// <summary>
    /// The HTML element's post content.
    /// </summary>
    /// <remarks>Value is appended to the <see cref="ITagHelper"/>'s final output.</remarks>
    public TagHelperContent PostContent
    {
        get
        {
            if (_postContent == null)
            {
                _postContent = new DefaultTagHelperContent();
            }

            return _postContent;
        }
    }

    /// <summary>
    /// Content that follows the HTML element.
    /// </summary>
    /// <remarks>Value is rendered after the HTML element.</remarks>
    public TagHelperContent PostElement
    {
        get
        {
            if (_postElement == null)
            {
                _postElement = new DefaultTagHelperContent();
            }

            return _postElement;
        }
    }

    /// <summary>
    /// <c>true</c> if <see cref="Content"/> has been set, <c>false</c> otherwise.
    /// </summary>
    public bool IsContentModified
    {
        get
        {
            return _wasSuppressOutputCalled || _content?.IsModified == true;
        }
    }

    /// <summary>
    /// Syntax of the element in the generated HTML.
    /// </summary>
    public TagMode TagMode { get; set; }

    /// <summary>
    /// The HTML element's attributes.
    /// </summary>
    /// <remarks>
    /// MVC will HTML encode <see cref="string"/> values when generating the start tag. It will not HTML encode
    /// a <c>Microsoft.AspNetCore.Mvc.Rendering.HtmlString</c> instance. MVC converts most other types to a
    /// <see cref="string"/>, then HTML encodes the result.
    /// </remarks>
    public TagHelperAttributeList Attributes { get; }

    /// <summary>
    /// Clears the <see cref="TagHelperOutput"/> and updates its state with the provided values.
    /// </summary>
    /// <param name="tagName">The tag name to use.</param>
    /// <param name="tagMode">The <see cref="TagMode"/> to use.</param>
    public void Reinitialize(string tagName, TagMode tagMode)
    {
        TagName = tagName;
        TagMode = tagMode;
        Attributes.Clear();

        _preElement?.Reinitialize();
        _preContent?.Reinitialize();
        _content?.Reinitialize();
        _postContent?.Reinitialize();
        _postElement?.Reinitialize();

        _wasSuppressOutputCalled = false;
    }

    /// <summary>
    /// Changes <see cref="TagHelperOutput"/> to generate nothing.
    /// </summary>
    /// <remarks>
    /// Sets <see cref="TagName"/> to <c>null</c>, and clears <see cref="PreElement"/>, <see cref="PreContent"/>,
    /// <see cref="Content"/>, <see cref="PostContent"/>, and <see cref="PostElement"/> to suppress output.
    /// </remarks>
    public void SuppressOutput()
    {
        TagName = null;
        _wasSuppressOutputCalled = true;
        _preElement?.Clear();
        _preContent?.Clear();
        _content?.Clear();
        _postContent?.Clear();
        _postElement?.Clear();
    }

    /// <summary>
    /// Executes children asynchronously and returns their rendered content.
    /// </summary>
    /// <returns>A <see cref="Task"/> that on completion returns content rendered by children.</returns>
    /// <remarks>
    /// This method is memoized. Multiple calls will not cause children to re-execute with the page's original
    /// <see cref="HtmlEncoder"/>.
    /// </remarks>
    public Task<TagHelperContent> GetChildContentAsync()
    {
        return GetChildContentAsync(useCachedResult: true, encoder: null);
    }

    /// <summary>
    /// Executes children asynchronously and returns their rendered content.
    /// </summary>
    /// <param name="useCachedResult">
    /// If <c>true</c>, multiple calls will not cause children to re-execute with the page's original
    /// <see cref="HtmlEncoder"/>; returns cached content.
    /// </param>
    /// <returns>A <see cref="Task"/> that on completion returns content rendered by children.</returns>
    public Task<TagHelperContent> GetChildContentAsync(bool useCachedResult)
    {
        return GetChildContentAsync(useCachedResult, encoder: null);
    }

    /// <summary>
    /// Executes children asynchronously with the given <paramref name="encoder"/> in scope and returns their
    /// rendered content.
    /// </summary>
    /// <param name="encoder">
    /// The <see cref="HtmlEncoder"/> to use when the page handles non-<see cref="IHtmlContent"/> C# expressions.
    /// If <c>null</c>, executes children with the page's current <see cref="HtmlEncoder"/>.
    /// </param>
    /// <returns>A <see cref="Task"/> that on completion returns content rendered by children.</returns>
    /// <remarks>
    /// This method is memoized. Multiple calls with the same <see cref="HtmlEncoder"/> instance will not cause
    /// children to re-execute with that encoder in scope.
    /// </remarks>
    public Task<TagHelperContent> GetChildContentAsync(HtmlEncoder encoder)
    {
        return GetChildContentAsync(useCachedResult: true, encoder: encoder);
    }

    /// <summary>
    /// Executes children asynchronously with the given <paramref name="encoder"/> in scope and returns their
    /// rendered content.
    /// </summary>
    /// <param name="useCachedResult">
    /// If <c>true</c>, multiple calls with the same <see cref="HtmlEncoder"/> will not cause children to
    /// re-execute; returns cached content.
    /// </param>
    /// <param name="encoder">
    /// The <see cref="HtmlEncoder"/> to use when the page handles non-<see cref="IHtmlContent"/> C# expressions.
    /// If <c>null</c>, executes children with the page's current <see cref="HtmlEncoder"/>.
    /// </param>
    /// <returns>A <see cref="Task"/> that on completion returns content rendered by children.</returns>
    public Task<TagHelperContent> GetChildContentAsync(bool useCachedResult, HtmlEncoder encoder)
    {
        return _getChildContentAsync(useCachedResult, encoder);
    }

    void IHtmlContentContainer.CopyTo(IHtmlContentBuilder destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        _preElement?.CopyTo(destination);

        var isTagNameNullOrWhitespace = string.IsNullOrWhiteSpace(TagName);

        if (!isTagNameNullOrWhitespace)
        {
            destination.AppendHtml("<");
            destination.AppendHtml(TagName);

            // Perf: Avoid allocating enumerator, cache .Count as it goes via interface
            var count = Attributes.Count;
            for (var i = 0; i < count; i++)
            {
                var attribute = Attributes[i];
                destination.AppendHtml(" ");
                attribute.CopyTo(destination);
            }

            if (TagMode == TagMode.SelfClosing)
            {
                destination.AppendHtml(" /");
            }

            destination.AppendHtml(">");
        }

        if (isTagNameNullOrWhitespace || TagMode == TagMode.StartTagAndEndTag)
        {
            _preContent?.CopyTo(destination);

            _content?.CopyTo(destination);

            _postContent?.CopyTo(destination);
        }

        if (!isTagNameNullOrWhitespace && TagMode == TagMode.StartTagAndEndTag)
        {
            destination.AppendHtml("</");
            destination.AppendHtml(TagName);
            destination.AppendHtml(">");
        }

        _postElement?.CopyTo(destination);
    }

    void IHtmlContentContainer.MoveTo(IHtmlContentBuilder destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        _preElement?.MoveTo(destination);

        var isTagNameNullOrWhitespace = string.IsNullOrWhiteSpace(TagName);

        if (!isTagNameNullOrWhitespace)
        {
            destination.AppendHtml("<");
            destination.AppendHtml(TagName);

            // Perf: Avoid allocating enumerator, cache .Count as it goes via interface
            var count = Attributes.Count;
            for (var i = 0; i < count; i++)
            {
                var attribute = Attributes[i];
                destination.AppendHtml(" ");
                attribute.MoveTo(destination);
            }

            if (TagMode == TagMode.SelfClosing)
            {
                destination.AppendHtml(" /");
            }

            destination.AppendHtml(">");
        }

        if (isTagNameNullOrWhitespace || TagMode == TagMode.StartTagAndEndTag)
        {
            _preContent?.MoveTo(destination);
            _content?.MoveTo(destination);
            _postContent?.MoveTo(destination);
        }

        if (!isTagNameNullOrWhitespace && TagMode == TagMode.StartTagAndEndTag)
        {
            destination.AppendHtml("</");
            destination.AppendHtml(TagName);
            destination.AppendHtml(">");
        }

        _postElement?.MoveTo(destination);

        // Depending on the code path we took, these might need to be cleared.
        _preContent?.Clear();
        _content?.Clear();
        _postContent?.Clear();
        Attributes.Clear();
    }

    /// <inheritdoc />
    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(encoder);

        _preElement?.WriteTo(writer, encoder);

        var isTagNameNullOrWhitespace = string.IsNullOrWhiteSpace(TagName);

        if (!isTagNameNullOrWhitespace)
        {
            writer.Write("<");
            writer.Write(TagName);

            // Perf: Avoid allocating enumerator, cache .Count as it goes via interface
            var count = Attributes.Count;
            for (var i = 0; i < count; i++)
            {
                var attribute = Attributes[i];
                writer.Write(" ");
                attribute.WriteTo(writer, encoder);
            }

            if (TagMode == TagMode.SelfClosing)
            {
                writer.Write(" /");
            }

            writer.Write(">");
        }

        if (isTagNameNullOrWhitespace || TagMode == TagMode.StartTagAndEndTag)
        {
            _preContent?.WriteTo(writer, encoder);

            _content?.WriteTo(writer, encoder);

            _postContent?.WriteTo(writer, encoder);
        }

        if (!isTagNameNullOrWhitespace && TagMode == TagMode.StartTagAndEndTag)
        {
            writer.Write("</");
            writer.Write(TagName);
            writer.Write(">");
        }

        _postElement?.WriteTo(writer, encoder);
    }
}
