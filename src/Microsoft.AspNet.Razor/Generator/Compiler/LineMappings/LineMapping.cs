// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class LineMapping
    {
        public LineMapping(MappingLocation documentLocation, MappingLocation generatedLocation)
        {
            DocumentLocation = documentLocation;
            GeneratedLocation = generatedLocation;
        }

        public MappingLocation DocumentLocation { get; }

        public MappingLocation GeneratedLocation { get; }

        public override bool Equals(object obj)
        {
            var other = obj as LineMapping;
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return DocumentLocation.Equals(other.DocumentLocation) &&
                GeneratedLocation.Equals(other.GeneratedLocation);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(DocumentLocation)
                .Add(GeneratedLocation)
                .CombinedHash;
        }

        public static bool operator ==(LineMapping left, LineMapping right)
        {
            if (ReferenceEquals(left, right))
            {
                // Exact equality e.g. both objects are null.
                return true;
            }

            if (ReferenceEquals(left, null))
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(LineMapping left, LineMapping right)
        {
            if (ReferenceEquals(left, right))
            {
                // Exact equality e.g. both objects are null.
                return false;
            }

            if (ReferenceEquals(left, null))
            {
                return true;
            }

            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentUICulture, "{0} -> {1}", DocumentLocation, GeneratedLocation);
        }
    }
}
