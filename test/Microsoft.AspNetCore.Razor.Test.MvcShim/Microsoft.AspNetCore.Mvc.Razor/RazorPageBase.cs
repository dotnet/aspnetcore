// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public abstract class RazorPageBase
    {
        public virtual ViewContext ViewContext { get; set; }

        public string Layout { get; set; }

        public virtual TextWriter Output { get; }

        public string Path { get; set; }

        public IDictionary<string, RenderAsyncDelegate> SectionWriters { get; }

        public dynamic ViewBag { get; }

        public bool IsLayoutBeingRendered { get; set; }

        public IHtmlContent BodyContent { get; set; }

        public IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters { get; set; }

        public DiagnosticSource DiagnosticSource { get; set; }

        public HtmlEncoder HtmlEncoder { get; set; }

        // This was "ClaimsPrincipal" but we didn't want to add the reference.
        public virtual object User { get; }

        public ITempDataDictionary TempData { get; }

        public abstract Task ExecuteAsync();

        public TTagHelper CreateTagHelper<TTagHelper>() where TTagHelper : ITagHelper
        {
            throw new NotImplementedException();
        }

        public void StartTagHelperWritingScope(HtmlEncoder encoder)
        {
        }

        public TagHelperContent EndTagHelperWritingScope()
        {
            throw new NotImplementedException();
        }

        public void BeginWriteTagHelperAttribute()
        {
        }

        public string EndWriteTagHelperAttribute()
        {
            throw new NotImplementedException();
        }

        public virtual string Href(string contentPath)
        {
            throw new NotImplementedException();
        }

        public virtual void DefineSection(string name, RenderAsyncDelegate section)
        {
        }

        public virtual void Write(object value)
        {
        }

        public virtual void WriteTo(TextWriter writer, object value)
        {
        }

        public static void WriteTo(TextWriter writer, HtmlEncoder encoder, object value)
        {
        }

        public virtual void WriteTo(TextWriter writer, string value)
        {
        }

        private static void WriteTo(TextWriter writer, HtmlEncoder encoder, string value)
        {
        }

        public virtual void WriteLiteral(object value)
        {
        }

        public virtual void WriteLiteralTo(TextWriter writer, object value)
        {
        }

        public virtual void WriteLiteralTo(TextWriter writer, string value)
        {
        }

        public virtual void BeginWriteAttribute(
            string name,
            string prefix,
            int prefixOffset,
            string suffix,
            int suffixOffset,
            int attributeValuesCount)
        {
        }

        public virtual void BeginWriteAttributeTo(
            TextWriter writer,
            string name,
            string prefix,
            int prefixOffset,
            string suffix,
            int suffixOffset,
            int attributeValuesCount)
        {
        }

        public void WriteAttributeValue(
            string prefix,
            int prefixOffset,
            object value,
            int valueOffset,
            int valueLength,
            bool isLiteral)
        {
        }

        public void WriteAttributeValueTo(
            TextWriter writer,
            string prefix,
            int prefixOffset,
            object value,
            int valueOffset,
            int valueLength,
            bool isLiteral)
        {
        }

        public virtual void EndWriteAttribute()
        {
        }

        public virtual void EndWriteAttributeTo(TextWriter writer)
        {
        }

        public void BeginAddHtmlAttributeValues(
            TagHelperExecutionContext executionContext,
            string attributeName,
            int attributeValuesCount,
            HtmlAttributeValueStyle attributeValueStyle)
        {
        }

        public void AddHtmlAttributeValue(
            string prefix,
            int prefixOffset,
            object value,
            int valueOffset,
            int valueLength,
            bool isLiteral)
        {
        }

        public void EndAddHtmlAttributeValues(TagHelperExecutionContext executionContext)
        {
        }

        public virtual Task<HtmlString> FlushAsync()
        {
            throw new NotImplementedException();
        }

        public virtual HtmlString SetAntiforgeryCookieAndHeader()
        {
            throw new NotImplementedException();
        }

        public abstract void BeginContext(int position, int length, bool isLiteral);

        public abstract void EndContext();

        public abstract void EnsureRenderedBodyOrSections();
    }
}
