// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// An HTML tag helper attribute.
    /// </summary>
    public class TagHelperAttribute : IReadOnlyTagHelperAttribute
    {
        private static readonly int TypeHashCode = typeof(TagHelperAttribute).GetHashCode();

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperAttribute"/>.
        /// </summary>
        public TagHelperAttribute()
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperAttribute"/> with the specified <paramref name="name"/>
        /// and <paramref name="value"/>.
        /// </summary>
        /// <param name="name">The <see cref="Name"/> of the attribute.</param>
        /// <param name="value">The <see cref="Value"/> of the attribute.</param>
        public TagHelperAttribute(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the name of the attribute.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the attribute.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Converts the specified <paramref name="value"/> into a <see cref="TagHelperAttribute"/>.
        /// </summary>
        /// <param name="value">The <see cref="Value"/> of the created <see cref="TagHelperAttribute"/>.</param>
        /// <remarks>Created <see cref="TagHelperAttribute"/>s <see cref="Name"/> is set to <c>null</c>.</remarks>
        public static implicit operator TagHelperAttribute(string value)
        {
            return new TagHelperAttribute
            {
                Value = value
            };
        }

        /// <inheritdoc />
        /// <remarks><see cref="Name"/> is compared case-insensitively.</remarks>
        public bool Equals(IReadOnlyTagHelperAttribute other)
        {
            return
                other != null &&
                string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                Equals(Value, other.Value);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as IReadOnlyTagHelperAttribute;

            return Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return TypeHashCode;
        }
    }
}