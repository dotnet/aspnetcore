// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    /// <summary>
    /// An HTML tag helper attribute.
    /// </summary>
    public class TagHelperAttribute
    {
        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperAttribute"/> with the specified <paramref name="name"/>.
        /// <see cref="Minimized"/> is set to <c>true</c> and <see cref="Value"/> to <c>null</c>.
        /// </summary>
        /// <param name="name">The <see cref="Name"/> of the attribute.</param>
        public TagHelperAttribute(string name)
            : this(name, value: null, minimized: true)
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperAttribute"/> with the specified <paramref name="name"/>
        /// and <paramref name="value"/>. <see cref="Minimized"/> is set to <c>false</c>.
        /// </summary>
        /// <param name="name">The <see cref="Name"/> of the attribute.</param>
        /// <param name="value">The <see cref="Value"/> of the attribute.</param>
        public TagHelperAttribute(string name, object value)
            : this(name, value, minimized: false)
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperAttribute"/> with the specified <paramref name="name"/>,
        /// <paramref name="value"/> and <paramref name="minimized"/>.
        /// </summary>
        /// <param name="name">The <see cref="Name"/> of the new instance.</param>
        /// <param name="value">The <see cref="Value"/> of the new instance.</param>
        /// <param name="minimized">The <see cref="Minimized"/> value of the new instance.</param>
        /// <remarks>If <paramref name="minimized"/> is <c>true</c>, <paramref name="value"/> is ignored when this
        /// instance is rendered.</remarks>
        public TagHelperAttribute(string name, object value, bool minimized)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Value = value;
            Minimized = minimized;
        }

        /// <summary>
        /// Gets the name of the attribute.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value of the attribute.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Gets an indication whether the attribute is minimized or not.
        /// </summary>
        /// <remarks>If <c>true</c>, <see cref="Value"/> will be ignored.</remarks>
        public bool Minimized { get; }


        /// <inheritdoc />
        /// <remarks><see cref="Name"/> is compared case-insensitively.</remarks>
        public bool Equals(TagHelperAttribute other)
        {
            return
                other != null &&
                string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                Minimized == other.Minimized &&
                (Minimized || Equals(Value, other.Value));
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as TagHelperAttribute;

            return Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(Name, StringComparer.Ordinal);
            hashCodeCombiner.Add(Value);
            hashCodeCombiner.Add(Minimized);

            return hashCodeCombiner.CombinedHash;
        }
    }
}