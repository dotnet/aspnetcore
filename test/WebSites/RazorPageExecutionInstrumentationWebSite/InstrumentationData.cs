// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace RazorPageExecutionInstrumentationWebSite
{
    public class InstrumentationData
    {
        public InstrumentationData(string filePath, int position, int length, bool isLiteral)
        {
            FilePath = filePath;
            Position = position;
            Length = length;
            IsLiteral = isLiteral;
        }

        public string FilePath { get; }

        public int Position { get; }

        public int Length { get; }

        public bool IsLiteral { get; }

        public override string ToString()
        {
            var literal = IsLiteral ? "Literal" : "Non-literal";

            return $"{FilePath}: {literal} at {Position} contains {Length} characters.";
        }
    }
}
