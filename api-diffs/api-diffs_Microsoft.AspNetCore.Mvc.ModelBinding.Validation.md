# Microsoft.AspNetCore.Mvc.ModelBinding.Validation

``` diff
 namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation {
+    public class ClientValidatorCache {
+        public ClientValidatorCache();
+        public IReadOnlyList<IClientModelValidator> GetValidators(ModelMetadata metadata, IClientModelValidatorProvider validatorProvider);
+    }
     public class ValidationVisitor {
-        public ValidationVisitor(ActionContext actionContext, IModelValidatorProvider validatorProvider, ValidatorCache validatorCache, IModelMetadataProvider metadataProvider, ValidationStateDictionary validationState);

+        public ValidationVisitor(ActionContext actionContext, IModelValidatorProvider validatorProvider, ValidatorCache validatorCache, IModelMetadataProvider metadataProvider, ValidationStateDictionary validationState);
-        protected ValidationStack CurrentPath { get; }

     }
+    public class ValidatorCache {
+        public ValidatorCache();
+        public IReadOnlyList<IModelValidator> GetValidators(ModelMetadata metadata, IModelValidatorProvider validatorProvider);
+    }
 }
```

