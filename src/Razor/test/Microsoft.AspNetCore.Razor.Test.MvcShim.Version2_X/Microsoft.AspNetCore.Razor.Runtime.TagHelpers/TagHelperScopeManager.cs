// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers
{
    public class TagHelperScopeManager
    {
        public TagHelperScopeManager(
            Action<HtmlEncoder> startTagHelperWritingScope,
            Func<TagHelperContent> endTagHelperWritingScope)
        {
        }

        public TagHelperExecutionContext Begin(
            string tagName,
            TagMode tagMode,
            string uniqueId,
            Func<Task> executeChildContentAsync)
        {
            throw null;
        }

        public TagHelperExecutionContext End()
        {
            throw null;
        }
    }
}