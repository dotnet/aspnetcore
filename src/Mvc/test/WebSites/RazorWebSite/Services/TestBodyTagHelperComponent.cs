// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace RazorWebSite
{
    public class TestBodyTagHelperComponent : ITagHelperComponent
    {
        private int _order;
        private string _html;

        public TestBodyTagHelperComponent() : this(1, "<script>'This was injected!!'</script>")
        {

        }

        public TestBodyTagHelperComponent(int order, string html)
        {
            _order = order;
            _html = html;
        }

        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public int Order => _order;

        public void Init(TagHelperContext context)
        {
        }

        public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (string.Equals(context.TagName, "body", StringComparison.Ordinal) &&
                output.Attributes.ContainsName("inject"))
            {
                output.PostContent.AppendHtml(_html);
                ViewContext.ViewData["TestData"] = "NewValue";
            }

            return Task.FromResult(0);
        }
    }
}
