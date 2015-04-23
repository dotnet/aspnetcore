// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
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
        public TagHelperAttributeList([NotNull] IEnumerable<TagHelperAttribute> attributes)
            : base(attributes)
        {
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
            [param: NotNull]
            set
            {
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
        public new TagHelperAttribute this[[NotNull] string name]
        {
            get
            {
                return base[name];
            }
            [param: NotNull]
            set
            {
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
        public void Add([NotNull] string name, object value)
        {
            Attributes.Add(new TagHelperAttribute(name, value));
        }

        /// <inheritdoc />
        /// <remarks>
        /// <paramref name="attribute"/>'s <see cref="TagHelperAttribute.Name"/> must not be <c>null</c>.
        /// </remarks>
        public void Add([NotNull] TagHelperAttribute attribute)
        {
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
        public void Insert(int index, [NotNull] TagHelperAttribute attribute)
        {
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
        public void CopyTo([NotNull] TagHelperAttribute[] array, int index)
        {
            Attributes.CopyTo(array, index);
        }

        /// <inheritdoc />
        /// <remarks>
        /// <paramref name="attribute"/>s <see cref="TagHelperAttribute.Name"/> is compared case-insensitively.
        /// </remarks>
        public bool Remove([NotNull] TagHelperAttribute attribute)
        {
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
        public bool RemoveAll([NotNull] string name)
        {
            return Attributes.RemoveAll(attribute => NameEquals(name, attribute)) > 0;
        }

        /// <inheritdoc />
        public void Clear()
        {
            Attributes.Clear();
        }
    }
}