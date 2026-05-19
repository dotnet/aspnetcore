// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Abstract class used to buffer content returned by <see cref="ITagHelper"/>s.
/// </summary>
public abstract class TagHelperContent : IHtmlContentBuilder
{
    /// <summary>
    /// Gets a value indicating whether the content was modified.
    /// </summary>
    public abstract bool IsModified { get; }

    /// <summary>
    /// Gets a value indicating whether the content is empty or whitespace.
    /// </summary>
    public abstract bool IsEmptyOrWhiteSpace { get; }

    /// <summary>
    /// Sets the content.
    /// </summary>
    /// <param name="htmlContent">The <see cref="IHtmlContent"/> that replaces the content.</param>
    /// <returns>A reference to this instance after the set operation has completed.</returns>
    public TagHelperContent SetHtmlContent(IHtmlContent htmlContent)
    {
        HtmlContentBuilderExtensions.SetHtmlContent(this, htmlContent);
        return this;
    }

    /// <summary>
    /// Sets the content.
    /// </summary>
    /// <param name="unencoded">
    /// The <see cref="string"/> that replaces the content. The value is assume to be unencoded
    /// as-provided and will be HTML encoded before being written.
    /// </param>
    /// <returns>A reference to this instance after the set operation has completed.</returns>
    public TagHelperContent SetContent(string unencoded)
    {
        HtmlContentBuilderExtensions.SetContent(this, unencoded);
        return this;
    }

    /// <summary>
    /// Sets the content.
    /// </summary>
    /// <param name="encoded">
    /// The <see cref="string"/> that replaces the content. The value is assume to be HTML encoded
    /// as-provided and no further encoding will be performed.
    /// </param>
    /// <returns>A reference to this instance after the set operation has completed.</returns>
    public TagHelperContent SetHtmlContent(string encoded)
    {
        HtmlContentBuilderExtensions.SetHtmlContent(this, encoded);
        return this;
    }

    /// <summary>
    /// Appends <paramref name="unencoded"/> to the existing content.
    /// </summary>
    /// <param name="unencoded">The <see cref="string"/> to be appended.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    public abstract TagHelperContent Append(string unencoded);

    /// <summary>
    /// Appends <paramref name="htmlContent"/> to the existing content.
    /// </summary>
    /// <param name="htmlContent">The <see cref="IHtmlContent"/> to be appended.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    public abstract TagHelperContent AppendHtml(IHtmlContent htmlContent);

    /// <summary>
    /// Appends <paramref name="encoded"/> to the existing content. <paramref name="encoded"/> is assumed
    /// to be an HTML encoded <see cref="string"/> and no further encoding will be performed.
    /// </summary>
    /// <param name="encoded">The <see cref="string"/> to be appended.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    public abstract TagHelperContent AppendHtml(string encoded);

    /// <summary>
    /// Appends the specified <paramref name="format"/> to the existing content after
    /// replacing each format item with the HTML encoded <see cref="string"/> representation of the
    /// corresponding item in the <paramref name="args"/> array.
    /// </summary>
    /// <param name="format">
    /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
    /// </param>
    /// <param name="args">The object array to format.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    public TagHelperContent AppendFormat([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object[] args)
    {
        HtmlContentBuilderExtensions.AppendFormat(this, null, format, args);
        return this;
    }

    /// <summary>
    /// Appends the specified <paramref name="format"/> to the existing content with information from the
    /// <paramref name="provider"/> after replacing each format item with the HTML encoded <see cref="string"/>
    /// representation of the corresponding item in the <paramref name="args"/> array.
    /// </summary>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <param name="format">
    /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
    /// </param>
    /// <param name="args">The object array to format.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    public TagHelperContent AppendFormat(
        IFormatProvider provider,
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object[] args)
    {
        HtmlContentBuilderExtensions.AppendFormat(this, provider, format, args);
        return this;
    }

    /// <summary>
    /// Clears the content.
    /// </summary>
    /// <returns>A reference to this instance after the clear operation has completed.</returns>
    public abstract TagHelperContent Clear();

    /// <summary>
    /// Clears the <see cref="TagHelperContent"/>,  so it can be reused.
    /// </summary>
    /// <remarks>This method does more than what <see cref="Clear"/> does. It also resets the
    /// <see cref="IsModified"/> flag.</remarks>
    public abstract void Reinitialize();

    /// <inheritdoc />
    public abstract void CopyTo(IHtmlContentBuilder destination);

    /// <inheritdoc />
    public abstract void MoveTo(IHtmlContentBuilder destination);

    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <returns>A <see cref="string"/> containing the content.</returns>
    public abstract string GetContent();

    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <param name="encoder">The <see cref="HtmlEncoder"/>.</param>
    /// <returns>A <see cref="string"/> containing the content.</returns>
    public abstract string GetContent(HtmlEncoder encoder);

    /// <inheritdoc />
    public abstract void WriteTo(TextWriter writer, HtmlEncoder encoder);

    /// <inheritdoc />
    IHtmlContentBuilder IHtmlContentBuilder.AppendHtml(IHtmlContent content)
    {
        return AppendHtml(content);
    }

    /// <inheritdoc />
    IHtmlContentBuilder IHtmlContentBuilder.Append(string unencoded)
    {
        return Append(unencoded);
    }

    /// <inheritdoc />
    IHtmlContentBuilder IHtmlContentBuilder.AppendHtml(string encoded)
    {
        return AppendHtml(encoded);
    }

    /// <inheritdoc />
    IHtmlContentBuilder IHtmlContentBuilder.Clear()
    {
        return Clear();
    }
}
