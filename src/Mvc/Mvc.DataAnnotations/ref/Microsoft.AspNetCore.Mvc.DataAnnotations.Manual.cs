// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    internal partial class RegularExpressionAttributeAdapter : Microsoft.AspNetCore.Mvc.DataAnnotations.AttributeAdapterBase<System.ComponentModel.DataAnnotations.RegularExpressionAttribute>
    {
        public RegularExpressionAttributeAdapter(System.ComponentModel.DataAnnotations.RegularExpressionAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) : base (default(System.ComponentModel.DataAnnotations.RegularExpressionAttribute), default(Microsoft.Extensions.Localization.IStringLocalizer)) { }
        public override void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { }
        public override string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext) { throw null; }
    }
    internal partial class MaxLengthAttributeAdapter : Microsoft.AspNetCore.Mvc.DataAnnotations.AttributeAdapterBase<System.ComponentModel.DataAnnotations.MaxLengthAttribute>
    {
        public MaxLengthAttributeAdapter(System.ComponentModel.DataAnnotations.MaxLengthAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) : base (default(System.ComponentModel.DataAnnotations.MaxLengthAttribute), default(Microsoft.Extensions.Localization.IStringLocalizer)) { }
        public override void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { }
        public override string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext) { throw null; }
    }
    internal partial class MinLengthAttributeAdapter : Microsoft.AspNetCore.Mvc.DataAnnotations.AttributeAdapterBase<System.ComponentModel.DataAnnotations.MinLengthAttribute>
    {
        public MinLengthAttributeAdapter(System.ComponentModel.DataAnnotations.MinLengthAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) : base (default(System.ComponentModel.DataAnnotations.MinLengthAttribute), default(Microsoft.Extensions.Localization.IStringLocalizer)) { }
        public override void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { }
        public override string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext) { throw null; }
    }
    internal partial class RangeAttributeAdapter : Microsoft.AspNetCore.Mvc.DataAnnotations.AttributeAdapterBase<System.ComponentModel.DataAnnotations.RangeAttribute>
    {
        public RangeAttributeAdapter(System.ComponentModel.DataAnnotations.RangeAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) : base (default(System.ComponentModel.DataAnnotations.RangeAttribute), default(Microsoft.Extensions.Localization.IStringLocalizer)) { }
        public override void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { }
        public override string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext) { throw null; }
    }
    internal partial class CompareAttributeAdapter : Microsoft.AspNetCore.Mvc.DataAnnotations.AttributeAdapterBase<System.ComponentModel.DataAnnotations.CompareAttribute>
    {
        public CompareAttributeAdapter(System.ComponentModel.DataAnnotations.CompareAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) : base (default(System.ComponentModel.DataAnnotations.CompareAttribute), default(Microsoft.Extensions.Localization.IStringLocalizer)) { }
        public override void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { }
        public override string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext) { throw null; }
    }
    internal partial class FileExtensionsAttributeAdapter : Microsoft.AspNetCore.Mvc.DataAnnotations.AttributeAdapterBase<System.ComponentModel.DataAnnotations.FileExtensionsAttribute>
    {
        public FileExtensionsAttributeAdapter(System.ComponentModel.DataAnnotations.FileExtensionsAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) : base (default(System.ComponentModel.DataAnnotations.FileExtensionsAttribute), default(Microsoft.Extensions.Localization.IStringLocalizer)) { }
        public override void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { }
        public override string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext) { throw null; }
    }
    internal partial class DataTypeAttributeAdapter : Microsoft.AspNetCore.Mvc.DataAnnotations.AttributeAdapterBase<System.ComponentModel.DataAnnotations.DataTypeAttribute>
    {
        public DataTypeAttributeAdapter(System.ComponentModel.DataAnnotations.DataTypeAttribute attribute, string ruleName, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) : base (default(System.ComponentModel.DataAnnotations.DataTypeAttribute), default(Microsoft.Extensions.Localization.IStringLocalizer)) { }
        public string RuleName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { }
        public override string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext) { throw null; }
    }
    internal partial class NumericClientModelValidator : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidator
    {
        public NumericClientModelValidator() { }
        public void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { }
    }
    internal partial class DataAnnotationsMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IBindingMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IDisplayMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IValidationMetadataProvider
    {
        public DataAnnotationsMetadataProvider(Microsoft.AspNetCore.Mvc.MvcOptions options, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> localizationOptions, Microsoft.Extensions.Localization.IStringLocalizerFactory stringLocalizerFactory) { }
        public void CreateBindingMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadataProviderContext context) { }
        public void CreateDisplayMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DisplayMetadataProviderContext context) { }
        public void CreateValidationMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadataProviderContext context) { }
        internal static bool HasNullableAttribute(System.Collections.Generic.IEnumerable<object> attributes, out bool isNullable) { throw null; }
        internal static bool IsNullableBasedOnContext(System.Type containingType, System.Reflection.MemberInfo member) { throw null; }
        internal static bool IsNullableReferenceType(System.Type containingType, System.Reflection.MemberInfo member, System.Collections.Generic.IEnumerable<object> attributes) { throw null; }
    }
    internal partial class DefaultClientModelValidatorProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidatorProvider
    {
        public DefaultClientModelValidatorProvider() { }
        public void CreateValidators(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientValidatorProviderContext context) { }
    }
    internal partial class DataAnnotationsClientModelValidatorProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidatorProvider
    {
        public DataAnnotationsClientModelValidatorProvider(Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> options, Microsoft.Extensions.Localization.IStringLocalizerFactory stringLocalizerFactory) { }
        public void CreateValidators(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientValidatorProviderContext context) { }
    }
    internal partial class NumericClientModelValidatorProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidatorProvider
    {
        public NumericClientModelValidatorProvider() { }
        public void CreateValidators(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientValidatorProviderContext context) { }
    }
    internal sealed partial class DataAnnotationsModelValidatorProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IMetadataBasedModelValidatorProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidatorProvider
    {
        public DataAnnotationsModelValidatorProvider(Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> options, Microsoft.Extensions.Localization.IStringLocalizerFactory stringLocalizerFactory) { }
        public void CreateValidators(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidatorProviderContext context) { }
        public bool HasValidators(System.Type modelType, System.Collections.Generic.IList<object> validatorMetadata) { throw null; }
    }
    internal partial class StringLengthAttributeAdapter : Microsoft.AspNetCore.Mvc.DataAnnotations.AttributeAdapterBase<System.ComponentModel.DataAnnotations.StringLengthAttribute>
    {
        public StringLengthAttributeAdapter(System.ComponentModel.DataAnnotations.StringLengthAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) : base (default(System.ComponentModel.DataAnnotations.StringLengthAttribute), default(Microsoft.Extensions.Localization.IStringLocalizer)) { }
        public override void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { }
        public override string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext) { throw null; }
    }
    internal partial class DataAnnotationsModelValidator : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidator
    {
        public DataAnnotationsModelValidator(Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider, System.ComponentModel.DataAnnotations.ValidationAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) { }
        public System.ComponentModel.DataAnnotations.ValidationAttribute Attribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationResult> Validate(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContext validationContext) { throw null; }
    }
    internal partial class ValidatableObjectAdapter : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidator
    {
        public ValidatableObjectAdapter() { }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationResult> Validate(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContext context) { throw null; }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    internal partial class MvcDataAnnotationsMvcOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Mvc.MvcOptions>
    {
        public MvcDataAnnotationsMvcOptionsSetup(Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions) { }
        public MvcDataAnnotationsMvcOptionsSetup(Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions, Microsoft.Extensions.Localization.IStringLocalizerFactory stringLocalizerFactory) { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcOptions options) { }
    }
}
