// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    /// <summary>
    /// A Pattern segment contains a portion of the test string/ substitution segment with a type associated.
    /// This type can either be: Regex, Rule Variable, Condition Variable, or a Server Variable.
    /// </summary>
    public class PatternSegment
    {
        public string Variable { get; } // TODO make this a range s.t. we don't copy the string.
        public SegmentType Type { get; }

        /// <summary>
        /// Create a Pattern segment.
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="type"></param>
        public PatternSegment(string variable, SegmentType type)
        {
            Variable = variable;
            Type = type;
        }
    }
}
