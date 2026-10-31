// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Provides an <see cref="ITagHelper"/>'s target.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class HtmlTargetElementAttribute : Attribute
{
    /// <summary>
    /// The value for a tag helper that targets all HTML elements.
    /// </summary>
    public const string ElementCatchAllTarget = "*";

    /// <summary>
    /// Instantiates a new instance of the <see cref="HtmlTargetElementAttribute"/> class that targets all HTML
    /// elements with the required <see cref="Attributes"/>.
    /// </summary>
    /// <remarks><see cref="Tag"/> is set to <c>*</c>.</remarks>
    public HtmlTargetElementAttribute()
        : this(ElementCatchAllTarget)
    {
    }

    /// <summary>
    /// Instantiates a new instance of the <see cref="HtmlTargetElementAttribute"/> class with the given
    /// <paramref name="tag"/> as its <see cref="Tag"/> value.
    /// </summary>
    /// <param name="tag">
    /// The HTML tag the <see cref="ITagHelper"/> targets.
    /// </param>
    /// <remarks>A <c>*</c> <paramref name="tag"/> value indicates this <see cref="ITagHelper"/>
    /// targets all HTML elements with the required <see cref="Attributes"/>.</remarks>
    public HtmlTargetElementAttribute(string tag)
    {
        Tag = tag;
    }

    /// <summary>
    /// The HTML tag the <see cref="ITagHelper"/> targets. A <c>*</c> value indicates this <see cref="ITagHelper"/>
    /// targets all HTML elements with the required <see cref="Attributes"/>.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// A comma-separated <see cref="string"/> of attribute selectors the HTML element must match for the
    /// <see cref="ITagHelper"/> to run. <c>*</c> at the end of an attribute name acts as a prefix match. A value
    /// surrounded by square brackets is handled as a CSS attribute value selector. Operators <c>^=</c>, <c>$=</c> and
    /// <c>=</c> are supported e.g. <c>"name"</c>, <c>"[name]"</c>, <c>"[name=value]"</c>, <c>"[ name ^= 'value' ]"</c>.
    /// </summary>
    public string Attributes { get; set; }

    /// <summary>
    /// The expected tag structure. Defaults to <see cref="TagStructure.Unspecified"/>.
    /// </summary>
    /// <remarks>
    /// If <see cref="TagStructure.Unspecified"/> and no other tag helpers applying to the same element specify
    /// their <see cref="TagStructure"/> the <see cref="TagStructure.NormalOrSelfClosing"/> behavior is used:
    /// <para>
    /// <code>
    /// &lt;my-tag-helper&gt;&lt;/my-tag-helper&gt;
    /// &lt;!-- OR --&gt;
    /// &lt;my-tag-helper /&gt;
    /// </code>
    /// Otherwise, if another tag helper applying to the same element does specify their behavior, that behavior
    /// is used.
    /// </para>
    /// <para>
    /// If <see cref="TagStructure.WithoutEndTag"/> HTML elements can be written in the following formats:
    /// <code>
    /// &lt;my-tag-helper&gt;
    /// &lt;!-- OR --&gt;
    /// &lt;my-tag-helper /&gt;
    /// </code>
    /// </para>
    /// </remarks>
    public TagStructure TagStructure { get; set; }

    /// <summary>
    /// The required HTML element name of the direct parent. A <c>null</c> value indicates any HTML element name is
    /// allowed.
    /// </summary>
    public string ParentTag { get; set; }
}
