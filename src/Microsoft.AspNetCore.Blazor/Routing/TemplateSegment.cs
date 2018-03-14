// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Routing
{
    internal class TemplateSegment
    {
        public TemplateSegment(string segment, bool isParameter)
        {
            Value = segment;
            IsParameter = isParameter;
        }

        // The value of the segment. The exact text to match when is a literal.
        // The parameter name when its a segment
        public string Value { get; }

        public bool IsParameter { get; }

        public bool Match(string pathSegment)
        {
            if (IsParameter)
            {
                return true;
            }
            else
            {
                return string.Equals(Value, pathSegment, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
