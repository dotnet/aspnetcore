// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Csp
{
    /// <summary>
    /// Tag helper used to automatically a nonce attribute to existing script tags. This is required to support nonce-based content security policies.
    /// </summary>
    [HtmlTargetElement("script")]
    public class NoncedScriptTagHelper : TagHelper
    {
        private readonly INonce _nonce;

        public NoncedScriptTagHelper(INonce nonce)
        {
            _nonce = nonce;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "script";
            output.Attributes.SetAttribute("nonce", _nonce.GetValue());
        }
    }
}
