// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Mvc.ViewFeatures, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]

[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Mvc.DataAnnotations.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]
[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Mvc.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]
[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Mvc.Core.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]
[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Mvc.Core.TestCommon, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]
[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Mvc.IntegrationTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]
[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Mvc.ViewFeatures.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]

[assembly: InternalsVisibleTo("Microsoft.AspNetCore.Mvc.Performance, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    internal partial class DataAnnotationsMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IBindingMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IDisplayMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IValidationMetadataProvider
    {
        private const string NullableAttributeFullTypeName = "System.Runtime.CompilerServices.NullableAttribute";
        private const string NullableContextAttributeFullName = "System.Runtime.CompilerServices.NullableContextAttribute";
        private const string NullableContextFlagsFieldName = "Flag";
        private const string NullableFlagsFieldName = "NullableFlags";
        private readonly Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions _localizationOptions;
        private readonly Microsoft.AspNetCore.Mvc.MvcOptions _options;
        private readonly Microsoft.Extensions.Localization.IStringLocalizerFactory _stringLocalizerFactory;
        public DataAnnotationsMetadataProvider(Microsoft.AspNetCore.Mvc.MvcOptions options, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> localizationOptions, Microsoft.Extensions.Localization.IStringLocalizerFactory stringLocalizerFactory) { }
        public void CreateBindingMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadataProviderContext context) { }
        public void CreateDisplayMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DisplayMetadataProviderContext context) { }
        public void CreateValidationMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ValidationMetadataProviderContext context) { }
        private static string GetDisplayGroup(System.Reflection.FieldInfo field) { throw null; }
        private static string GetDisplayName(System.Reflection.FieldInfo field, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) { throw null; }
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
        private readonly Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> _options;
        private readonly Microsoft.Extensions.Localization.IStringLocalizerFactory _stringLocalizerFactory;
        private readonly Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider _validationAttributeAdapterProvider;
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
        private readonly Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> _options;
        private readonly Microsoft.Extensions.Localization.IStringLocalizerFactory _stringLocalizerFactory;
        private readonly Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider _validationAttributeAdapterProvider;
        public DataAnnotationsModelValidatorProvider(Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> options, Microsoft.Extensions.Localization.IStringLocalizerFactory stringLocalizerFactory) { }
        public void CreateValidators(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidatorProviderContext context) { }
        public bool HasValidators(System.Type modelType, System.Collections.Generic.IList<object> validatorMetadata) { throw null; }
    }
    internal partial class StringLengthAttributeAdapter : Microsoft.AspNetCore.Mvc.DataAnnotations.AttributeAdapterBase<System.ComponentModel.DataAnnotations.StringLengthAttribute>
    {
        private readonly string _max;
        private readonly string _min;
        public StringLengthAttributeAdapter(System.ComponentModel.DataAnnotations.StringLengthAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) : base (default(System.ComponentModel.DataAnnotations.StringLengthAttribute), default(Microsoft.Extensions.Localization.IStringLocalizer)) { }
        public override void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context) { }
        public override string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext) { throw null; }
    }
    internal partial class DataAnnotationsModelValidator : Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IModelValidator
    {
        private static readonly object _emptyValidationContextInstance;
        private readonly Microsoft.Extensions.Localization.IStringLocalizer _stringLocalizer;
        private readonly Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider _validationAttributeAdapterProvider;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        private readonly System.ComponentModel.DataAnnotations.ValidationAttribute _Attribute_k__BackingField;
        public DataAnnotationsModelValidator(Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider, System.ComponentModel.DataAnnotations.ValidationAttribute attribute, Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) { }
        public System.ComponentModel.DataAnnotations.ValidationAttribute Attribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        private string GetErrorMessage(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ModelValidationContextBase validationContext) { throw null; }
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
        private readonly Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> _dataAnnotationLocalizationOptions;
        private readonly Microsoft.Extensions.Localization.IStringLocalizerFactory _stringLocalizerFactory;
        private readonly Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider _validationAttributeAdapterProvider;
        public MvcDataAnnotationsMvcOptionsSetup(Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions) { }
        public MvcDataAnnotationsMvcOptionsSetup(Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider validationAttributeAdapterProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.DataAnnotations.MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions, Microsoft.Extensions.Localization.IStringLocalizerFactory stringLocalizerFactory) { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcOptions options) { }
    }
}