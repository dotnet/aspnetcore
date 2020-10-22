// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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