// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Html
{
    public partial class HtmlContentBuilder : Microsoft.AspNetCore.Html.IHtmlContent, Microsoft.AspNetCore.Html.IHtmlContentBuilder, Microsoft.AspNetCore.Html.IHtmlContentContainer
    {
        public HtmlContentBuilder() { }
        public HtmlContentBuilder(System.Collections.Generic.IList<object> entries) { }
        public HtmlContentBuilder(int capacity) { }
        public int Count { get { throw null; } }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder Append(string unencoded) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendHtml(Microsoft.AspNetCore.Html.IHtmlContent htmlContent) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendHtml(string encoded) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContentBuilder Clear() { throw null; }
        public void CopyTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination) { }
        public void MoveTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination) { }
        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { }
    }
    public static partial class HtmlContentBuilderExtensions
    {
        public static Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendFormat(this Microsoft.AspNetCore.Html.IHtmlContentBuilder builder, System.IFormatProvider formatProvider, string format, params object[] args) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendFormat(this Microsoft.AspNetCore.Html.IHtmlContentBuilder builder, string format, params object[] args) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendHtmlLine(this Microsoft.AspNetCore.Html.IHtmlContentBuilder builder, string encoded) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendLine(this Microsoft.AspNetCore.Html.IHtmlContentBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendLine(this Microsoft.AspNetCore.Html.IHtmlContentBuilder builder, Microsoft.AspNetCore.Html.IHtmlContent content) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendLine(this Microsoft.AspNetCore.Html.IHtmlContentBuilder builder, string unencoded) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContentBuilder SetContent(this Microsoft.AspNetCore.Html.IHtmlContentBuilder builder, string unencoded) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContentBuilder SetHtmlContent(this Microsoft.AspNetCore.Html.IHtmlContentBuilder builder, Microsoft.AspNetCore.Html.IHtmlContent content) { throw null; }
        public static Microsoft.AspNetCore.Html.IHtmlContentBuilder SetHtmlContent(this Microsoft.AspNetCore.Html.IHtmlContentBuilder builder, string encoded) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public partial class HtmlFormattableString : Microsoft.AspNetCore.Html.IHtmlContent
    {
        public HtmlFormattableString(System.IFormatProvider formatProvider, string format, params object[] args) { }
        public HtmlFormattableString(string format, params object[] args) { }
        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { }
    }
    public partial class HtmlString : Microsoft.AspNetCore.Html.IHtmlContent
    {
        public static readonly Microsoft.AspNetCore.Html.HtmlString Empty;
        public static readonly Microsoft.AspNetCore.Html.HtmlString NewLine;
        public HtmlString(string value) { }
        public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override string ToString() { throw null; }
        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { }
    }
    public partial interface IHtmlContent
    {
        void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder);
    }
    public partial interface IHtmlContentBuilder : Microsoft.AspNetCore.Html.IHtmlContent, Microsoft.AspNetCore.Html.IHtmlContentContainer
    {
        Microsoft.AspNetCore.Html.IHtmlContentBuilder Append(string unencoded);
        Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendHtml(Microsoft.AspNetCore.Html.IHtmlContent content);
        Microsoft.AspNetCore.Html.IHtmlContentBuilder AppendHtml(string encoded);
        Microsoft.AspNetCore.Html.IHtmlContentBuilder Clear();
    }
    public partial interface IHtmlContentContainer : Microsoft.AspNetCore.Html.IHtmlContent
    {
        void CopyTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder builder);
        void MoveTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder builder);
    }
}
