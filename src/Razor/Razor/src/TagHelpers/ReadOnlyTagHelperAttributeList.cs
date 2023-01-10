// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// A read-only collection of <see cref="TagHelperAttribute"/>s.
/// </summary>
public abstract class ReadOnlyTagHelperAttributeList : ReadOnlyCollection<TagHelperAttribute>
{
    /// <summary>
    /// Instantiates a new instance of <see cref="ReadOnlyTagHelperAttributeList"/> with an empty
    /// collection.
    /// </summary>
    protected ReadOnlyTagHelperAttributeList()
        : base(new List<TagHelperAttribute>())
    {
    }

    /// <summary>
    /// Instantiates a new instance of <see cref="ReadOnlyTagHelperAttributeList"/> with the specified
    /// <paramref name="attributes"/>.
    /// </summary>
    /// <param name="attributes">The collection to wrap.</param>
    public ReadOnlyTagHelperAttributeList(IList<TagHelperAttribute> attributes)
        : base(attributes)
    {
    }

    /// <summary>
    /// Gets the first <see cref="TagHelperAttribute"/> with <see cref="TagHelperAttribute.Name"/>
    /// matching <paramref name="name"/>.
    /// </summary>
    /// <param name="name">
    /// The <see cref="TagHelperAttribute.Name"/> of the <see cref="TagHelperAttribute"/> to get.
    /// </param>
    /// <returns>The first <see cref="TagHelperAttribute"/> with <see cref="TagHelperAttribute.Name"/>
    /// matching <paramref name="name"/>.
    /// </returns>
    /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
    public TagHelperAttribute this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            // Perf: Avoid allocating enumerator
            var items = Items;
            // Read interface .Count once rather than per iteration
            var itemsCount = items.Count;
            for (var i = 0; i < itemsCount; i++)
            {
                var attribute = items[i];
                if (NameEquals(name, attribute))
                {
                    return attribute;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Determines whether a <see cref="TagHelperAttribute"/> with <see cref="TagHelperAttribute.Name"/>
    /// matching <paramref name="name"/> exists in the collection.
    /// </summary>
    /// <param name="name">The <see cref="TagHelperAttribute.Name"/> of the
    /// <see cref="TagHelperAttribute"/> to get.</param>
    /// <returns>
    /// <c>true</c> if a <see cref="TagHelperAttribute"/> with the same
    /// <see cref="TagHelperAttribute.Name"/> exists in the collection; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
    public bool ContainsName(string name)
    {
        return this[name] != null;
    }

    /// <summary>
    /// Retrieves the first <see cref="TagHelperAttribute"/> with <see cref="TagHelperAttribute.Name"/>
    /// matching <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The <see cref="TagHelperAttribute.Name"/> of the
    /// <see cref="TagHelperAttribute"/> to get.</param>
    /// <param name="attribute">When this method returns, the first <see cref="TagHelperAttribute"/> with
    /// <see cref="TagHelperAttribute.Name"/> matching <paramref name="name"/>, if found; otherwise,
    /// <c>null</c>.</param>
    /// <returns><c>true</c> if a <see cref="TagHelperAttribute"/> with the same
    /// <see cref="TagHelperAttribute.Name"/> exists in the collection; otherwise, <c>false</c>.</returns>
    /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
    public bool TryGetAttribute(string name, out TagHelperAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(name);

        attribute = this[name];

        return attribute != null;
    }

    /// <summary>
    /// Retrieves <see cref="TagHelperAttribute"/>s in the collection with
    /// <see cref="TagHelperAttribute.Name"/> matching <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The <see cref="TagHelperAttribute.Name"/> of the
    /// <see cref="TagHelperAttribute"/>s to get.</param>
    /// <param name="attributes">When this method returns, the <see cref="TagHelperAttribute"/>s with
    /// <see cref="TagHelperAttribute.Name"/> matching <paramref name="name"/>.</param>
    /// <returns><c>true</c> if at least one <see cref="TagHelperAttribute"/> with the same
    /// <see cref="TagHelperAttribute.Name"/> exists in the collection; otherwise, <c>false</c>.</returns>
    /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
    public bool TryGetAttributes(string name, out IReadOnlyList<TagHelperAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(name);

        // Perf: Avoid allocating enumerator
        List<TagHelperAttribute> matchedAttributes = null;
        var items = Items;
        // Read interface .Count once rather than per iteration
        var itemsCount = items.Count;
        for (var i = 0; i < itemsCount; i++)
        {
            var attribute = items[i];
            if (NameEquals(name, attribute))
            {
                if (matchedAttributes == null)
                {
                    matchedAttributes = new List<TagHelperAttribute>();
                }

                matchedAttributes.Add(attribute);
            }
        }
        attributes = matchedAttributes ?? (IReadOnlyList<TagHelperAttribute>)Array.Empty<TagHelperAttribute>();

        return matchedAttributes != null;
    }

    /// <summary>
    /// Searches for a <see cref="TagHelperAttribute"/> who's <see cref="TagHelperAttribute.Name"/>
    /// case-insensitively matches <paramref name="name"/> and returns the zero-based index of the first
    /// occurrence.
    /// </summary>
    /// <param name="name">The <see cref="TagHelperAttribute.Name"/> to locate in the collection.</param>
    /// <returns>The zero-based index of the first matching <see cref="TagHelperAttribute"/> within the collection,
    /// if found; otherwise, -1.</returns>
    public int IndexOfName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var items = Items;
        // Read interface .Count once rather than per iteration
        var itemsCount = items.Count;
        for (var i = 0; i < itemsCount; i++)
        {
            if (NameEquals(name, items[i]))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Determines if the specified <paramref name="attribute"/> has the same name as <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The value to compare against <paramref name="attribute"/>s
    /// <see cref="TagHelperAttribute.Name"/>.</param>
    /// <param name="attribute">The attribute to compare against.</param>
    /// <returns><c>true</c> if <paramref name="name"/> case-insensitively matches <paramref name="attribute"/>s
    /// <see cref="TagHelperAttribute.Name"/>.</returns>
    protected static bool NameEquals(string name, TagHelperAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        return string.Equals(name, attribute.Name, StringComparison.OrdinalIgnoreCase);
    }
}
