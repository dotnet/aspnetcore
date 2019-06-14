# Microsoft.AspNetCore.Mvc.ModelBinding

``` diff
 namespace Microsoft.AspNetCore.Mvc.ModelBinding {
     public class FormValueProvider : BindingSourceValueProvider, IEnumerableValueProvider, IValueProvider
     public abstract class JQueryValueProvider : BindingSourceValueProvider, IEnumerableValueProvider, IKeyRewriterValueProvider, IValueProvider
     public class ModelAttributes {
-        public ModelAttributes(IEnumerable<object> typeAttributes);

-        public ModelAttributes(IEnumerable<object> propertyAttributes, IEnumerable<object> typeAttributes);

     }
     public class ModelBinderFactory : IModelBinderFactory {
-        public ModelBinderFactory(IModelMetadataProvider metadataProvider, IOptions<MvcOptions> options);

     }
     public abstract class ObjectModelValidator : IObjectModelValidator {
-        public abstract ValidationVisitor GetValidationVisitor(ActionContext actionContext, IModelValidatorProvider validatorProvider, ValidatorCache validatorCache, IModelMetadataProvider metadataProvider, ValidationStateDictionary validationState);

+        public abstract ValidationVisitor GetValidationVisitor(ActionContext actionContext, IModelValidatorProvider validatorProvider, ValidatorCache validatorCache, IModelMetadataProvider metadataProvider, ValidationStateDictionary validationState);
     }
     public class ParameterBinder {
-        public ParameterBinder(IModelMetadataProvider modelMetadataProvider, IModelBinderFactory modelBinderFactory, IObjectModelValidator validator);

-        public Task<ModelBindingResult> BindModelAsync(ActionContext actionContext, IValueProvider valueProvider, ParameterDescriptor parameter);

-        public virtual Task<ModelBindingResult> BindModelAsync(ActionContext actionContext, IValueProvider valueProvider, ParameterDescriptor parameter, object value);

     }
+    public class PrefixContainer {
+        public PrefixContainer(ICollection<string> values);
+        public bool ContainsPrefix(string prefix);
+        public IDictionary<string, string> GetKeysFromPrefix(string prefix);
+    }
     public class QueryStringValueProvider : BindingSourceValueProvider, IEnumerableValueProvider, IValueProvider
     public class RouteValueProvider : BindingSourceValueProvider
 }
```

