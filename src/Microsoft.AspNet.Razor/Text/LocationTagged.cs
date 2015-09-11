// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Text
{
    [DebuggerDisplay("({Location})\"{Value}\"")]
    public class LocationTagged<TValue> : IFormattable
    {
        private LocationTagged()
        {
            Location = SourceLocation.Undefined;
            Value = default(TValue);
        }

        public LocationTagged(TValue value, int offset, int line, int col)
            : this(value, new SourceLocation(offset, line, col))
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
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
            LocationTagged<TValue> other = obj as LocationTagged<TValue>;
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return Equals(other.Location, Location) &&
                Equals(other.Value, Value);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Location)
                .Add(Value)
                .CombinedHash;
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

        public static bool operator ==(LocationTagged<TValue> left, LocationTagged<TValue> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LocationTagged<TValue> left, LocationTagged<TValue> right)
        {
            return !Equals(left, right);
        }
    }
}
