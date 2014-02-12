// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Generator
{
    public struct GeneratedCodeMapping
    {
        public GeneratedCodeMapping(int startLine, int startColumn, int startGeneratedColumn, int codeLength)
            : this(null, startLine, startColumn, startGeneratedColumn, codeLength)
        {
        }

        public GeneratedCodeMapping(int startOffset, int startLine, int startColumn, int startGeneratedColumn, int codeLength)
            : this((int?)startOffset, startLine, startColumn, startGeneratedColumn, codeLength)
        {
        }

        private GeneratedCodeMapping(int? startOffset, int startLine, int startColumn, int startGeneratedColumn, int codeLength)
            : this()
        {
            if (startLine < 0)
            {
                throw new ArgumentOutOfRangeException("startLine", CommonResources.Argument_Must_Be_GreaterThanOrEqualTo(0));
            }
            if (startColumn < 0)
            {
                throw new ArgumentOutOfRangeException("startColumn", CommonResources.Argument_Must_Be_GreaterThanOrEqualTo(0));
            }
            if (startGeneratedColumn < 0)
            {
                throw new ArgumentOutOfRangeException("startGeneratedColumn", CommonResources.Argument_Must_Be_GreaterThanOrEqualTo(0));
            }
            if (codeLength < 0)
            {
                throw new ArgumentOutOfRangeException("codeLength", CommonResources.Argument_Must_Be_GreaterThanOrEqualTo(0));
            }

            StartOffset = startOffset;
            StartLine = startLine;
            StartColumn = startColumn;
            StartGeneratedColumn = startGeneratedColumn;
            CodeLength = codeLength;
        }

        public int? StartOffset { get; set; }
        public int CodeLength { get; set; }
        public int StartColumn { get; set; }
        public int StartGeneratedColumn { get; set; }
        public int StartLine { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is GeneratedCodeMapping))
            {
                return false;
            }
            GeneratedCodeMapping other = (GeneratedCodeMapping)obj;
            return CodeLength == other.CodeLength &&
                   StartColumn == other.StartColumn &&
                   StartGeneratedColumn == other.StartGeneratedColumn &&
                   StartLine == other.StartLine &&
                   // Null means it matches the other no matter what.
                   (StartOffset == null || other.StartOffset == null || StartOffset.Equals(other.StartOffset));
        }

        public override string ToString()
        {
            return String.Format(
                CultureInfo.CurrentCulture,
                "({0}, {1}, {2}) -> (?, {3}) [{4}]",
                StartOffset == null ? "?" : StartOffset.Value.ToString(CultureInfo.CurrentCulture),
                StartLine,
                StartColumn,
                StartGeneratedColumn,
                CodeLength);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(CodeLength)
                .Add(StartColumn)
                .Add(StartGeneratedColumn)
                .Add(StartLine)
                .Add(StartOffset)
                .CombinedHash;
        }

        public static bool operator ==(GeneratedCodeMapping left, GeneratedCodeMapping right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GeneratedCodeMapping left, GeneratedCodeMapping right)
        {
            return !left.Equals(right);
        }
    }
}
