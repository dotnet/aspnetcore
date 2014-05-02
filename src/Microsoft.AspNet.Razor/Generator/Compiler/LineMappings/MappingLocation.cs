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

using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class MappingLocation
    {
        public MappingLocation() : base() { }

        public MappingLocation(SourceLocation location, int contentLength)
        {
            ContentLength = contentLength;
            AbsoluteIndex = location.AbsoluteIndex;
            LineIndex = location.LineIndex;
            CharacterIndex = location.CharacterIndex;
        }

        public int ContentLength { get; set; }
        public int AbsoluteIndex { get; set; }
        public int LineIndex { get; set; }
        public int CharacterIndex { get; set; }

        public override bool Equals(object obj)
        {
            MappingLocation other = obj as MappingLocation;

            return AbsoluteIndex == other.AbsoluteIndex &&
                   ContentLength == other.ContentLength &&
                   LineIndex == other.LineIndex &&
                   CharacterIndex == other.CharacterIndex;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(MappingLocation left, MappingLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MappingLocation left, MappingLocation right)
        {
            return !left.Equals(right);
        }
    }
}
