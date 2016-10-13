// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class RazorError : IEquatable<RazorError>
    {
        internal static readonly RazorError[] EmptyArray = new RazorError[0];

        /// <summary>
        /// Used only for deserialization.
        /// </summary>
        public RazorError()
            : this(message: string.Empty, location: SourceLocation.Undefined, length: -1)
        {
        }

        public RazorError(string message, int absoluteIndex, int lineIndex, int columnIndex, int length)
            : this(message, new SourceLocation(absoluteIndex, lineIndex, columnIndex), length)
        {
        }

        public RazorError(string message, SourceLocation location, int length)
        {
            Message = message;
            Location = location;
            Length = length;
        }

        /// <summary>
        /// Gets (or sets) the message describing the error.
        /// </summary>
        /// <remarks>Set property is only accessible for deserialization purposes.</remarks>
        public string Message { get; set; }

        /// <summary>
        /// Gets (or sets) the start position of the erroneous text.
        /// </summary>
        /// <remarks>Set property is only accessible for deserialization purposes.</remarks>
        public SourceLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the length of the erroneous text.
        /// </summary>
        /// <remarks>Set property is only accessible for deserialization purposes.</remarks>
        public int Length { get; set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Error @ {0}({2}) - [{1}]", Location, Message, Length);
        }

        public override bool Equals(object obj)
        {
            var error = obj as RazorError;
            return Equals(error);
        }

        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(Message, StringComparer.Ordinal);
            hashCodeCombiner.Add(Location);

            return hashCodeCombiner;
        }

        public bool Equals(RazorError other)
        {
            return other != null &&
                string.Equals(other.Message, Message, StringComparison.Ordinal) &&
                Location.Equals(other.Location) &&
                Length.Equals(other.Length);
        }
    }
}
