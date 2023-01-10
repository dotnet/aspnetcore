// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// A collection of <see cref="TagHelperAttribute"/>s.
/// </summary>
public class TagHelperAttributeList : ReadOnlyTagHelperAttributeList, IList<TagHelperAttribute>
{
    /// <summary>
    /// Instantiates a new instance of <see cref="TagHelperAttributeList"/> with an empty collection.
    /// </summary>
    public TagHelperAttributeList()
        : base()
    {
    }

    /// <summary>
    /// Instantiates a new instance of <see cref="TagHelperAttributeList"/> with the specified
    /// <paramref name="attributes"/>.
    /// </summary>
    /// <param name="attributes">The collection to wrap.</param>
    public TagHelperAttributeList(IEnumerable<TagHelperAttribute> attributes)
        : base(new List<TagHelperAttribute>(attributes))
    {
        ArgumentNullException.ThrowIfNull(attributes);
    }

    /// <summary>
    /// Instantiates a new instance of <see cref="TagHelperAttributeList"/> with the specified
    /// <paramref name="attributes"/>.
    /// </summary>
    /// <param name="attributes">The collection to wrap.</param>
    public TagHelperAttributeList(List<TagHelperAttribute> attributes)
        : base(attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <paramref name="value"/>'s <see cref="TagHelperAttribute.Name"/> must not be <c>null</c>.
    /// </remarks>
    public new TagHelperAttribute this[int index]
    {
        get
        {
            return base[index];
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            Items[index] = value;
        }
    }

    /// <summary>
    /// Replaces the first <see cref="TagHelperAttribute"/> with <see cref="TagHelperAttribute.Name"/> matching
    /// <paramref name="name"/> and removes any additional matching <see cref="TagHelperAttribute"/>s. If a
    /// matching <see cref="TagHelperAttribute"/> is not found, adds a <see cref="TagHelperAttribute"/> with
    /// <paramref name="name"/> and <paramref name="value"/> to the end of the collection.</summary>
    /// <param name="name">
    /// The <see cref="TagHelperAttribute.Name"/> of the <see cref="TagHelperAttribute"/> to set.
    /// </param>
    /// <param name="value">
    /// The <see cref="TagHelperAttribute.Value"/> to set.
    /// </param>
    /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
    public void SetAttribute(string name, object value)
    {
        var attribute = new TagHelperAttribute(name, value);
        SetAttribute(attribute);
    }

    /// <summary>
    /// Replaces the first <see cref="TagHelperAttribute"/> with <see cref="TagHelperAttribute.Name"/> matching
    /// <paramref name="attribute"/>'s <see cref="TagHelperAttribute.Name"/> and removes any additional matching
    /// <see cref="TagHelperAttribute"/>s. If a matching <see cref="TagHelperAttribute"/> is not found, adds the
    /// specified <paramref name="attribute"/> to the end of the collection.
    /// </summary>
    /// <param name="attribute">
    /// The <see cref="TagHelperAttribute"/> to set.
    /// </param>
    /// <remarks><paramref name="attribute"/>'s <see cref="TagHelperAttribute.Name"/> is compared
    /// case-insensitively.</remarks>
    public void SetAttribute(TagHelperAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        var attributeReplaced = false;

        // Perf: Avoid allocating enumerator
        for (var i = 0; i < Items.Count; i++)
        {
            if (NameEquals(attribute.Name, Items[i]))
            {
                // We replace the first attribute with the provided attribute, remove all the rest.
                if (!attributeReplaced)
                {
                    // We replace the first attribute we find with the same name.
                    Items[i] = attribute;
                    attributeReplaced = true;
                }
                else
                {
                    Items.RemoveAt(i--);
                }
            }
        }

        // If we didn't replace an attribute value we should add value to the end of the collection.
        if (!attributeReplaced)
        {
            Add(attribute);
        }
    }

    /// <inheritdoc />
    bool ICollection<TagHelperAttribute>.IsReadOnly => false;

    /// <summary>
    /// Adds a <see cref="TagHelperAttribute"/> to the end of the collection with the specified
    /// <paramref name="name"/> and <paramref name="value"/>.
    /// </summary>
    /// <param name="name">The <see cref="TagHelperAttribute.Name"/> of the attribute to add.</param>
    /// <param name="value">The <see cref="TagHelperAttribute.Value"/> of the attribute to add.</param>
    public void Add(string name, object value)
    {
        var attribute = new TagHelperAttribute(name, value);
        Items.Add(attribute);
    }

    /// <inheritdoc />
    public void Add(TagHelperAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        Items.Add(attribute);
    }

    /// <inheritdoc />
    public void Insert(int index, TagHelperAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        Items.Insert(index, attribute);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <paramref name="attribute"/>s <see cref="TagHelperAttribute.Name"/> is compared case-insensitively.
    /// </remarks>
    public bool Remove(TagHelperAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        return Items.Remove(attribute);
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        Items.RemoveAt(index);
    }

    /// <summary>
    /// Removes all <see cref="TagHelperAttribute"/>s with <see cref="TagHelperAttribute.Name"/> matching
    /// <paramref name="name"/>.
    /// </summary>
    /// <param name="name">
    /// The <see cref="TagHelperAttribute.Name"/> of <see cref="TagHelperAttribute"/>s to remove.
    /// </param>
    /// <returns>
    /// <c>true</c> if at least 1 <see cref="TagHelperAttribute"/> was removed; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
    public bool RemoveAll(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        // Perf: Avoid allocating enumerator
        var removedAtLeastOne = false;
        for (var i = Items.Count - 1; i >= 0; i--)
        {
            if (NameEquals(name, Items[i]))
            {
                Items.RemoveAt(i);
                removedAtLeastOne = true;
            }
        }

        return removedAtLeastOne;
    }

    /// <inheritdoc />
    public void Clear()
    {
        Items.Clear();
    }
}
