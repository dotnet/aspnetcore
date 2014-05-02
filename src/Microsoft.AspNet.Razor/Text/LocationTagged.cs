// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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

        public SourceLocation Location { get; private set; }
        public T Value { get; private set; }

        public override bool Equals(object obj)
        {
            LocationTagged<T> other = obj as LocationTagged<T>;
            return other != null &&
                   Equals(other.Location, Location) &&
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
            if (String.IsNullOrEmpty(format))
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
                    return String.Format(formatProvider, "{0}@{1}", Value, Location);
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
