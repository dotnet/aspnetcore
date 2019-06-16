# Microsoft.AspNetCore.Mvc.ModelBinding.Validation

``` diff
 namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation {
     public class ClientModelValidationContext : ModelValidationContextBase {
         public ClientModelValidationContext(ActionContext actionContext, ModelMetadata metadata, IModelMetadataProvider metadataProvider, IDictionary<string, string> attributes);
         public IDictionary<string, string> Attributes { get; }
     }
+    public class ClientValidatorCache {
+        public ClientValidatorCache();
+        public IReadOnlyList<IClientModelValidator> GetValidators(ModelMetadata metadata, IClientModelValidatorProvider validatorProvider);
+    }
     public class ClientValidatorItem {
         public ClientValidatorItem();
         public ClientValidatorItem(object validatorMetadata);
         public bool IsReusable { get; set; }
         public IClientModelValidator Validator { get; set; }
         public object ValidatorMetadata { get; }
     }
     public class ClientValidatorProviderContext {
         public ClientValidatorProviderContext(ModelMetadata modelMetadata, IList<ClientValidatorItem> items);
         public ModelMetadata ModelMetadata { get; }
         public IList<ClientValidatorItem> Results { get; }
         public IReadOnlyList<object> ValidatorMetadata { get; }
     }
     public class CompositeClientModelValidatorProvider : IClientModelValidatorProvider {
         public CompositeClientModelValidatorProvider(IEnumerable<IClientModelValidatorProvider> providers);
         public IReadOnlyList<IClientModelValidatorProvider> ValidatorProviders { get; }
         public void CreateValidators(ClientValidatorProviderContext context);
     }
     public class CompositeModelValidatorProvider : IModelValidatorProvider {
         public CompositeModelValidatorProvider(IList<IModelValidatorProvider> providers);
         public IList<IModelValidatorProvider> ValidatorProviders { get; }
         public void CreateValidators(ModelValidatorProviderContext context);
     }
     public interface IClientModelValidator {
         void AddValidation(ClientModelValidationContext context);
     }
     public interface IClientModelValidatorProvider {
         void CreateValidators(ClientValidatorProviderContext context);
     }
     public interface IMetadataBasedModelValidatorProvider : IModelValidatorProvider {
         bool HasValidators(Type modelType, IList<object> validatorMetadata);
     }
     public interface IModelValidator {
         IEnumerable<ModelValidationResult> Validate(ModelValidationContext context);
     }
     public interface IModelValidatorProvider {
         void CreateValidators(ModelValidatorProviderContext context);
     }
     public interface IObjectModelValidator {
         void Validate(ActionContext actionContext, ValidationStateDictionary validationState, string prefix, object model);
     }
     public interface IPropertyValidationFilter {
         bool ShouldValidateEntry(ValidationEntry entry, ValidationEntry parentEntry);
     }
     public interface IValidationStrategy {
         IEnumerator<ValidationEntry> GetChildren(ModelMetadata metadata, string key, object model);
     }
     public class ModelValidationContext : ModelValidationContextBase {
         public ModelValidationContext(ActionContext actionContext, ModelMetadata modelMetadata, IModelMetadataProvider metadataProvider, object container, object model);
         public object Container { get; }
         public object Model { get; }
     }
     public class ModelValidationContextBase {
         public ModelValidationContextBase(ActionContext actionContext, ModelMetadata modelMetadata, IModelMetadataProvider metadataProvider);
         public ActionContext ActionContext { get; }
         public IModelMetadataProvider MetadataProvider { get; }
         public ModelMetadata ModelMetadata { get; }
     }
     public class ModelValidationResult {
         public ModelValidationResult(string memberName, string message);
         public string MemberName { get; }
         public string Message { get; }
     }
     public class ModelValidatorProviderContext {
         public ModelValidatorProviderContext(ModelMetadata modelMetadata, IList<ValidatorItem> items);
         public ModelMetadata ModelMetadata { get; set; }
         public IList<ValidatorItem> Results { get; }
         public IReadOnlyList<object> ValidatorMetadata { get; }
     }
     public static class ModelValidatorProviderExtensions {
         public static void RemoveType(this IList<IModelValidatorProvider> list, Type type);
         public static void RemoveType<TModelValidatorProvider>(this IList<IModelValidatorProvider> list) where TModelValidatorProvider : IModelValidatorProvider;
     }
     public sealed class ValidateNeverAttribute : Attribute, IPropertyValidationFilter {
         public ValidateNeverAttribute();
         public bool ShouldValidateEntry(ValidationEntry entry, ValidationEntry parentEntry);
     }
     public struct ValidationEntry {
         public ValidationEntry(ModelMetadata metadata, string key, Func<object> modelAccessor);
         public ValidationEntry(ModelMetadata metadata, string key, object model);
         public string Key { get; }
         public ModelMetadata Metadata { get; }
         public object Model { get; }
     }
     public class ValidationStateDictionary : ICollection<KeyValuePair<object, ValidationStateEntry>>, IDictionary<object, ValidationStateEntry>, IEnumerable, IEnumerable<KeyValuePair<object, ValidationStateEntry>>, IReadOnlyCollection<KeyValuePair<object, ValidationStateEntry>>, IReadOnlyDictionary<object, ValidationStateEntry> {
         public ValidationStateDictionary();
         public int Count { get; }
         public bool IsReadOnly { get; }
         public ICollection<object> Keys { get; }
         IEnumerable<object> System.Collections.Generic.IReadOnlyDictionary<System.Object,Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationStateEntry>.Keys { get; }
         IEnumerable<ValidationStateEntry> System.Collections.Generic.IReadOnlyDictionary<System.Object,Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationStateEntry>.Values { get; }
         public ValidationStateEntry this[object key] { get; set; }
         public ICollection<ValidationStateEntry> Values { get; }
         public void Add(KeyValuePair<object, ValidationStateEntry> item);
         public void Add(object key, ValidationStateEntry value);
         public void Clear();
         public bool Contains(KeyValuePair<object, ValidationStateEntry> item);
         public bool ContainsKey(object key);
         public void CopyTo(KeyValuePair<object, ValidationStateEntry>[] array, int arrayIndex);
         public IEnumerator<KeyValuePair<object, ValidationStateEntry>> GetEnumerator();
         public bool Remove(KeyValuePair<object, ValidationStateEntry> item);
         public bool Remove(object key);
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
         public bool TryGetValue(object key, out ValidationStateEntry value);
     }
     public class ValidationStateEntry {
         public ValidationStateEntry();
         public string Key { get; set; }
         public ModelMetadata Metadata { get; set; }
         public IValidationStrategy Strategy { get; set; }
         public bool SuppressValidation { get; set; }
     }
     public class ValidationVisitor {
-        public ValidationVisitor(ActionContext actionContext, IModelValidatorProvider validatorProvider, ValidatorCache validatorCache, IModelMetadataProvider metadataProvider, ValidationStateDictionary validationState);

+        public ValidationVisitor(ActionContext actionContext, IModelValidatorProvider validatorProvider, ValidatorCache validatorCache, IModelMetadataProvider metadataProvider, ValidationStateDictionary validationState);
         public bool AllowShortCircuitingValidationWhenNoValidatorsArePresent { get; set; }
         protected ValidatorCache Cache { get; }
         protected object Container { get; set; }
         protected ActionContext Context { get; }
-        protected ValidationStack CurrentPath { get; }

         protected string Key { get; set; }
         public Nullable<int> MaxValidationDepth { get; set; }
         protected ModelMetadata Metadata { get; set; }
         protected IModelMetadataProvider MetadataProvider { get; }
         protected object Model { get; set; }
         protected ModelStateDictionary ModelState { get; }
         protected IValidationStrategy Strategy { get; set; }
         public bool ValidateComplexTypesIfChildValidationFails { get; set; }
         protected ValidationStateDictionary ValidationState { get; }
         protected IModelValidatorProvider ValidatorProvider { get; }
         protected virtual ValidationStateEntry GetValidationEntry(object model);
         protected virtual void SuppressValidation(string key);
         public bool Validate(ModelMetadata metadata, string key, object model);
         public virtual bool Validate(ModelMetadata metadata, string key, object model, bool alwaysValidateAtTopLevel);
         protected virtual bool ValidateNode();
         protected virtual bool Visit(ModelMetadata metadata, string key, object model);
         protected virtual bool VisitChildren(IValidationStrategy strategy);
         protected virtual bool VisitComplexType(IValidationStrategy defaultStrategy);
         protected virtual bool VisitSimpleType();
         protected readonly struct StateManager : IDisposable {
             public StateManager(ValidationVisitor visitor, object newModel);
             public void Dispose();
             public static ValidationVisitor.StateManager Recurse(ValidationVisitor visitor, string key, ModelMetadata metadata, object model, IValidationStrategy strategy);
         }
     }
+    public class ValidatorCache {
+        public ValidatorCache();
+        public IReadOnlyList<IModelValidator> GetValidators(ModelMetadata metadata, IModelValidatorProvider validatorProvider);
+    }
     public class ValidatorItem {
         public ValidatorItem();
         public ValidatorItem(object validatorMetadata);
         public bool IsReusable { get; set; }
         public IModelValidator Validator { get; set; }
         public object ValidatorMetadata { get; }
     }
 }
```

