// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Used to override an <see cref="ITagHelper"/> property's HTML attribute name.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class HtmlAttributeNameAttribute : Attribute
{
    private string _dictionaryAttributePrefix;

    /// <summary>
    /// Instantiates a new instance of the <see cref="HtmlAttributeNameAttribute"/> class with <see cref="Name"/>
    /// equal to <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Associated property must not have a public setter and must be compatible with
    /// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> where <c>TKey</c> is
    /// <see cref="string"/>.
    /// </remarks>
    public HtmlAttributeNameAttribute()
    {
    }

    /// <summary>
    /// Instantiates a new instance of the <see cref="HtmlAttributeNameAttribute"/> class.
    /// </summary>
    /// <param name="name">
    /// HTML attribute name for the associated property. Must be <c>null</c> or empty if associated property does
    /// not have a public setter and is compatible with
    /// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> where <c>TKey</c> is
    /// <see cref="string"/>. Otherwise must not be <c>null</c> or empty.
    /// </param>
    public HtmlAttributeNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// HTML attribute name of the associated property.
    /// </summary>
    /// <value>
    /// <c>null</c> or empty if and only if associated property does not have a public setter and is compatible
    /// with <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> where <c>TKey</c> is
    /// <see cref="string"/>.
    /// </value>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the prefix used to match HTML attribute names. Matching attributes are added to the
    /// associated property (an <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>).
    /// </summary>
    /// <remarks>
    /// If non-<c>null</c> associated property must be compatible with
    /// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> where <c>TKey</c> is
    /// <see cref="string"/>.
    /// </remarks>
    /// <value>
    /// <para>
    /// If associated property is compatible with
    /// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>, default value is <c>Name + "-"</c>.
    /// <see cref="Name"/> must not be <c>null</c> or empty in this case.
    /// </para>
    /// <para>
    /// Otherwise default value is <c>null</c>.
    /// </para>
    /// </value>
    public string DictionaryAttributePrefix
    {
        get
        {
            return _dictionaryAttributePrefix;
        }
        set
        {
            _dictionaryAttributePrefix = value;
            DictionaryAttributePrefixSet = true;
        }
    }

    /// <summary>
    /// Gets an indication whether <see cref="DictionaryAttributePrefix"/> has been set. Used to distinguish an
    /// uninitialized <see cref="DictionaryAttributePrefix"/> value from an explicit <c>null</c> setting.
    /// </summary>
    /// <value><c>true</c> if <see cref="DictionaryAttributePrefix"/> was set. <c>false</c> otherwise.</value>
    public bool DictionaryAttributePrefixSet { get; private set; }
}
