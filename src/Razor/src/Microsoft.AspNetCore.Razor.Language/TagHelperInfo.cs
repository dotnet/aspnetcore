// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class TagHelperInfo
    {
        public TagHelperInfo(
            string tagName,
            TagMode tagMode,
            TagHelperBinding bindingResult)
        {
            TagName = tagName;
            TagMode = tagMode;
            BindingResult = bindingResult;
        }

        public string TagName { get; }

        public TagMode TagMode { get; }

        public TagHelperBinding BindingResult { get; }
    }
}
