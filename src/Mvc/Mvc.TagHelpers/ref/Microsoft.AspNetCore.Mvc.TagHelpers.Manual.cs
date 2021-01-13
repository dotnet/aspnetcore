// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("script", Attributes="asp-append-version")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("script", Attributes="asp-fallback-src")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("script", Attributes="asp-fallback-src-exclude")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("script", Attributes="asp-fallback-src-include")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("script", Attributes="asp-fallback-test")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("script", Attributes="asp-src-exclude")]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("script", Attributes="asp-src-include")]
    public partial class ScriptTagHelper : Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper
    {
        internal Microsoft.AspNetCore.Mvc.ViewFeatures.IFileVersionProvider FileVersionProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class ModeAttributes<TMode>
    {
        public ModeAttributes(TMode mode, string[] attributes) { }
        public string[] Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TMode Mode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class CacheTagHelperMemoryCacheFactory
    {
        internal CacheTagHelperMemoryCacheFactory(Microsoft.Extensions.Caching.Memory.IMemoryCache cache) { }
    }
    internal static partial class AttributeMatcher
    {
        public static bool TryDetermineMode<TMode>(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Mvc.TagHelpers.ModeAttributes<TMode>> modeInfos, System.Func<TMode, TMode, int> compare, out TMode result) { throw null; }
    }
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("link", Attributes="asp-append-version", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("link", Attributes="asp-fallback-href", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("link", Attributes="asp-fallback-href-exclude", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("link", Attributes="asp-fallback-href-include", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("link", Attributes="asp-fallback-test-class", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("link", Attributes="asp-fallback-test-property", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("link", Attributes="asp-fallback-test-value", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("link", Attributes="asp-href-exclude", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("link", Attributes="asp-href-include", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    public partial class LinkTagHelper : Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper
    {
        internal Microsoft.AspNetCore.Mvc.ViewFeatures.IFileVersionProvider FileVersionProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class CacheTagHelper : Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelperBase
    {
        internal Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions GetMemoryCacheEntryOptions() { throw null; }
    }
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("distributed-cache", Attributes="name")]
    public partial class DistributedCacheTagHelper : Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelperBase
    {
        internal Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions GetDistributedCacheEntryOptions() { throw null; }
    }
    public partial class GlobbingUrlBuilder
    {
        internal System.Func<Microsoft.Extensions.FileSystemGlobbing.Matcher> MatcherBuilder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal static partial class JavaScriptResources
    {
        public static string GetEmbeddedJavaScript(string resourceName) { throw null; }
        internal static string GetEmbeddedJavaScript(string resourceName, System.Func<string, System.IO.Stream> getManifestResourceStream, System.Collections.Concurrent.ConcurrentDictionary<string, string> cache) { throw null; }
    }
    internal static partial class Resources
    {
        internal static string AnchorTagHelper_CannotOverrideHref { get { throw null; } }
        internal static string ArgumentCannotContainHtmlSpace { get { throw null; } }
        internal static string CannotDetermineAttributeFor { get { throw null; } }
        internal static System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal static string FormActionTagHelper_CannotOverrideFormAction { get { throw null; } }
        internal static string FormTagHelper_CannotOverrideAction { get { throw null; } }
        internal static string InputTagHelper_InvalidExpressionResult { get { throw null; } }
        internal static string InputTagHelper_InvalidStringResult { get { throw null; } }
        internal static string InputTagHelper_ValueRequired { get { throw null; } }
        internal static string InvalidEnumArgument { get { throw null; } }
        internal static string PartialTagHelper_InvalidModelAttributes { get { throw null; } }
        internal static string PropertyOfTypeCannotBeNull { get { throw null; } }
        internal static System.Resources.ResourceManager ResourceManager { get { throw null; } }
        internal static string TagHelperOutput_AttributeDoesNotExist { get { throw null; } }
        internal static string TagHelpers_NoProvidedMetadata { get { throw null; } }
        internal static string ViewEngine_FallbackViewNotFound { get { throw null; } }
        internal static string ViewEngine_PartialViewNotFound { get { throw null; } }
        internal static string FormatAnchorTagHelper_CannotOverrideHref(object p0, object p1, object p2, object p3, object p4, object p5, object p6, object p7, object p8, object p9, object p10, object p11) { throw null; }
        internal static string FormatCannotDetermineAttributeFor(object p0, object p1) { throw null; }
        internal static string FormatFormActionTagHelper_CannotOverrideFormAction(object p0, object p1, object p2, object p3, object p4, object p5, object p6, object p7, object p8, object p9) { throw null; }
        internal static string FormatFormTagHelper_CannotOverrideAction(object p0, object p1, object p2, object p3, object p4, object p5, object p6, object p7, object p8, object p9) { throw null; }
        internal static string FormatInputTagHelper_InvalidExpressionResult(object p0, object p1, object p2, object p3, object p4, object p5, object p6) { throw null; }
        internal static string FormatInputTagHelper_InvalidStringResult(object p0, object p1, object p2) { throw null; }
        internal static string FormatInputTagHelper_ValueRequired(object p0, object p1, object p2, object p3) { throw null; }
        internal static string FormatInvalidEnumArgument(object p0, object p1, object p2) { throw null; }
        internal static string FormatPartialTagHelper_InvalidModelAttributes(object p0, object p1, object p2) { throw null; }
        internal static string FormatPropertyOfTypeCannotBeNull(object p0, object p1) { throw null; }
        internal static string FormatTagHelperOutput_AttributeDoesNotExist(object p0, object p1) { throw null; }
        internal static string FormatTagHelpers_NoProvidedMetadata(object p0, object p1, object p2, object p3) { throw null; }
        internal static string FormatViewEngine_FallbackViewNotFound(object p0, object p1) { throw null; }
        internal static string FormatViewEngine_PartialViewNotFound(object p0, object p1) { throw null; }
    }
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("partial", Attributes="name", TagStructure=Microsoft.AspNetCore.Razor.TagHelpers.TagStructure.WithoutEndTag)]
    public partial class PartialTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
    {
        internal object ResolveModel() { throw null; }
    }
    internal partial class CurrentValues
    {
        public CurrentValues(System.Collections.Generic.ICollection<string> values) { }
        public System.Collections.Generic.ICollection<string> Values { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.ICollection<string> ValuesAndEncodedValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Mvc.TagHelpers.Cache
{
    public partial class CacheTagKey : System.IEquatable<Microsoft.AspNetCore.Mvc.TagHelpers.Cache.CacheTagKey>
    {
        internal string Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}