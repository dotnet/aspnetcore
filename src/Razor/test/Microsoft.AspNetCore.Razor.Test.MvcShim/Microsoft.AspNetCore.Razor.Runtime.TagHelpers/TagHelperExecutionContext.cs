// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers
{
    public class TagHelperExecutionContext
    {
        public TagHelperExecutionContext(
            string tagName,
            TagMode tagMode,
            IDictionary<object, object> items,
            string uniqueId,
            Func<Task> executeChildContentAsync,
            Action<HtmlEncoder> startTagHelperWritingScope,
            Func<TagHelperContent> endTagHelperWritingScope)
        {
        }

        public bool ChildContentRetrieved => false;

        public IDictionary<object, object> Items { get; private set; }

        public IList<ITagHelper> TagHelpers => null;

        public TagHelperOutput Output { get; internal set; }

        public TagHelperContext Context { get; }

        public void Add(ITagHelper tagHelper)
        {
        }

        public void AddHtmlAttribute(string name, object value, HtmlAttributeValueStyle valueStyle)
        {
        }

        public void AddHtmlAttribute(TagHelperAttribute attribute)
        {
        }

        public void AddTagHelperAttribute(string name, object value, HtmlAttributeValueStyle valueStyle)
        {
        }

        public void AddTagHelperAttribute(TagHelperAttribute attribute)
        {
        }

        public void Reinitialize(
            string tagName,
            TagMode tagMode,
            IDictionary<object, object> items,
            string uniqueId,
            Func<Task> executeChildContentAsync)
        {
        }

        public Task SetOutputContentAsync()
        {
            throw null;
        }
    }
}