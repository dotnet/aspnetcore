# Microsoft.AspNetCore.Mvc.ModelBinding.Metadata

``` diff
 namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata {
     public class BindingMetadata {
         public BindingMetadata();
         public string BinderModelName { get; set; }
         public Type BinderType { get; set; }
         public BindingSource BindingSource { get; set; }
         public bool IsBindingAllowed { get; set; }
         public bool IsBindingRequired { get; set; }
         public Nullable<bool> IsReadOnly { get; set; }
         public DefaultModelBindingMessageProvider ModelBindingMessageProvider { get; set; }
         public IPropertyFilterProvider PropertyFilterProvider { get; set; }
     }
     public class BindingMetadataProviderContext {
         public BindingMetadataProviderContext(ModelMetadataIdentity key, ModelAttributes attributes);
         public IReadOnlyList<object> Attributes { get; }
         public BindingMetadata BindingMetadata { get; }
         public ModelMetadataIdentity Key { get; }
         public IReadOnlyList<object> ParameterAttributes { get; }
         public IReadOnlyList<object> PropertyAttributes { get; }
         public IReadOnlyList<object> TypeAttributes { get; }
     }
     public class BindingSourceMetadataProvider : IBindingMetadataProvider, IMetadataDetailsProvider {
         public BindingSourceMetadataProvider(Type type, BindingSource bindingSource);
         public BindingSource BindingSource { get; }
         public Type Type { get; }
         public void CreateBindingMetadata(BindingMetadataProviderContext context);
     }
     public class DataMemberRequiredBindingMetadataProvider : IBindingMetadataProvider, IMetadataDetailsProvider {
         public DataMemberRequiredBindingMetadataProvider();
         public void CreateBindingMetadata(BindingMetadataProviderContext context);
     }
     public class DefaultMetadataDetails {
         public DefaultMetadataDetails(ModelMetadataIdentity key, ModelAttributes attributes);
         public BindingMetadata BindingMetadata { get; set; }
         public ModelMetadata ContainerMetadata { get; set; }
         public DisplayMetadata DisplayMetadata { get; set; }
         public ModelMetadataIdentity Key { get; }
         public ModelAttributes ModelAttributes { get; }
         public ModelMetadata[] Properties { get; set; }
         public Func<object, object> PropertyGetter { get; set; }
         public Action<object, object> PropertySetter { get; set; }
         public ValidationMetadata ValidationMetadata { get; set; }
     }
     public class DefaultModelBindingMessageProvider : ModelBindingMessageProvider {
         public DefaultModelBindingMessageProvider();
         public DefaultModelBindingMessageProvider(DefaultModelBindingMessageProvider originalProvider);
         public override Func<string, string, string> AttemptedValueIsInvalidAccessor { get; }
         public override Func<string, string> MissingBindRequiredValueAccessor { get; }
         public override Func<string> MissingKeyOrValueAccessor { get; }
         public override Func<string> MissingRequestBodyRequiredValueAccessor { get; }
         public override Func<string, string> NonPropertyAttemptedValueIsInvalidAccessor { get; }
         public override Func<string> NonPropertyUnknownValueIsInvalidAccessor { get; }
         public override Func<string> NonPropertyValueMustBeANumberAccessor { get; }
         public override Func<string, string> UnknownValueIsInvalidAccessor { get; }
         public override Func<string, string> ValueIsInvalidAccessor { get; }
         public override Func<string, string> ValueMustBeANumberAccessor { get; }
         public override Func<string, string> ValueMustNotBeNullAccessor { get; }
         public void SetAttemptedValueIsInvalidAccessor(Func<string, string, string> attemptedValueIsInvalidAccessor);
         public void SetMissingBindRequiredValueAccessor(Func<string, string> missingBindRequiredValueAccessor);
         public void SetMissingKeyOrValueAccessor(Func<string> missingKeyOrValueAccessor);
         public void SetMissingRequestBodyRequiredValueAccessor(Func<string> missingRequestBodyRequiredValueAccessor);
         public void SetNonPropertyAttemptedValueIsInvalidAccessor(Func<string, string> nonPropertyAttemptedValueIsInvalidAccessor);
         public void SetNonPropertyUnknownValueIsInvalidAccessor(Func<string> nonPropertyUnknownValueIsInvalidAccessor);
         public void SetNonPropertyValueMustBeANumberAccessor(Func<string> nonPropertyValueMustBeANumberAccessor);
         public void SetUnknownValueIsInvalidAccessor(Func<string, string> unknownValueIsInvalidAccessor);
         public void SetValueIsInvalidAccessor(Func<string, string> valueIsInvalidAccessor);
         public void SetValueMustBeANumberAccessor(Func<string, string> valueMustBeANumberAccessor);
         public void SetValueMustNotBeNullAccessor(Func<string, string> valueMustNotBeNullAccessor);
     }
     public class DefaultModelMetadata : ModelMetadata {
         public DefaultModelMetadata(IModelMetadataProvider provider, ICompositeMetadataDetailsProvider detailsProvider, DefaultMetadataDetails details);
         public DefaultModelMetadata(IModelMetadataProvider provider, ICompositeMetadataDetailsProvider detailsProvider, DefaultMetadataDetails details, DefaultModelBindingMessageProvider modelBindingMessageProvider);
         public override IReadOnlyDictionary<object, object> AdditionalValues { get; }
         public ModelAttributes Attributes { get; }
         public override string BinderModelName { get; }
         public override Type BinderType { get; }
         public BindingMetadata BindingMetadata { get; }
         public override BindingSource BindingSource { get; }
         public override ModelMetadata ContainerMetadata { get; }
         public override bool ConvertEmptyStringToNull { get; }
         public override string DataTypeName { get; }
         public override string Description { get; }
         public override string DisplayFormatString { get; }
         public DisplayMetadata DisplayMetadata { get; }
         public override string DisplayName { get; }
         public override string EditFormatString { get; }
         public override ModelMetadata ElementMetadata { get; }
         public override IEnumerable<KeyValuePair<EnumGroupAndName, string>> EnumGroupedDisplayNamesAndValues { get; }
         public override IReadOnlyDictionary<string, string> EnumNamesAndValues { get; }
         public override bool HasNonDefaultEditFormat { get; }
         public override Nullable<bool> HasValidators { get; }
         public override bool HideSurroundingHtml { get; }
         public override bool HtmlEncode { get; }
         public override bool IsBindingAllowed { get; }
         public override bool IsBindingRequired { get; }
         public override bool IsEnum { get; }
         public override bool IsFlagsEnum { get; }
         public override bool IsReadOnly { get; }
         public override bool IsRequired { get; }
         public override ModelBindingMessageProvider ModelBindingMessageProvider { get; }
         public override string NullDisplayText { get; }
         public override int Order { get; }
         public override string Placeholder { get; }
         public override ModelPropertyCollection Properties { get; }
         public override IPropertyFilterProvider PropertyFilterProvider { get; }
         public override Func<object, object> PropertyGetter { get; }
         public override Action<object, object> PropertySetter { get; }
         public override IPropertyValidationFilter PropertyValidationFilter { get; }
         public override bool ShowForDisplay { get; }
         public override bool ShowForEdit { get; }
         public override string SimpleDisplayProperty { get; }
         public override string TemplateHint { get; }
         public override bool ValidateChildren { get; }
         public ValidationMetadata ValidationMetadata { get; }
         public override IReadOnlyList<object> ValidatorMetadata { get; }
         public override IEnumerable<ModelMetadata> GetMetadataForProperties(Type modelType);
         public override ModelMetadata GetMetadataForType(Type modelType);
     }
     public class DefaultModelMetadataProvider : ModelMetadataProvider {
         public DefaultModelMetadataProvider(ICompositeMetadataDetailsProvider detailsProvider);
         public DefaultModelMetadataProvider(ICompositeMetadataDetailsProvider detailsProvider, IOptions<MvcOptions> optionsAccessor);
         protected ICompositeMetadataDetailsProvider DetailsProvider { get; }
         protected DefaultModelBindingMessageProvider ModelBindingMessageProvider { get; }
         protected virtual ModelMetadata CreateModelMetadata(DefaultMetadataDetails entry);
         protected virtual DefaultMetadataDetails CreateParameterDetails(ModelMetadataIdentity key);
         protected virtual DefaultMetadataDetails[] CreatePropertyDetails(ModelMetadataIdentity key);
         protected virtual DefaultMetadataDetails CreateTypeDetails(ModelMetadataIdentity key);
         public override ModelMetadata GetMetadataForParameter(ParameterInfo parameter);
         public override ModelMetadata GetMetadataForParameter(ParameterInfo parameter, Type modelType);
         public override IEnumerable<ModelMetadata> GetMetadataForProperties(Type modelType);
         public override ModelMetadata GetMetadataForProperty(PropertyInfo propertyInfo, Type modelType);
         public override ModelMetadata GetMetadataForType(Type modelType);
     }
     public class DisplayMetadata {
         public DisplayMetadata();
         public IDictionary<object, object> AdditionalValues { get; }
         public bool ConvertEmptyStringToNull { get; set; }
         public string DataTypeName { get; set; }
         public Func<string> Description { get; set; }
         public string DisplayFormatString { get; set; }
         public Func<string> DisplayFormatStringProvider { get; set; }
         public Func<string> DisplayName { get; set; }
         public string EditFormatString { get; set; }
         public Func<string> EditFormatStringProvider { get; set; }
         public IEnumerable<KeyValuePair<EnumGroupAndName, string>> EnumGroupedDisplayNamesAndValues { get; set; }
         public IReadOnlyDictionary<string, string> EnumNamesAndValues { get; set; }
         public bool HasNonDefaultEditFormat { get; set; }
         public bool HideSurroundingHtml { get; set; }
         public bool HtmlEncode { get; set; }
         public bool IsEnum { get; set; }
         public bool IsFlagsEnum { get; set; }
         public string NullDisplayText { get; set; }
         public Func<string> NullDisplayTextProvider { get; set; }
         public int Order { get; set; }
         public Func<string> Placeholder { get; set; }
         public bool ShowForDisplay { get; set; }
         public bool ShowForEdit { get; set; }
         public string SimpleDisplayProperty { get; set; }
         public string TemplateHint { get; set; }
     }
     public class DisplayMetadataProviderContext {
         public DisplayMetadataProviderContext(ModelMetadataIdentity key, ModelAttributes attributes);
         public IReadOnlyList<object> Attributes { get; }
         public DisplayMetadata DisplayMetadata { get; }
         public ModelMetadataIdentity Key { get; }
         public IReadOnlyList<object> PropertyAttributes { get; }
         public IReadOnlyList<object> TypeAttributes { get; }
     }
     public class ExcludeBindingMetadataProvider : IBindingMetadataProvider, IMetadataDetailsProvider {
         public ExcludeBindingMetadataProvider(Type type);
         public void CreateBindingMetadata(BindingMetadataProviderContext context);
     }
     public interface IBindingMetadataProvider : IMetadataDetailsProvider {
         void CreateBindingMetadata(BindingMetadataProviderContext context);
     }
     public interface ICompositeMetadataDetailsProvider : IBindingMetadataProvider, IDisplayMetadataProvider, IMetadataDetailsProvider, IValidationMetadataProvider
     public interface IDisplayMetadataProvider : IMetadataDetailsProvider {
         void CreateDisplayMetadata(DisplayMetadataProviderContext context);
     }
     public interface IMetadataDetailsProvider
     public interface IValidationMetadataProvider : IMetadataDetailsProvider {
         void CreateValidationMetadata(ValidationMetadataProviderContext context);
     }
     public static class MetadataDetailsProviderExtensions {
         public static void RemoveType(this IList<IMetadataDetailsProvider> list, Type type);
         public static void RemoveType<TMetadataDetailsProvider>(this IList<IMetadataDetailsProvider> list) where TMetadataDetailsProvider : IMetadataDetailsProvider;
     }
     public abstract class ModelBindingMessageProvider {
         protected ModelBindingMessageProvider();
         public virtual Func<string, string, string> AttemptedValueIsInvalidAccessor { get; }
         public virtual Func<string, string> MissingBindRequiredValueAccessor { get; }
         public virtual Func<string> MissingKeyOrValueAccessor { get; }
         public virtual Func<string> MissingRequestBodyRequiredValueAccessor { get; }
         public virtual Func<string, string> NonPropertyAttemptedValueIsInvalidAccessor { get; }
         public virtual Func<string> NonPropertyUnknownValueIsInvalidAccessor { get; }
         public virtual Func<string> NonPropertyValueMustBeANumberAccessor { get; }
         public virtual Func<string, string> UnknownValueIsInvalidAccessor { get; }
         public virtual Func<string, string> ValueIsInvalidAccessor { get; }
         public virtual Func<string, string> ValueMustBeANumberAccessor { get; }
         public virtual Func<string, string> ValueMustNotBeNullAccessor { get; }
     }
     public readonly struct ModelMetadataIdentity : IEquatable<ModelMetadataIdentity> {
         public Type ContainerType { get; }
         public ModelMetadataKind MetadataKind { get; }
         public Type ModelType { get; }
         public string Name { get; }
         public ParameterInfo ParameterInfo { get; }
         public bool Equals(ModelMetadataIdentity other);
         public override bool Equals(object obj);
         public static ModelMetadataIdentity ForParameter(ParameterInfo parameter);
         public static ModelMetadataIdentity ForParameter(ParameterInfo parameter, Type modelType);
         public static ModelMetadataIdentity ForProperty(Type modelType, string name, Type containerType);
         public static ModelMetadataIdentity ForType(Type modelType);
         public override int GetHashCode();
     }
     public enum ModelMetadataKind {
         Parameter = 2,
         Property = 1,
         Type = 0,
     }
     public class ValidationMetadata {
         public ValidationMetadata();
         public Nullable<bool> HasValidators { get; set; }
         public Nullable<bool> IsRequired { get; set; }
         public IPropertyValidationFilter PropertyValidationFilter { get; set; }
         public Nullable<bool> ValidateChildren { get; set; }
         public IList<object> ValidatorMetadata { get; }
     }
     public class ValidationMetadataProviderContext {
         public ValidationMetadataProviderContext(ModelMetadataIdentity key, ModelAttributes attributes);
         public IReadOnlyList<object> Attributes { get; }
         public ModelMetadataIdentity Key { get; }
+        public IReadOnlyList<object> ParameterAttributes { get; }
         public IReadOnlyList<object> PropertyAttributes { get; }
         public IReadOnlyList<object> TypeAttributes { get; }
         public ValidationMetadata ValidationMetadata { get; }
     }
 }
```

