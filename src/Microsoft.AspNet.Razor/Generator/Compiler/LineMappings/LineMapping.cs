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

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class LineMapping
    {
        public LineMapping()
            : this(documentLocation: null, generatedLocation: null)
        {
        }

        public LineMapping(MappingLocation documentLocation, MappingLocation generatedLocation)
        {
            DocumentLocation = documentLocation;
            GeneratedLocation = generatedLocation;
        }

        public MappingLocation DocumentLocation { get; set; }
        public MappingLocation GeneratedLocation { get; set; }

        public override bool Equals(object obj)
        {
            LineMapping other = obj as LineMapping;
            return DocumentLocation.Equals(other.DocumentLocation) &&
                   GeneratedLocation.Equals(other.GeneratedLocation);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(LineMapping left, LineMapping right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LineMapping left, LineMapping right)
        {
            return !left.Equals(right);
        }
    }
}
