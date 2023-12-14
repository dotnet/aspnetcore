// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Html;

/// <summary>
/// Extension methods for <see cref="IHtmlContentBuilder"/>.
/// </summary>
public static class HtmlContentBuilderExtensions
{
    /// <summary>
    /// Appends the specified <paramref name="format"/> to the existing content after replacing each format
    /// item with the HTML encoded <see cref="string"/> representation of the corresponding item in the
    /// <paramref name="args"/> array.
    /// </summary>
    /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
    /// <param name="format">
    /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
    /// The format string is assumed to be HTML encoded as-provided, and no further encoding will be performed.
    /// </param>
    /// <param name="args">
    /// The object array to format. Each element in the array will be formatted and then HTML encoded.
    /// </param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    public static IHtmlContentBuilder AppendFormat(
        this IHtmlContentBuilder builder,
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(args);

        builder.AppendHtml(new HtmlFormattableString(format, args));
        return builder;
    }

    /// <summary>
    /// Appends the specified <paramref name="format"/> to the existing content with information from the
    /// <paramref name="formatProvider"/> after replacing each format item with the HTML encoded
    /// <see cref="string"/> representation of the corresponding item in the <paramref name="args"/> array.
    /// </summary>
    /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <param name="format">
    /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
    /// The format string is assumed to be HTML encoded as-provided, and no further encoding will be performed.
    /// </param>
    /// <param name="args">
    /// The object array to format. Each element in the array will be formatted and then HTML encoded.
    /// </param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    public static IHtmlContentBuilder AppendFormat(
        this IHtmlContentBuilder builder,
        IFormatProvider formatProvider,
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(args);

        builder.AppendHtml(new HtmlFormattableString(formatProvider, format, args));
        return builder;
    }

    /// <summary>
    /// Appends an <see cref="Environment.NewLine"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
    /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
    public static IHtmlContentBuilder AppendLine(this IHtmlContentBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AppendHtml(HtmlString.NewLine);
        return builder;
    }

    /// <summary>
    /// Appends an <see cref="Environment.NewLine"/> after appending the <see cref="string"/> value.
    /// The value is treated as unencoded as-provided, and will be HTML encoded before writing to output.
    /// </summary>
    /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
    /// <param name="unencoded">The <see cref="string"/> to append.</param>
    /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
    public static IHtmlContentBuilder AppendLine(this IHtmlContentBuilder builder, string unencoded)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Append(unencoded);
        builder.AppendHtml(HtmlString.NewLine);
        return builder;
    }

    /// <summary>
    /// Appends an <see cref="Environment.NewLine"/> after appending the <see cref="IHtmlContent"/> value.
    /// </summary>
    /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
    /// <param name="content">The <see cref="IHtmlContent"/> to append.</param>
    /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
    public static IHtmlContentBuilder AppendLine(this IHtmlContentBuilder builder, IHtmlContent content)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AppendHtml(content);
        builder.AppendHtml(HtmlString.NewLine);
        return builder;
    }

    /// <summary>
    /// Appends an <see cref="Environment.NewLine"/> after appending the <see cref="string"/> value.
    /// The value is treated as HTML encoded as-provided, and no further encoding will be performed.
    /// </summary>
    /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
    /// <param name="encoded">The HTML encoded <see cref="string"/> to append.</param>
    /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
    public static IHtmlContentBuilder AppendHtmlLine(this IHtmlContentBuilder builder, string encoded)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AppendHtml(encoded);
        builder.AppendHtml(HtmlString.NewLine);
        return builder;
    }

    /// <summary>
    /// Sets the content to the <see cref="string"/> value. The value is treated as unencoded as-provided,
    /// and will be HTML encoded before writing to output.
    /// </summary>
    /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
    /// <param name="unencoded">The <see cref="string"/> value that replaces the content.</param>
    /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
    public static IHtmlContentBuilder SetContent(this IHtmlContentBuilder builder, string unencoded)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Clear();
        builder.Append(unencoded);
        return builder;
    }

    /// <summary>
    /// Sets the content to the <see cref="IHtmlContent"/> value.
    /// </summary>
    /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
    /// <param name="content">The <see cref="IHtmlContent"/> value that replaces the content.</param>
    /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
    public static IHtmlContentBuilder SetHtmlContent(this IHtmlContentBuilder builder, IHtmlContent content)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Clear();
        builder.AppendHtml(content);
        return builder;
    }

    /// <summary>
    /// Sets the content to the <see cref="string"/> value. The value is treated as HTML encoded as-provided, and
    /// no further encoding will be performed.
    /// </summary>
    /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
    /// <param name="encoded">The HTML encoded <see cref="string"/> that replaces the content.</param>
    /// <returns>The <see cref="IHtmlContentBuilder"/>.</returns>
    public static IHtmlContentBuilder SetHtmlContent(this IHtmlContentBuilder builder, string encoded)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Clear();
        builder.AppendHtml(encoded);
        return builder;
    }
}
