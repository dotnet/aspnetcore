// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Text
{
    [DebuggerDisplay("({Location})\"{Value}\"")]
    public class LocationTagged<T> : IFormattable
    {
        private LocationTagged()
        {
            Location = SourceLocation.Undefined;
            Value = default(T);
        }

        public LocationTagged(T value, int offset, int line, int col)
            : this(value, new SourceLocation(offset, line, col))
        {
        }

        public LocationTagged(T value, SourceLocation location)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            Location = location;
            Value = value;
        }

        public SourceLocation Location { get; }

        public T Value { get; }

        public override bool Equals(object obj)
        {
            LocationTagged<T> other = obj as LocationTagged<T>;
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

        public static implicit operator T(LocationTagged<T> value)
        {
            return value.Value;
        }

        public static bool operator ==(LocationTagged<T> left, LocationTagged<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LocationTagged<T> left, LocationTagged<T> right)
        {
            return !Equals(left, right);
        }
    }
}
