// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace TagHelpersWebSite.TagHelpers
{
    [TargetElement(Attributes = nameof(Surround))]
    public class SurroundTagHelper : TagHelper
    {
        public override int Order
        {
            get
            {
                // Run first
                return int.MinValue;
            }
        }

        public string Surround { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var surroundingTagName = Surround.ToLowerInvariant();

            output.PreElement.SetContent($"<{surroundingTagName}>");
            output.PostElement.SetContent($"</{surroundingTagName}>");
        }
    }
}