// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    [DebuggerDisplay("({Location})\"{Value}\"")]
    internal class LocationTagged<TValue> : IFormattable
    {
        public LocationTagged(TValue value, int absoluteIndex, int lineIndex, int characterIndex)
            : this (value, new SourceLocation(absoluteIndex, lineIndex, characterIndex))
        {
        }

        public LocationTagged(TValue value, SourceLocation location)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Location = location;
            Value = value;
        }

        public SourceLocation Location { get; }

        public TValue Value { get; }

        public override bool Equals(object obj)
        {
            var other = obj as LocationTagged<TValue>;
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return Equals(other.Location, Location) &&
                Equals(other.Value, Value);
        }

        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(Location);
            hashCodeCombiner.Add(Value);

            return hashCodeCombiner.CombinedHash;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
            {
                format = "P";
            }
            if (formatProvider == null)
            {
                formatProvider = CultureInfo.CurrentCulture;
            }
            switch (format.ToUpperInvariant())
            {
                case "F":
                    return string.Format(formatProvider, "{0}@{1}", Value, Location);
                default:
                    return Value.ToString();
            }
        }

        public static implicit operator TValue(LocationTagged<TValue> value)
        {
            return value == null ? default(TValue) : value.Value;
        }
    }
}
