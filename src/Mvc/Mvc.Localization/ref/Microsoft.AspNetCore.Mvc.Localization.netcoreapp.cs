// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Localization
{
    public partial class HtmlLocalizer : Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer
    {
        public HtmlLocalizer(Microsoft.Extensions.Localization.IStringLocalizer localizer) { }
        public virtual Microsoft.AspNetCore.Mvc.Localization.LocalizedHtmlString this[string name] { get { throw null; } }
        public virtual Microsoft.AspNetCore.Mvc.Localization.LocalizedHtmlString this[string name, params object[] arguments] { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures) { throw null; }
        public virtual Microsoft.Extensions.Localization.LocalizedString GetString(string name) { throw null; }
        public virtual Microsoft.Extensions.Localization.LocalizedString GetString(string name, params object[] arguments) { throw null; }
        protected virtual Microsoft.AspNetCore.Mvc.Localization.LocalizedHtmlString ToHtmlString(Microsoft.Extensions.Localization.LocalizedString result) { throw null; }
        protected virtual Microsoft.AspNetCore.Mvc.Localization.LocalizedHtmlString ToHtmlString(Microsoft.Extensions.Localization.LocalizedString result, object[] arguments) { throw null; }
    }
    public static partial class HtmlLocalizerExtensions
    {
        public static System.Collections.Generic.IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(this Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer htmlLocalizer) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Localization.LocalizedHtmlString GetHtml(this Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer htmlLocalizer, string name) { throw null; }
        public static Microsoft.AspNetCore.Mvc.Localization.LocalizedHtmlString GetHtml(this Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer htmlLocalizer, string name, params object[] arguments) { throw null; }
    }
    public partial class HtmlLocalizerFactory : Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizerFactory
    {
        public HtmlLocalizerFactory(Microsoft.Extensions.Localization.IStringLocalizerFactory localizerFactory) { }
        public virtual Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer Create(string baseName, string location) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer Create(System.Type resourceSource) { throw null; }
    }
    public partial class HtmlLocalizer<TResource> : Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer, Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer<TResource>
    {
        public HtmlLocalizer(Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizerFactory factory) { }
        public virtual Microsoft.AspNetCore.Mvc.Localization.LocalizedHtmlString this[string name] { get { throw null; } }
        public virtual Microsoft.AspNetCore.Mvc.Localization.LocalizedHtmlString this[string name, params object[] arguments] { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures) { throw null; }
        public virtual Microsoft.Extensions.Localization.LocalizedString GetString(string name) { throw null; }
        public virtual Microsoft.Extensions.Localization.LocalizedString GetString(string name, params object[] arguments) { throw null; }
    }
    public partial interface IHtmlLocalizer
    {
        Microsoft.AspNetCore.Mvc.Localization.LocalizedHtmlString this[string name] { get; }
        Microsoft.AspNetCore.Mvc.Localization.LocalizedHtmlString this[string name, params object[] arguments] { get; }
        System.Collections.Generic.IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures);
        Microsoft.Extensions.Localization.LocalizedString GetString(string name);
        Microsoft.Extensions.Localization.LocalizedString GetString(string name, params object[] arguments);
    }
    public partial interface IHtmlLocalizerFactory
    {
        Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer Create(string baseName, string location);
        Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer Create(System.Type resourceSource);
    }
    public partial interface IHtmlLocalizer<TResource> : Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer
    {
    }
    public partial interface IViewLocalizer : Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer
    {
    }
    public partial class LocalizedHtmlString : Microsoft.AspNetCore.Html.IHtmlContent
    {
        public LocalizedHtmlString(string name, string value) { }
        public LocalizedHtmlString(string name, string value, bool isResourceNotFound) { }
        public LocalizedHtmlString(string name, string value, bool isResourceNotFound, params object[] arguments) { }
        public bool IsResourceNotFound { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) { }
    }
    public partial class ViewLocalizer : Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizer, Microsoft.AspNetCore.Mvc.Localization.IViewLocalizer, Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware
    {
        public ViewLocalizer(Microsoft.AspNetCore.Mvc.Localization.IHtmlLocalizerFactory localizerFactory, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment) { }
        public virtual Microsoft.AspNetCore.Mvc.Localization.LocalizedHtmlString this[string key] { get { throw null; } }
        public virtual Microsoft.AspNetCore.Mvc.Localization.LocalizedHtmlString this[string key, params object[] arguments] { get { throw null; } }
        public void Contextualize(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { }
        public System.Collections.Generic.IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures) { throw null; }
        public Microsoft.Extensions.Localization.LocalizedString GetString(string name) { throw null; }
        public Microsoft.Extensions.Localization.LocalizedString GetString(string name, params object[] values) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class MvcLocalizationMvcBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format, System.Action<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> localizationOptionsSetupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> localizationOptionsSetupAction, Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> localizationOptionsSetupAction, Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format, System.Action<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> localizationOptionsSetupAction, System.Action<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddViewLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddViewLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddViewLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddViewLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> setupAction) { throw null; }
    }
    public static partial class MvcLocalizationMvcCoreBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format, System.Action<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> localizationOptionsSetupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> localizationOptionsSetupAction, Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> localizationOptionsSetupAction, Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format, System.Action<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddMvcLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> localizationOptionsSetupAction, System.Action<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddViewLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddViewLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddViewLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat format, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddViewLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> setupAction) { throw null; }
    }
}
