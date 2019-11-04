// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString(),nq}")]
    public partial class DefaultTagHelperContent : Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent
    {
        public DefaultTagHelperContent() { }
        public override bool IsEmptyOrWhiteSpace { get { throw null; } }
        public override bool IsModified { get { throw null; } }
        public override Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent Append(string unencoded) { throw null; }
        public override Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent AppendHtml(Microsoft.AspNetCore.Html.IHtmlContent htmlContent) { throw null; }
        public override Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent AppendHtml(string encoded) { throw null; }
        public override Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent Clear() { throw null; }
        public override void CopyTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination) { }
        public override string GetContent() { throw null; }
        public override string GetContent(System.Text.Encodings.Web.HtmlEncoder encoder) { throw null; }
        public override void MoveTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination) { }
        public override void Reinitialize() { }
        public override void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=false)]
    public sealed partial class HtmlAttributeNameAttribute : System.Attribute
    {
        public HtmlAttributeNameAttribute() { }
        public HtmlAttributeNameAttribute(string name) { }
        public string DictionaryAttributePrefix { get { throw null; } set { } }
        public bool DictionaryAttributePrefixSet { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=false)]
    public sealed partial class HtmlAttributeNotBoundAttribute : System.Attribute
    {
        public HtmlAttributeNotBoundAttribute() { }
    }
    public enum HtmlAttributeValueStyle
    {
        DoubleQuotes = 0,
        SingleQuotes = 1,
        NoQuotes = 2,
        Minimized = 3,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true, Inherited=false)]
    public sealed partial class HtmlTargetElementAttribute : System.Attribute
    {
        public const string ElementCatchAllTarget = "*";
        public HtmlTargetElementAttribute() { }
        public HtmlTargetElementAttribute(string tag) { }
        public string Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ParentTag { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Tag { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagStructure TagStructure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial interface ITagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent
    {
    }
    public partial interface ITagHelperComponent
    {
        int Order { get; }
        void Init(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context);
        System.Threading.Tasks.Task ProcessAsync(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output);
    }
    public sealed partial class NullHtmlEncoder : System.Text.Encodings.Web.HtmlEncoder
    {
        internal NullHtmlEncoder() { }
        public static new Microsoft.AspNetCore.Razor.TagHelpers.NullHtmlEncoder Default { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override int MaxOutputCharactersPerInputCharacter { get { throw null; } }
        public override void Encode(System.IO.TextWriter output, char[] value, int startIndex, int characterCount) { }
        public override void Encode(System.IO.TextWriter output, string value, int startIndex, int characterCount) { }
        public override string Encode(string value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public unsafe override int FindFirstCharacterToEncode(char* text, int textLength) { throw null; }
        public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public override bool WillEncode(int unicodeScalar) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
    public sealed partial class OutputElementHintAttribute : System.Attribute
    {
        public OutputElementHintAttribute(string outputElement) { }
        public string OutputElement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class ReadOnlyTagHelperAttributeList : System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute>
    {
        protected ReadOnlyTagHelperAttributeList() : base (default(System.Collections.Generic.IList<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute>)) { }
        public ReadOnlyTagHelperAttributeList(System.Collections.Generic.IList<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute> attributes) : base (default(System.Collections.Generic.IList<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute>)) { }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute this[string name] { get { throw null; } }
        public bool ContainsName(string name) { throw null; }
        public int IndexOfName(string name) { throw null; }
        protected static bool NameEquals(string name, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute attribute) { throw null; }
        public bool TryGetAttribute(string name, out Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute attribute) { throw null; }
        public bool TryGetAttributes(string name, out System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute> attributes) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, Inherited=false, AllowMultiple=false)]
    public sealed partial class RestrictChildrenAttribute : System.Attribute
    {
        public RestrictChildrenAttribute(string childTag, params string[] childTags) { }
        public System.Collections.Generic.IEnumerable<string> ChildTags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class TagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper, Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent
    {
        protected TagHelper() { }
        public virtual int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual void Init(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context) { }
        public virtual void Process(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output) { }
        public virtual System.Threading.Tasks.Task ProcessAsync(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output) { throw null; }
    }
    public partial class TagHelperAttribute : Microsoft.AspNetCore.Html.IHtmlContent, Microsoft.AspNetCore.Html.IHtmlContentContainer
    {
        public TagHelperAttribute(string name) { }
        public TagHelperAttribute(string name, object value) { }
        public TagHelperAttribute(string name, object value, Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle valueStyle) { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle ValueStyle { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void CopyTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination) { }
        public bool Equals(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public void MoveTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination) { }
        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { }
    }
    public partial class TagHelperAttributeList : Microsoft.AspNetCore.Razor.TagHelpers.ReadOnlyTagHelperAttributeList, System.Collections.Generic.ICollection<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute>, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute>, System.Collections.Generic.IList<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute>, System.Collections.IEnumerable
    {
        public TagHelperAttributeList() { }
        public TagHelperAttributeList(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute> attributes) { }
        public TagHelperAttributeList(System.Collections.Generic.List<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute> attributes) { }
        public new Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute this[int index] { get { throw null; } set { } }
        bool System.Collections.Generic.ICollection<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute>.IsReadOnly { get { throw null; } }
        public void Add(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute attribute) { }
        public void Add(string name, object value) { }
        public void Clear() { }
        public void Insert(int index, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute attribute) { }
        public bool Remove(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute attribute) { throw null; }
        public bool RemoveAll(string name) { throw null; }
        public void RemoveAt(int index) { }
        public void SetAttribute(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute attribute) { }
        public void SetAttribute(string name, object value) { }
    }
    public abstract partial class TagHelperComponent : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent
    {
        protected TagHelperComponent() { }
        public virtual int Order { get { throw null; } }
        public virtual void Init(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context) { }
        public virtual void Process(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output) { }
        public virtual System.Threading.Tasks.Task ProcessAsync(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output) { throw null; }
    }
    public abstract partial class TagHelperContent : Microsoft.AspNetCore.Html.IHtmlContent, Microsoft.AspNetCore.Html.IHtmlContentBuilder, Microsoft.AspNetCore.Html.IHtmlContentContainer
    {
        protected TagHelperContent() { }
        public abstract bool IsEmptyOrWhiteSpace { get; }
        public abstract bool IsModified { get; }
        public abstract Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent Append(string unencoded);
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent AppendFormat(System.IFormatProvider provider, string format, params object[] args) { throw null; }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent AppendFormat(string format, params object[] args) { throw null; }
        public abstract Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent AppendHtml(Microsoft.AspNetCore.Html.IHtmlContent htmlContent);
        public abstract Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent AppendHtml(string encoded);
        public abstract Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent Clear();
        public abstract void CopyTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination);
        public abstract string GetContent();
        public abstract string GetContent(System.Text.Encodings.Web.HtmlEncoder encoder);
        Microsoft.AspNetCore.Html.IHtmlContentBuilder Microsoft.AspNetCore.Html.IHtmlContentBuilder.Append(string unencoded) { throw null; }
        Microsoft.AspNetCore.Html.IHtmlContentBuilder Microsoft.AspNetCore.Html.IHtmlContentBuilder.AppendHtml(Microsoft.AspNetCore.Html.IHtmlContent content) { throw null; }
        Microsoft.AspNetCore.Html.IHtmlContentBuilder Microsoft.AspNetCore.Html.IHtmlContentBuilder.AppendHtml(string encoded) { throw null; }
        Microsoft.AspNetCore.Html.IHtmlContentBuilder Microsoft.AspNetCore.Html.IHtmlContentBuilder.Clear() { throw null; }
        public abstract void MoveTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination);
        public abstract void Reinitialize();
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent SetContent(string unencoded) { throw null; }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent SetHtmlContent(Microsoft.AspNetCore.Html.IHtmlContent htmlContent) { throw null; }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent SetHtmlContent(string encoded) { throw null; }
        public abstract void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder);
    }
    public partial class TagHelperContext
    {
        public TagHelperContext(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttributeList allAttributes, System.Collections.Generic.IDictionary<object, object> items, string uniqueId) { }
        public TagHelperContext(string tagName, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttributeList allAttributes, System.Collections.Generic.IDictionary<object, object> items, string uniqueId) { }
        public Microsoft.AspNetCore.Razor.TagHelpers.ReadOnlyTagHelperAttributeList AllAttributes { get { throw null; } }
        public System.Collections.Generic.IDictionary<object, object> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string TagName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string UniqueId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void Reinitialize(System.Collections.Generic.IDictionary<object, object> items, string uniqueId) { }
        public void Reinitialize(string tagName, System.Collections.Generic.IDictionary<object, object> items, string uniqueId) { }
    }
    public partial class TagHelperOutput : Microsoft.AspNetCore.Html.IHtmlContent, Microsoft.AspNetCore.Html.IHtmlContentContainer
    {
        public TagHelperOutput(string tagName, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttributeList attributes, System.Func<bool, System.Text.Encodings.Web.HtmlEncoder, System.Threading.Tasks.Task<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent>> getChildContentAsync) { }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttributeList Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent Content { get { throw null; } set { } }
        public bool IsContentModified { get { throw null; } }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent PostContent { get { throw null; } }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent PostElement { get { throw null; } }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent PreContent { get { throw null; } }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent PreElement { get { throw null; } }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagMode TagMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string TagName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent> GetChildContentAsync() { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent> GetChildContentAsync(bool useCachedResult) { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent> GetChildContentAsync(bool useCachedResult, System.Text.Encodings.Web.HtmlEncoder encoder) { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent> GetChildContentAsync(System.Text.Encodings.Web.HtmlEncoder encoder) { throw null; }
        void Microsoft.AspNetCore.Html.IHtmlContentContainer.CopyTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination) { }
        void Microsoft.AspNetCore.Html.IHtmlContentContainer.MoveTo(Microsoft.AspNetCore.Html.IHtmlContentBuilder destination) { }
        public void Reinitialize(string tagName, Microsoft.AspNetCore.Razor.TagHelpers.TagMode tagMode) { }
        public void SuppressOutput() { }
        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { }
    }
    public enum TagMode
    {
        StartTagAndEndTag = 0,
        SelfClosing = 1,
        StartTagOnly = 2,
    }
    public enum TagStructure
    {
        Unspecified = 0,
        NormalOrSelfClosing = 1,
        WithoutEndTag = 2,
    }
}
