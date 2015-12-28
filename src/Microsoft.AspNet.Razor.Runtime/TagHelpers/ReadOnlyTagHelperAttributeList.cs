// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// A read-only collection of <typeparamref name="TAttribute"/>s.
    /// </summary>
    /// <typeparam name="TAttribute">
    /// The type of <see cref="IReadOnlyTagHelperAttribute"/>s in the collection.
    /// </typeparam>
    public class ReadOnlyTagHelperAttributeList<TAttribute> : IReadOnlyList<TAttribute>
        where TAttribute : IReadOnlyTagHelperAttribute
    {
        private static readonly IReadOnlyList<TAttribute> EmptyList =
#if NET451
            new TAttribute[0];
#else
            Array.Empty<TAttribute>();
#endif

        /// <summary>
        /// Instantiates a new instance of <see cref="ReadOnlyTagHelperAttributeList{TAttribute}"/> with an empty
        /// collection.
        /// </summary>
        protected ReadOnlyTagHelperAttributeList()
        {
            Attributes = new List<TAttribute>();
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="ReadOnlyTagHelperAttributeList{TAttribute}"/> with the specified
        /// <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The collection to wrap.</param>
        public ReadOnlyTagHelperAttributeList(IEnumerable<TAttribute> attributes)
        {
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            Attributes = new List<TAttribute>(attributes);
        }

        /// <summary>
        /// The underlying collection of <typeparamref name="TAttribute"/>s.
        /// </summary>
        /// <remarks>Intended for use in a non-read-only subclass. Changes to this <see cref="List{TAttribute}"/> will
        /// affect all getters that <see cref="ReadOnlyTagHelperAttributeList{TAttribute}"/> provides.</remarks>
        protected List<TAttribute> Attributes { get; }

        /// <inheritdoc />
        public TAttribute this[int index] => Attributes[index];

        /// <summary>
        /// Gets the first <typeparamref name="TAttribute"/> with <see cref="IReadOnlyTagHelperAttribute.Name"/>
        /// matching <paramref name="name"/>.
        /// </summary>
        /// <param name="name">
        /// The <see cref="IReadOnlyTagHelperAttribute.Name"/> of the <typeparamref name="TAttribute"/> to get.
        /// </param>
        /// <returns>The first <typeparamref name="TAttribute"/> with <see cref="IReadOnlyTagHelperAttribute.Name"/>
        /// matching <paramref name="name"/>.
        /// </returns>
        /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
        public TAttribute this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                // Perf: Avoid allocating enumerator
                for (var i = 0; i < Attributes.Count; i++)
                {
                    if (NameEquals(name, Attributes[i]))
                    {
                        return Attributes[i];
                    }
                }

                return default(TAttribute);
            }
        }

        /// <inheritdoc />
        public int Count => Attributes.Count;

        /// <summary>
        /// Determines whether a <typeparamref name="TAttribute"/> matching <paramref name="item"/> exists in the
        /// collection.
        /// </summary>
        /// <param name="item">The <typeparamref name="TAttribute"/> to locate.</param>
        /// <returns>
        /// <c>true</c> if an <typeparamref name="TAttribute"/> matching <paramref name="item"/> exists in the
        /// collection; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <paramref name="item"/>s <see cref="IReadOnlyTagHelperAttribute.Name"/> is compared case-insensitively.
        /// </remarks>
        public bool Contains(TAttribute item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return Attributes.Contains(item);
        }

        /// <summary>
        /// Determines whether a <typeparamref name="TAttribute"/> with the same
        /// <see cref="IReadOnlyTagHelperAttribute.Name"/> exists in the collection.
        /// </summary>
        /// <param name="name">The <see cref="IReadOnlyTagHelperAttribute.Name"/> of the
        /// <typeparamref name="TAttribute"/> to get.</param>
        /// <returns>
        /// <c>true</c> if a <typeparamref name="TAttribute"/> with the same
        /// <see cref="IReadOnlyTagHelperAttribute.Name"/> exists in the collection; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
        public bool ContainsName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            // Perf: Avoid allocating enumerator
            for (var i = 0; i < Attributes.Count; i++)
            {
                if (NameEquals(name, Attributes[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Searches for a <typeparamref name="TAttribute"/> matching <paramref name="item"/> in the collection and
        /// returns the zero-based index of the first occurrence.
        /// </summary>
        /// <param name="item">The <typeparamref name="TAttribute"/> to locate.</param>
        /// <returns>The zero-based index of the first occurrence of a <typeparamref name="TAttribute"/> matching
        /// <paramref name="item"/> in the collection, if found; otherwise, –1.</returns>
        /// <remarks>
        /// <paramref name="item"/>s <see cref="IReadOnlyTagHelperAttribute.Name"/> is compared case-insensitively.
        /// </remarks>
        public int IndexOf(TAttribute item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return Attributes.IndexOf(item);
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TAttribute"/> with <see cref="IReadOnlyTagHelperAttribute.Name"/>
        /// matching <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The <see cref="IReadOnlyTagHelperAttribute.Name"/> of the
        /// <typeparamref name="TAttribute"/> to get.</param>
        /// <param name="attribute">When this method returns, the first <typeparamref name="TAttribute"/> with
        /// <see cref="IReadOnlyTagHelperAttribute.Name"/> matching <paramref name="name"/>, if found; otherwise,
        /// <c>null</c>.</param>
        /// <returns><c>true</c> if a <typeparamref name="TAttribute"/> with the same
        /// <see cref="IReadOnlyTagHelperAttribute.Name"/> exists in the collection; otherwise, <c>false</c>.</returns>
        /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
        public bool TryGetAttribute(string name, out TAttribute attribute)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            attribute = this[name];

            return attribute != null;
        }

        /// <summary>
        /// Retrieves <typeparamref name="TAttribute"/>s in the collection with
        /// <see cref="IReadOnlyTagHelperAttribute.Name"/> matching <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The <see cref="IReadOnlyTagHelperAttribute.Name"/> of the
        /// <typeparamref name="TAttribute"/>s to get.</param>
        /// <param name="attributes">When this method returns, the <typeparamref name="TAttribute"/>s with
        /// <see cref="IReadOnlyTagHelperAttribute.Name"/> matching <paramref name="name"/>.</param>
        /// <returns><c>true</c> if at least one <typeparamref name="TAttribute"/> with the same
        /// <see cref="IReadOnlyTagHelperAttribute.Name"/> exists in the collection; otherwise, <c>false</c>.</returns>
        /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
        public bool TryGetAttributes(string name, out IReadOnlyList<TAttribute> attributes)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            // Perf: Avoid allocating enumerator
            List<TAttribute> matchedAttributes = null;
            for (var i = 0; i < Attributes.Count; i++)
            {
                if (NameEquals(name, Attributes[i]))
                {
                    if (matchedAttributes == null)
                    {
                        matchedAttributes = new List<TAttribute>();
                    }

                    matchedAttributes.Add(Attributes[i]);
                }
            }
            attributes = matchedAttributes ?? EmptyList;

            return matchedAttributes != null;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator<TAttribute> GetEnumerator()
        {
            return Attributes.GetEnumerator();
        }

        /// <summary>
        /// Determines if the specified <paramref name="attribute"/> has the same name as <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The value to compare against <paramref name="attribute"/>s
        /// <see cref="TagHelperAttribute.Name"/>.</param>
        /// <param name="attribute">The attribute to compare against.</param>
        /// <returns><c>true</c> if <paramref name="name"/> case-insensitively matches <paramref name="attribute"/>s
        /// <see cref="TagHelperAttribute.Name"/>.</returns>
        protected static bool NameEquals(string name, TAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            return string.Equals(name, attribute.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}