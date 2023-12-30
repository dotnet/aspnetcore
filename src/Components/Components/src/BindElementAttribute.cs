// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Configures options for binding specific element types.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class BindElementAttribute : Attribute
{
    /// <summary>
    /// Constructs an instance of <see cref="BindElementAttribute"/>.
    /// </summary>
    /// <param name="element">The tag name of the element.</param>
    /// <param name="suffix">The suffix value. For example, set this to <c>value</c> for <c>bind-value</c>, or set this to <see langword="null" /> for <c>bind</c>.</param>
    /// <param name="valueAttribute">The name of the value attribute to be bound.</param>
    /// <param name="changeAttribute">The name of an attribute that will register an associated change event.</param>
    public BindElementAttribute(string element, string? suffix, string valueAttribute, string changeAttribute)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(valueAttribute);
        ArgumentNullException.ThrowIfNull(changeAttribute);

        Element = element;
        ValueAttribute = valueAttribute;
        ChangeAttribute = changeAttribute;
        Suffix = suffix;
    }

    /// <summary>
    /// Gets the tag name of the element.
    /// </summary>
    public string Element { get; }

    /// <summary>
    /// Gets the suffix value.
    /// For example, this will be <c>value</c> to mean <c>bind-value</c>, or <see langword="null" /> to mean <c>bind</c>.
    /// </summary>
    public string? Suffix { get; }

    /// <summary>
    /// Gets the name of the value attribute to be bound.
    /// </summary>
    public string ValueAttribute { get; }

    /// <summary>
    /// Gets the name of an attribute that will register an associated change event.
    /// </summary>
    public string ChangeAttribute { get; }
}
