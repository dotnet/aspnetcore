# Microsoft.AspNetCore.Mvc.DataAnnotations

``` diff
 namespace Microsoft.AspNetCore.Mvc.DataAnnotations {
     public abstract class AttributeAdapterBase<TAttribute> : ValidationAttributeAdapter<TAttribute>, IAttributeAdapter, IClientModelValidator where TAttribute : ValidationAttribute {
         public AttributeAdapterBase(TAttribute attribute, IStringLocalizer stringLocalizer);
         public abstract string GetErrorMessage(ModelValidationContextBase validationContext);
     }
     public interface IAttributeAdapter : IClientModelValidator {
         string GetErrorMessage(ModelValidationContextBase validationContext);
     }
     public interface IValidationAttributeAdapterProvider {
         IAttributeAdapter GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer stringLocalizer);
     }
     public class MvcDataAnnotationsLocalizationOptions : IEnumerable, IEnumerable<ICompatibilitySwitch> {
         public Func<Type, IStringLocalizerFactory, IStringLocalizer> DataAnnotationLocalizerProvider;
         public MvcDataAnnotationsLocalizationOptions();
-        public bool AllowDataAnnotationsLocalizationForEnumDisplayAttributes { get; set; }

-        public IEnumerator<ICompatibilitySwitch> GetEnumerator();

+        IEnumerator<ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
+    public sealed class RequiredAttributeAdapter : AttributeAdapterBase<RequiredAttribute> {
+        public RequiredAttributeAdapter(RequiredAttribute attribute, IStringLocalizer stringLocalizer);
+        public override void AddValidation(ClientModelValidationContext context);
+        public override string GetErrorMessage(ModelValidationContextBase validationContext);
+    }
     public abstract class ValidationAttributeAdapter<TAttribute> : IClientModelValidator where TAttribute : ValidationAttribute {
         public ValidationAttributeAdapter(TAttribute attribute, IStringLocalizer stringLocalizer);
         public TAttribute Attribute { get; }
         public abstract void AddValidation(ClientModelValidationContext context);
         protected virtual string GetErrorMessage(ModelMetadata modelMetadata, params object[] arguments);
         protected static bool MergeAttribute(IDictionary<string, string> attributes, string key, string value);
     }
     public class ValidationAttributeAdapterProvider : IValidationAttributeAdapterProvider {
         public ValidationAttributeAdapterProvider();
         public IAttributeAdapter GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer stringLocalizer);
     }
     public abstract class ValidationProviderAttribute : Attribute {
         protected ValidationProviderAttribute();
         public abstract IEnumerable<ValidationAttribute> GetValidationAttributes();
     }
 }
```

