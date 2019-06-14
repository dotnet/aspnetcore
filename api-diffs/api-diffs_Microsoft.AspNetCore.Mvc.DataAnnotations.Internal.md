# Microsoft.AspNetCore.Mvc.DataAnnotations.Internal

``` diff
-namespace Microsoft.AspNetCore.Mvc.DataAnnotations.Internal {
 {
-    public class CompareAttributeAdapter : AttributeAdapterBase<CompareAttribute> {
 {
-        public CompareAttributeAdapter(CompareAttribute attribute, IStringLocalizer stringLocalizer);

-        public override void AddValidation(ClientModelValidationContext context);

-        public override string GetErrorMessage(ModelValidationContextBase validationContext);

-    }
-    public class DataAnnotationsClientModelValidatorProvider : IClientModelValidatorProvider {
 {
-        public DataAnnotationsClientModelValidatorProvider(IValidationAttributeAdapterProvider validationAttributeAdapterProvider, IOptions<MvcDataAnnotationsLocalizationOptions> options, IStringLocalizerFactory stringLocalizerFactory);

-        public void CreateValidators(ClientValidatorProviderContext context);

-    }
-    public static class DataAnnotationsLocalizationServices {
 {
-        public static void AddDataAnnotationsLocalizationServices(IServiceCollection services, Action<MvcDataAnnotationsLocalizationOptions> setupAction);

-    }
-    public class DataAnnotationsMetadataProvider : IBindingMetadataProvider, IDisplayMetadataProvider, IMetadataDetailsProvider, IValidationMetadataProvider {
 {
-        public DataAnnotationsMetadataProvider(IOptions<MvcDataAnnotationsLocalizationOptions> options, IStringLocalizerFactory stringLocalizerFactory);

-        public void CreateBindingMetadata(BindingMetadataProviderContext context);

-        public void CreateDisplayMetadata(DisplayMetadataProviderContext context);

-        public void CreateValidationMetadata(ValidationMetadataProviderContext context);

-    }
-    public class DataAnnotationsModelValidator : IModelValidator {
 {
-        public DataAnnotationsModelValidator(IValidationAttributeAdapterProvider validationAttributeAdapterProvider, ValidationAttribute attribute, IStringLocalizer stringLocalizer);

-        public ValidationAttribute Attribute { get; }

-        public IEnumerable<ModelValidationResult> Validate(ModelValidationContext validationContext);

-    }
-    public class DataTypeAttributeAdapter : AttributeAdapterBase<DataTypeAttribute> {
 {
-        public DataTypeAttributeAdapter(DataTypeAttribute attribute, string ruleName, IStringLocalizer stringLocalizer);

-        public string RuleName { get; }

-        public override void AddValidation(ClientModelValidationContext context);

-        public override string GetErrorMessage(ModelValidationContextBase validationContext);

-    }
-    public class DefaultClientModelValidatorProvider : IClientModelValidatorProvider {
 {
-        public DefaultClientModelValidatorProvider();

-        public void CreateValidators(ClientValidatorProviderContext context);

-    }
-    public class FileExtensionsAttributeAdapter : AttributeAdapterBase<FileExtensionsAttribute> {
 {
-        public FileExtensionsAttributeAdapter(FileExtensionsAttribute attribute, IStringLocalizer stringLocalizer);

-        public override void AddValidation(ClientModelValidationContext context);

-        public override string GetErrorMessage(ModelValidationContextBase validationContext);

-    }
-    public class MaxLengthAttributeAdapter : AttributeAdapterBase<MaxLengthAttribute> {
 {
-        public MaxLengthAttributeAdapter(MaxLengthAttribute attribute, IStringLocalizer stringLocalizer);

-        public override void AddValidation(ClientModelValidationContext context);

-        public override string GetErrorMessage(ModelValidationContextBase validationContext);

-    }
-    public class MinLengthAttributeAdapter : AttributeAdapterBase<MinLengthAttribute> {
 {
-        public MinLengthAttributeAdapter(MinLengthAttribute attribute, IStringLocalizer stringLocalizer);

-        public override void AddValidation(ClientModelValidationContext context);

-        public override string GetErrorMessage(ModelValidationContextBase validationContext);

-    }
-    public class MvcDataAnnotationsLocalizationOptionsSetup : IConfigureOptions<MvcDataAnnotationsLocalizationOptions> {
 {
-        public MvcDataAnnotationsLocalizationOptionsSetup();

-        public void Configure(MvcDataAnnotationsLocalizationOptions options);

-    }
-    public class MvcDataAnnotationsMvcOptionsSetup : IConfigureOptions<MvcOptions> {
 {
-        public MvcDataAnnotationsMvcOptionsSetup(IValidationAttributeAdapterProvider validationAttributeAdapterProvider, IOptions<MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions);

-        public MvcDataAnnotationsMvcOptionsSetup(IValidationAttributeAdapterProvider validationAttributeAdapterProvider, IOptions<MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions, IStringLocalizerFactory stringLocalizerFactory);

-        public void Configure(MvcOptions options);

-    }
-    public class NumericClientModelValidator : IClientModelValidator {
 {
-        public NumericClientModelValidator();

-        public void AddValidation(ClientModelValidationContext context);

-    }
-    public class NumericClientModelValidatorProvider : IClientModelValidatorProvider {
 {
-        public NumericClientModelValidatorProvider();

-        public void CreateValidators(ClientValidatorProviderContext context);

-    }
-    public class RangeAttributeAdapter : AttributeAdapterBase<RangeAttribute> {
 {
-        public RangeAttributeAdapter(RangeAttribute attribute, IStringLocalizer stringLocalizer);

-        public override void AddValidation(ClientModelValidationContext context);

-        public override string GetErrorMessage(ModelValidationContextBase validationContext);

-    }
-    public class RegularExpressionAttributeAdapter : AttributeAdapterBase<RegularExpressionAttribute> {
 {
-        public RegularExpressionAttributeAdapter(RegularExpressionAttribute attribute, IStringLocalizer stringLocalizer);

-        public override void AddValidation(ClientModelValidationContext context);

-        public override string GetErrorMessage(ModelValidationContextBase validationContext);

-    }
-    public class RequiredAttributeAdapter : AttributeAdapterBase<RequiredAttribute> {
 {
-        public RequiredAttributeAdapter(RequiredAttribute attribute, IStringLocalizer stringLocalizer);

-        public override void AddValidation(ClientModelValidationContext context);

-        public override string GetErrorMessage(ModelValidationContextBase validationContext);

-    }
-    public class StringLengthAttributeAdapter : AttributeAdapterBase<StringLengthAttribute> {
 {
-        public StringLengthAttributeAdapter(StringLengthAttribute attribute, IStringLocalizer stringLocalizer);

-        public override void AddValidation(ClientModelValidationContext context);

-        public override string GetErrorMessage(ModelValidationContextBase validationContext);

-    }
-    public class ValidatableObjectAdapter : IModelValidator {
 {
-        public ValidatableObjectAdapter();

-        public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context);

-    }
-}
```

