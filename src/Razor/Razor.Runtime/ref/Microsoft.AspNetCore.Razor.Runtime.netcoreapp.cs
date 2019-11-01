// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Hosting
{
    public partial interface IRazorSourceChecksumMetadata
    {
        string Checksum { get; }
        string ChecksumAlgorithm { get; }
        string Identifier { get; }
    }
    public abstract partial class RazorCompiledItem
    {
        protected RazorCompiledItem() { }
        public abstract string Identifier { get; }
        public abstract string Kind { get; }
        public abstract System.Collections.Generic.IReadOnlyList<object> Metadata { get; }
        public abstract System.Type Type { get; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=true, Inherited=false)]
    public sealed partial class RazorCompiledItemAttribute : System.Attribute
    {
        public RazorCompiledItemAttribute(System.Type type, string kind, string identifier) { }
        public string Identifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Kind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Type Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public static partial class RazorCompiledItemExtensions
    {
        public static System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Razor.Hosting.IRazorSourceChecksumMetadata> GetChecksumMetadata(this Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItem item) { throw null; }
    }
    public partial class RazorCompiledItemLoader
    {
        public RazorCompiledItemLoader() { }
        protected virtual Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItem CreateItem(Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute attribute) { throw null; }
        protected System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute> LoadAttributes(System.Reflection.Assembly assembly) { throw null; }
        public virtual System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItem> LoadItems(System.Reflection.Assembly assembly) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
    public sealed partial class RazorCompiledItemMetadataAttribute : System.Attribute
    {
        public RazorCompiledItemMetadataAttribute(string key, string value) { }
        public string Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=false, Inherited=false)]
    public sealed partial class RazorConfigurationNameAttribute : System.Attribute
    {
        public RazorConfigurationNameAttribute(string configurationName) { }
        public string ConfigurationName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=true, Inherited=false)]
    public sealed partial class RazorExtensionAssemblyNameAttribute : System.Attribute
    {
        public RazorExtensionAssemblyNameAttribute(string extensionName, string assemblyName) { }
        public string AssemblyName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ExtensionName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=false, Inherited=false)]
    public sealed partial class RazorLanguageVersionAttribute : System.Attribute
    {
        public RazorLanguageVersionAttribute(string languageVersion) { }
        public string LanguageVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
    public sealed partial class RazorSourceChecksumAttribute : System.Attribute, Microsoft.AspNetCore.Razor.Hosting.IRazorSourceChecksumMetadata
    {
        public RazorSourceChecksumAttribute(string checksumAlgorithm, string checksum, string identifier) { }
        public string Checksum { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ChecksumAlgorithm { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Identifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers
{
    public partial class TagHelperExecutionContext
    {
        public TagHelperExecutionContext(string tagName, Microsoft.AspNetCore.Razor.TagHelpers.TagMode tagMode, System.Collections.Generic.IDictionary<object, object> items, string uniqueId, System.Func<System.Threading.Tasks.Task> executeChildContentAsync, System.Action<System.Text.Encodings.Web.HtmlEncoder> startTagHelperWritingScope, System.Func<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent> endTagHelperWritingScope) { }
        public bool ChildContentRetrieved { get { throw null; } }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IDictionary<object, object> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput Output { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper> TagHelpers { get { throw null; } }
        public void Add(Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper tagHelper) { }
        public void AddHtmlAttribute(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute attribute) { }
        public void AddHtmlAttribute(string name, object value, Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle valueStyle) { }
        public void AddTagHelperAttribute(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute attribute) { }
        public void AddTagHelperAttribute(string name, object value, Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle valueStyle) { }
        public void Reinitialize(string tagName, Microsoft.AspNetCore.Razor.TagHelpers.TagMode tagMode, System.Collections.Generic.IDictionary<object, object> items, string uniqueId, System.Func<System.Threading.Tasks.Task> executeChildContentAsync) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task SetOutputContentAsync() { throw null; }
    }
    public partial class TagHelperRunner
    {
        public TagHelperRunner() { }
        public System.Threading.Tasks.Task RunAsync(Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext executionContext) { throw null; }
    }
    public partial class TagHelperScopeManager
    {
        public TagHelperScopeManager(System.Action<System.Text.Encodings.Web.HtmlEncoder> startTagHelperWritingScope, System.Func<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent> endTagHelperWritingScope) { }
        public Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext Begin(string tagName, Microsoft.AspNetCore.Razor.TagHelpers.TagMode tagMode, string uniqueId, System.Func<System.Threading.Tasks.Task> executeChildContentAsync) { throw null; }
        public Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext End() { throw null; }
    }
}
