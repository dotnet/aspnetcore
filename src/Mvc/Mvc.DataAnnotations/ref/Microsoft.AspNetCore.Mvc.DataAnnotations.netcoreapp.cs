// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class HiddenInputAttribute : System.Attribute
    {
        public HiddenInputAttribute() { }
        public bool DisplayValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    public abstract partial class AttributeAdapterBase<TAttribute> : Microsoft.AspNetCore.Mvc.DataAnnotations.ValidationAttributeAdapter<TAttribute>, Microsoft.AspNetCore.Mvc.DataAnnotations.IAttributeAdapter, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidator where TAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public AttributeAdapterBase(TAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) : base (default(TAttribute), default(Microsoft.Extensions.Localization.IStringLocalizer)) { }
        public abstract string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext);
    }
    public partial interface IAttributeAdapter : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidator
    {
        string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext);
    }
    public partial interface IValidationAttributeAdapterProvider
    {
        Microsoft.AspNetCore.Mvc.DataAnnotations.IAttributeAdapter GetAttributeAdapter(System.ComponentModel.DataAnnotations.ValidationAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer);
    }
    public partial class MvcDataAnnotationsLocalizationOptions : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>, System.Collections.IEnumerable
    {
        public System.Func<System.Type, Microsoft.Extensions.Localization.IStringLocalizerFactory, Microsoft.Extensions.Localization.IStringLocalizer> DataAnnotationLocalizerProvider;
        public MvcDataAnnotationsLocalizationOptions() { }
        System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class RequiredAttributeAdapter : Microsoft.AspNetCore.Mvc.DataAnnotations.AttributeAdapterBase<System.ComponentModel.DataAnnotations.RequiredAttribute>
    {
        public RequiredAttributeAdapter(System.ComponentModel.DataAnnotations.RequiredAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) : base (default(System.ComponentModel.DataAnnotations.RequiredAttribute), default(Microsoft.Extensions.Localization.IStringLocalizer)) { }
        public override void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { }
        public override string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext) { throw null; }
    }
    public partial class ValidationAttributeAdapterProvider : Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider
    {
        public ValidationAttributeAdapterProvider() { }
        public Microsoft.AspNetCore.Mvc.DataAnnotations.IAttributeAdapter GetAttributeAdapter(System.ComponentModel.DataAnnotations.ValidationAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) { throw null; }
    }
    public abstract partial class ValidationAttributeAdapter<TAttribute> : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidator where TAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public ValidationAttributeAdapter(TAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) { }
        public TAttribute Attribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public abstract void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context);
        protected virtual string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata modelMetadata, params object[] arguments) { throw null; }
        protected static bool MergeAttribute(System.Collections.Generic.IDictionary<string, string> attributes, string key, string value) { throw null; }
    }
    public abstract partial class ValidationProviderAttribute : System.Attribute
    {
        protected ValidationProviderAttribute() { }
        public abstract System.Collections.Generic.IEnumerable<System.ComponentModel.DataAnnotations.ValidationAttribute> GetValidationAttributes();
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class MvcDataAnnotationsMvcBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddDataAnnotationsLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddDataAnnotationsLocalization(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> setupAction) { throw null; }
    }
    public static partial class MvcDataAnnotationsMvcCoreBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddDataAnnotations(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddDataAnnotationsLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddDataAnnotationsLocalization(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> setupAction) { throw null; }
    }
}
