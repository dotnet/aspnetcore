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
using System.Globalization;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    public class RazorError : IEquatable<RazorError>
    {
        public RazorError(string message, SourceLocation location)
            : this(message, location, 1)
        {
        }

        public RazorError(string message, int absoluteIndex, int lineIndex, int columnIndex)
            : this(message, new SourceLocation(absoluteIndex, lineIndex, columnIndex))
        {
        }

        public RazorError(string message, SourceLocation location, int length)
        {
            Message = message;
            Location = location;
            Length = length;
        }

        public RazorError(string message, int absoluteIndex, int lineIndex, int columnIndex, int length)
            : this(message, new SourceLocation(absoluteIndex, lineIndex, columnIndex), length)
        {
        }

        public string Message { get; private set; }
        public SourceLocation Location { get; private set; }
        public int Length { get; private set; }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "Error @ {0}({2}) - [{1}]", Location, Message, Length);
        }

        public override bool Equals(object obj)
        {
            RazorError err = obj as RazorError;
            return (err != null) && (Equals(err));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(RazorError other)
        {
            return String.Equals(other.Message, Message, StringComparison.Ordinal) &&
                   Location.Equals(other.Location);
        }
    }
}
