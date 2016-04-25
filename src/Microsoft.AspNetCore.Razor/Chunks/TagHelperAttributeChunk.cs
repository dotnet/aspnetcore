// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Chunks
{
    public struct TagHelperAttributeTracker
    {
        public TagHelperAttributeTracker(string name, Chunk value, HtmlAttributeValueStyle valueStyle)
        {
            Name = name;
            Value = value;
            ValueStyle = valueStyle;
        }

        public string Name { get; }

        public Chunk Value { get; }

        public HtmlAttributeValueStyle ValueStyle { get; }
    }
}
