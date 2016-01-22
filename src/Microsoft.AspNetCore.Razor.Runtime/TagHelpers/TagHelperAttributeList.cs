// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Runtime;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// A collection of <see cref="TagHelperAttribute"/>s.
    /// </summary>
    public class TagHelperAttributeList : ReadOnlyTagHelperAttributeList<TagHelperAttribute>, IList<TagHelperAttribute>
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
            : base(attributes)
        {
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }
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
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value.Name == null)
                {
                    throw new ArgumentException(
                        Resources.FormatTagHelperAttributeList_CannotAddWithNullName(
                            typeof(TagHelperAttribute).FullName,
                            nameof(TagHelperAttribute.Name)),
                        nameof(value));
                }

                Attributes[index] = value;
            }
        }

        /// <summary>
        /// Gets the first <see cref="TagHelperAttribute"/> with <see cref="TagHelperAttribute.Name"/> matching
        /// <paramref name="name"/>. When setting, replaces the first matching
        /// <see cref="TagHelperAttribute"/> with the specified <paramref name="value"/> and removes any additional
        /// matching <see cref="TagHelperAttribute"/>s. If a matching <see cref="TagHelperAttribute"/> is not found,
        /// adds the specified <paramref name="value"/> to the end of the collection.
        /// </summary>
        /// <param name="name">
        /// The <see cref="TagHelperAttribute.Name"/> of the <see cref="TagHelperAttribute"/> to get or set.
        /// </param>
        /// <returns>The first <see cref="TagHelperAttribute"/> with <see cref="TagHelperAttribute.Name"/> matching
        /// <paramref name="name"/>.
        /// </returns>
        /// <remarks><paramref name="name"/> is compared case-insensitively. When setting,
        /// <see cref="TagHelperAttribute"/>s <see cref="TagHelperAttribute.Name"/> must be <c>null</c> or
        /// case-insensitively match the specified <paramref name="name"/>.</remarks>
        /// <example>
        /// <code>
        /// var attributes = new TagHelperAttributeList();
        ///
        /// // Will "value" be converted to a TagHelperAttribute with a null Name
        /// attributes["name"] = "value";
        ///
        /// // TagHelperAttribute.Name must match the specified name.
        /// attributes["name"] = new TagHelperAttribute("name", "value");
        /// </code>
        /// </example>
        public new TagHelperAttribute this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                return base[name];
            }
            set
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // Name will be null if user attempts to set the attribute via an implicit conversion:
                // output.Attributes["someName"] = "someValue"
                if (value.Name == null)
                {
                    value.Name = name;
                }
                else if (!NameEquals(name, value))
                {
                    throw new ArgumentException(
                        Resources.FormatTagHelperAttributeList_CannotAddAttribute(
                            nameof(TagHelperAttribute),
                            nameof(TagHelperAttribute.Name),
                            value.Name,
                            name),
                        nameof(name));
                }

                var attributeReplaced = false;

                // Perf: Avoid allocating enumerator
                for (var i = 0; i < Attributes.Count; i++)
                {
                    if (NameEquals(name, Attributes[i]))
                    {
                        // We replace the first attribute with the provided value, remove all the rest.
                        if (!attributeReplaced)
                        {
                            // We replace the first attribute we find with the same name.
                            Attributes[i] = value;
                            attributeReplaced = true;
                        }
                        else
                        {
                            Attributes.RemoveAt(i--);
                        }
                    }
                }

                // If we didn't replace an attribute value we should add value to the end of the collection.
                if (!attributeReplaced)
                {
                    Add(value);
                }
            }
        }

        /// <inheritdoc />
        bool ICollection<TagHelperAttribute>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Adds a <see cref="TagHelperAttribute"/> to the end of the collection with the specified
        /// <paramref name="name"/> and <paramref name="value"/>.
        /// </summary>
        /// <param name="name">The <see cref="TagHelperAttribute.Name"/> of the attribute to add.</param>
        /// <param name="value">The <see cref="TagHelperAttribute.Value"/> of the attribute to add.</param>
        public void Add(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Attributes.Add(new TagHelperAttribute(name, value));
        }

        /// <inheritdoc />
        /// <remarks>
        /// <paramref name="attribute"/>'s <see cref="TagHelperAttribute.Name"/> must not be <c>null</c>.
        /// </remarks>
        public void Add(TagHelperAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            if (attribute.Name == null)
            {
                throw new ArgumentException(
                    Resources.FormatTagHelperAttributeList_CannotAddWithNullName(
                        typeof(TagHelperAttribute).FullName,
                        nameof(TagHelperAttribute.Name)),
                    nameof(attribute));
            }

            Attributes.Add(attribute);
        }

        /// <inheritdoc />
        /// <remarks>
        /// <paramref name="attribute"/>'s <see cref="TagHelperAttribute.Name"/> must not be <c>null</c>.
        /// </remarks>
        public void Insert(int index, TagHelperAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            if (attribute.Name == null)
            {
                throw new ArgumentException(
                    Resources.FormatTagHelperAttributeList_CannotAddWithNullName(
                        typeof(TagHelperAttribute).FullName,
                        nameof(TagHelperAttribute.Name)),
                    nameof(attribute));
            }

            Attributes.Insert(index, attribute);
        }

        /// <inheritdoc />
        public void CopyTo(TagHelperAttribute[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            Attributes.CopyTo(array, index);
        }

        /// <inheritdoc />
        /// <remarks>
        /// <paramref name="attribute"/>s <see cref="TagHelperAttribute.Name"/> is compared case-insensitively.
        /// </remarks>
        public bool Remove(TagHelperAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            return Attributes.Remove(attribute);
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            Attributes.RemoveAt(index);
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
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            // Perf: Avoid allocating enumerator
            var removedAtLeastOne = false;
            for (var i = Attributes.Count - 1; i >= 0; i--)
            {
                if (NameEquals(name, Attributes[i]))
                {
                    Attributes.RemoveAt(i);
                    removedAtLeastOne = true;
                }
            }

            return removedAtLeastOne;
        }

        /// <inheritdoc />
        public void Clear()
        {
            Attributes.Clear();
        }
    }
}