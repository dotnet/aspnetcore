// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace BasicTestApp
{
    // Written in C# for flexibility and because we don't currently have the ability to write this in .razor.
    public class DuplicateAttributesOnElementChildComponent : ComponentBase
    {
        [Parameter] public string StringAttributeBefore { get; private set; }
        [Parameter] public bool BoolAttributeBefore { get; private set; }
        [Parameter] public string StringAttributeAfter { get; private set; }
        [Parameter] public bool? BoolAttributeAfter { get; private set; }

        [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> UnmatchedValues { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "string", StringAttributeBefore);
            builder.AddAttribute(2, "bool", BoolAttributeBefore);
            builder.AddMultipleAttributes(3, UnmatchedValues);
            if (StringAttributeAfter != null)
            {
                builder.AddAttribute(4, "string", StringAttributeAfter);
            }
            if (BoolAttributeAfter != null)
            {
                builder.AddAttribute(5, "bool", BoolAttributeAfter);
            }
            builder.CloseElement();
        }
    }
}
