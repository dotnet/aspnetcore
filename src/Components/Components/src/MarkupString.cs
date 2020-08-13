// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// A string value that can be rendered as markup such as HTML.
    /// </summary>
    public readonly struct MarkupString
    {
        /// <summary>
        /// Constructs an instance of <see cref="MarkupString"/>.
        /// </summary>
        /// <param name="value">The value for the new instance.</param>
        public MarkupString(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value of the <see cref="MarkupString"/>.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Casts a <see cref="string"/> to a <see cref="MarkupString"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> value.</param>
        public static explicit operator MarkupString(string value)
            => new MarkupString(value);

        /// <inheritdoc />
        public override string ToString()
            => Value ?? string.Empty;
    }
}
