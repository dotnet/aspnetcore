# Microsoft.AspNetCore.Mvc.ModelBinding

``` diff
 namespace Microsoft.AspNetCore.Mvc.ModelBinding {
     public enum BindingBehavior {
         Never = 1,
         Optional = 0,
         Required = 2,
     }
     public class BindingBehaviorAttribute : Attribute {
         public BindingBehaviorAttribute(BindingBehavior behavior);
         public BindingBehavior Behavior { get; }
     }
     public class BindingInfo {
         public BindingInfo();
         public BindingInfo(BindingInfo other);
         public string BinderModelName { get; set; }
         public Type BinderType { get; set; }
         public BindingSource BindingSource { get; set; }
         public IPropertyFilterProvider PropertyFilterProvider { get; set; }
         public Func<ActionContext, bool> RequestPredicate { get; set; }
         public static BindingInfo GetBindingInfo(IEnumerable<object> attributes);
         public static BindingInfo GetBindingInfo(IEnumerable<object> attributes, ModelMetadata modelMetadata);
         public bool TryApplyBindingInfo(ModelMetadata modelMetadata);
     }
     public class BindingSource : IEquatable<BindingSource> {
         public static readonly BindingSource Body;
         public static readonly BindingSource Custom;
         public static readonly BindingSource Form;
         public static readonly BindingSource FormFile;
         public static readonly BindingSource Header;
         public static readonly BindingSource ModelBinding;
         public static readonly BindingSource Path;
         public static readonly BindingSource Query;
         public static readonly BindingSource Services;
         public static readonly BindingSource Special;
         public BindingSource(string id, string displayName, bool isGreedy, bool isFromRequest);
         public string DisplayName { get; }
         public string Id { get; }
         public bool IsFromRequest { get; }
         public bool IsGreedy { get; }
         public virtual bool CanAcceptDataFrom(BindingSource bindingSource);
         public bool Equals(BindingSource other);
         public override bool Equals(object obj);
         public override int GetHashCode();
         public static bool operator ==(BindingSource s1, BindingSource s2);
         public static bool operator !=(BindingSource s1, BindingSource s2);
     }
     public abstract class BindingSourceValueProvider : IBindingSourceValueProvider, IValueProvider {
         public BindingSourceValueProvider(BindingSource bindingSource);
         protected BindingSource BindingSource { get; }
         public abstract bool ContainsPrefix(string prefix);
         public virtual IValueProvider Filter(BindingSource bindingSource);
         public abstract ValueProviderResult GetValue(string key);
     }
     public sealed class BindNeverAttribute : BindingBehaviorAttribute {
         public BindNeverAttribute();
     }
     public sealed class BindRequiredAttribute : BindingBehaviorAttribute {
         public BindRequiredAttribute();
     }
     public class CompositeBindingSource : BindingSource {
         public IEnumerable<BindingSource> BindingSources { get; }
         public override bool CanAcceptDataFrom(BindingSource bindingSource);
         public static CompositeBindingSource Create(IEnumerable<BindingSource> bindingSources, string displayName);
     }
     public class CompositeValueProvider : Collection<IValueProvider>, IBindingSourceValueProvider, IEnumerableValueProvider, IKeyRewriterValueProvider, IValueProvider {
         public CompositeValueProvider();
         public CompositeValueProvider(IList<IValueProvider> valueProviders);
         public virtual bool ContainsPrefix(string prefix);
         public static Task<CompositeValueProvider> CreateAsync(ActionContext actionContext, IList<IValueProviderFactory> factories);
         public static Task<CompositeValueProvider> CreateAsync(ControllerContext controllerContext);
         public IValueProvider Filter();
         public IValueProvider Filter(BindingSource bindingSource);
         public virtual IDictionary<string, string> GetKeysFromPrefix(string prefix);
         public virtual ValueProviderResult GetValue(string key);
         protected override void InsertItem(int index, IValueProvider item);
         protected override void SetItem(int index, IValueProvider item);
     }
     public class DefaultModelBindingContext : ModelBindingContext {
         public DefaultModelBindingContext();
         public override ActionContext ActionContext { get; set; }
         public override string BinderModelName { get; set; }
         public override BindingSource BindingSource { get; set; }
         public override string FieldName { get; set; }
         public override bool IsTopLevelObject { get; set; }
         public override object Model { get; set; }
         public override ModelMetadata ModelMetadata { get; set; }
         public override string ModelName { get; set; }
         public override ModelStateDictionary ModelState { get; set; }
         public IValueProvider OriginalValueProvider { get; set; }
         public override Func<ModelMetadata, bool> PropertyFilter { get; set; }
         public override ModelBindingResult Result { get; set; }
         public override ValidationStateDictionary ValidationState { get; set; }
         public override IValueProvider ValueProvider { get; set; }
         public static ModelBindingContext CreateBindingContext(ActionContext actionContext, IValueProvider valueProvider, ModelMetadata metadata, BindingInfo bindingInfo, string modelName);
         public override ModelBindingContext.NestedScope EnterNestedScope();
         public override ModelBindingContext.NestedScope EnterNestedScope(ModelMetadata modelMetadata, string fieldName, string modelName, object model);
         protected override void ExitNestedScope();
     }
     public class DefaultPropertyFilterProvider<TModel> : IPropertyFilterProvider where TModel : class {
         public DefaultPropertyFilterProvider();
         public virtual string Prefix { get; }
         public virtual Func<ModelMetadata, bool> PropertyFilter { get; }
         public virtual IEnumerable<Expression<Func<TModel, object>>> PropertyIncludeExpressions { get; }
     }
     public class EmptyModelMetadataProvider : DefaultModelMetadataProvider {
         public EmptyModelMetadataProvider();
     }
     public readonly struct EnumGroupAndName {
         public EnumGroupAndName(string group, Func<string> name);
         public EnumGroupAndName(string group, string name);
         public string Group { get; }
         public string Name { get; }
     }
     public class FormValueProvider : BindingSourceValueProvider, IEnumerableValueProvider, IValueProvider {
         public FormValueProvider(BindingSource bindingSource, IFormCollection values, CultureInfo culture);
         public CultureInfo Culture { get; }
         protected PrefixContainer PrefixContainer { get; }
         public override bool ContainsPrefix(string prefix);
         public virtual IDictionary<string, string> GetKeysFromPrefix(string prefix);
         public override ValueProviderResult GetValue(string key);
     }
     public class FormValueProviderFactory : IValueProviderFactory {
         public FormValueProviderFactory();
         public Task CreateValueProviderAsync(ValueProviderFactoryContext context);
     }
     public interface IBinderTypeProviderMetadata : IBindingSourceMetadata {
         Type BinderType { get; }
     }
     public interface IBindingSourceMetadata {
         BindingSource BindingSource { get; }
     }
     public interface IBindingSourceValueProvider : IValueProvider {
         IValueProvider Filter(BindingSource bindingSource);
     }
     public interface ICollectionModelBinder : IModelBinder {
         bool CanCreateInstance(Type targetType);
     }
     public interface IEnumerableValueProvider : IValueProvider {
         IDictionary<string, string> GetKeysFromPrefix(string prefix);
     }
     public interface IKeyRewriterValueProvider : IValueProvider {
         IValueProvider Filter();
     }
     public interface IModelBinder {
         Task BindModelAsync(ModelBindingContext bindingContext);
     }
     public interface IModelBinderFactory {
         IModelBinder CreateBinder(ModelBinderFactoryContext context);
     }
     public interface IModelBinderProvider {
         IModelBinder GetBinder(ModelBinderProviderContext context);
     }
     public interface IModelMetadataProvider {
         IEnumerable<ModelMetadata> GetMetadataForProperties(Type modelType);
         ModelMetadata GetMetadataForType(Type modelType);
     }
     public interface IModelNameProvider {
         string Name { get; }
     }
     public interface IPropertyFilterProvider {
         Func<ModelMetadata, bool> PropertyFilter { get; }
     }
     public interface IRequestPredicateProvider {
         Func<ActionContext, bool> RequestPredicate { get; }
     }
     public interface IValueProvider {
         bool ContainsPrefix(string prefix);
         ValueProviderResult GetValue(string key);
     }
     public interface IValueProviderFactory {
         Task CreateValueProviderAsync(ValueProviderFactoryContext context);
     }
     public class JQueryFormValueProvider : JQueryValueProvider {
         public JQueryFormValueProvider(BindingSource bindingSource, IDictionary<string, StringValues> values, CultureInfo culture);
     }
     public class JQueryFormValueProviderFactory : IValueProviderFactory {
         public JQueryFormValueProviderFactory();
         public Task CreateValueProviderAsync(ValueProviderFactoryContext context);
     }
     public class JQueryQueryStringValueProvider : JQueryValueProvider {
         public JQueryQueryStringValueProvider(BindingSource bindingSource, IDictionary<string, StringValues> values, CultureInfo culture);
     }
     public class JQueryQueryStringValueProviderFactory : IValueProviderFactory {
         public JQueryQueryStringValueProviderFactory();
         public Task CreateValueProviderAsync(ValueProviderFactoryContext context);
     }
     public abstract class JQueryValueProvider : BindingSourceValueProvider, IEnumerableValueProvider, IKeyRewriterValueProvider, IValueProvider {
         protected JQueryValueProvider(BindingSource bindingSource, IDictionary<string, StringValues> values, CultureInfo culture);
         public CultureInfo Culture { get; }
         protected PrefixContainer PrefixContainer { get; }
         public override bool ContainsPrefix(string prefix);
         public IValueProvider Filter();
         public IDictionary<string, string> GetKeysFromPrefix(string prefix);
         public override ValueProviderResult GetValue(string key);
     }
     public class ModelAttributes {
-        public ModelAttributes(IEnumerable<object> typeAttributes);

-        public ModelAttributes(IEnumerable<object> propertyAttributes, IEnumerable<object> typeAttributes);

         public IReadOnlyList<object> Attributes { get; }
         public IReadOnlyList<object> ParameterAttributes { get; }
         public IReadOnlyList<object> PropertyAttributes { get; }
         public IReadOnlyList<object> TypeAttributes { get; }
         public static ModelAttributes GetAttributesForParameter(ParameterInfo parameterInfo);
         public static ModelAttributes GetAttributesForParameter(ParameterInfo parameterInfo, Type modelType);
         public static ModelAttributes GetAttributesForProperty(Type type, PropertyInfo property);
         public static ModelAttributes GetAttributesForProperty(Type containerType, PropertyInfo property, Type modelType);
         public static ModelAttributes GetAttributesForType(Type type);
     }
     public class ModelBinderFactory : IModelBinderFactory {
-        public ModelBinderFactory(IModelMetadataProvider metadataProvider, IOptions<MvcOptions> options);

         public ModelBinderFactory(IModelMetadataProvider metadataProvider, IOptions<MvcOptions> options, IServiceProvider serviceProvider);
         public IModelBinder CreateBinder(ModelBinderFactoryContext context);
     }
     public class ModelBinderFactoryContext {
         public ModelBinderFactoryContext();
         public BindingInfo BindingInfo { get; set; }
         public object CacheToken { get; set; }
         public ModelMetadata Metadata { get; set; }
     }
     public abstract class ModelBinderProviderContext {
         protected ModelBinderProviderContext();
         public abstract BindingInfo BindingInfo { get; }
         public abstract ModelMetadata Metadata { get; }
         public abstract IModelMetadataProvider MetadataProvider { get; }
         public virtual IServiceProvider Services { get; }
         public abstract IModelBinder CreateBinder(ModelMetadata metadata);
         public virtual IModelBinder CreateBinder(ModelMetadata metadata, BindingInfo bindingInfo);
     }
     public static class ModelBinderProviderExtensions {
         public static void RemoveType(this IList<IModelBinderProvider> list, Type type);
         public static void RemoveType<TModelBinderProvider>(this IList<IModelBinderProvider> list) where TModelBinderProvider : IModelBinderProvider;
     }
     public abstract class ModelBindingContext {
         protected ModelBindingContext();
         public abstract ActionContext ActionContext { get; set; }
         public abstract string BinderModelName { get; set; }
         public abstract BindingSource BindingSource { get; set; }
         public abstract string FieldName { get; set; }
         public virtual HttpContext HttpContext { get; }
         public abstract bool IsTopLevelObject { get; set; }
         public abstract object Model { get; set; }
         public abstract ModelMetadata ModelMetadata { get; set; }
         public abstract string ModelName { get; set; }
         public abstract ModelStateDictionary ModelState { get; set; }
         public virtual Type ModelType { get; }
         public string OriginalModelName { get; protected set; }
         public abstract Func<ModelMetadata, bool> PropertyFilter { get; set; }
         public abstract ModelBindingResult Result { get; set; }
         public abstract ValidationStateDictionary ValidationState { get; set; }
         public abstract IValueProvider ValueProvider { get; set; }
         public abstract ModelBindingContext.NestedScope EnterNestedScope();
         public abstract ModelBindingContext.NestedScope EnterNestedScope(ModelMetadata modelMetadata, string fieldName, string modelName, object model);
         protected abstract void ExitNestedScope();
         public readonly struct NestedScope : IDisposable {
             public NestedScope(ModelBindingContext context);
             public void Dispose();
         }
     }
     public readonly struct ModelBindingResult : IEquatable<ModelBindingResult> {
         public bool IsModelSet { get; }
         public object Model { get; }
         public bool Equals(ModelBindingResult other);
         public override bool Equals(object obj);
         public static ModelBindingResult Failed();
         public override int GetHashCode();
         public static bool operator ==(ModelBindingResult x, ModelBindingResult y);
         public static bool operator !=(ModelBindingResult x, ModelBindingResult y);
         public static ModelBindingResult Success(object model);
         public override string ToString();
     }
     public class ModelError {
         public ModelError(Exception exception);
         public ModelError(Exception exception, string errorMessage);
         public ModelError(string errorMessage);
         public string ErrorMessage { get; }
         public Exception Exception { get; }
     }
     public class ModelErrorCollection : Collection<ModelError> {
         public ModelErrorCollection();
         public void Add(Exception exception);
         public void Add(string errorMessage);
     }
     public abstract class ModelMetadata : IEquatable<ModelMetadata>, IModelMetadataProvider {
         public static readonly int DefaultOrder;
         protected ModelMetadata(ModelMetadataIdentity identity);
         public abstract IReadOnlyDictionary<object, object> AdditionalValues { get; }
         public abstract string BinderModelName { get; }
         public abstract Type BinderType { get; }
         public abstract BindingSource BindingSource { get; }
         public virtual ModelMetadata ContainerMetadata { get; }
         public Type ContainerType { get; }
         public abstract bool ConvertEmptyStringToNull { get; }
         public abstract string DataTypeName { get; }
         public abstract string Description { get; }
         public abstract string DisplayFormatString { get; }
         public abstract string DisplayName { get; }
         public abstract string EditFormatString { get; }
         public abstract ModelMetadata ElementMetadata { get; }
         public Type ElementType { get; private set; }
         public abstract IEnumerable<KeyValuePair<EnumGroupAndName, string>> EnumGroupedDisplayNamesAndValues { get; }
         public abstract IReadOnlyDictionary<string, string> EnumNamesAndValues { get; }
         public abstract bool HasNonDefaultEditFormat { get; }
         public virtual Nullable<bool> HasValidators { get; }
         public abstract bool HideSurroundingHtml { get; }
         public abstract bool HtmlEncode { get; }
         protected ModelMetadataIdentity Identity { get; }
         public abstract bool IsBindingAllowed { get; }
         public abstract bool IsBindingRequired { get; }
         public bool IsCollectionType { get; private set; }
         public bool IsComplexType { get; private set; }
         public abstract bool IsEnum { get; }
         public bool IsEnumerableType { get; private set; }
         public abstract bool IsFlagsEnum { get; }
         public bool IsNullableValueType { get; private set; }
         public abstract bool IsReadOnly { get; }
         public bool IsReferenceOrNullableType { get; private set; }
         public abstract bool IsRequired { get; }
         public ModelMetadataKind MetadataKind { get; }
         public abstract ModelBindingMessageProvider ModelBindingMessageProvider { get; }
         public Type ModelType { get; }
         public string Name { get; }
         public abstract string NullDisplayText { get; }
         public abstract int Order { get; }
         public string ParameterName { get; }
         public abstract string Placeholder { get; }
         public abstract ModelPropertyCollection Properties { get; }
         public abstract IPropertyFilterProvider PropertyFilterProvider { get; }
         public abstract Func<object, object> PropertyGetter { get; }
         public string PropertyName { get; }
         public abstract Action<object, object> PropertySetter { get; }
         public virtual IPropertyValidationFilter PropertyValidationFilter { get; }
         public abstract bool ShowForDisplay { get; }
         public abstract bool ShowForEdit { get; }
         public abstract string SimpleDisplayProperty { get; }
         public abstract string TemplateHint { get; }
         public Type UnderlyingOrModelType { get; private set; }
         public abstract bool ValidateChildren { get; }
         public abstract IReadOnlyList<object> ValidatorMetadata { get; }
         public bool Equals(ModelMetadata other);
         public override bool Equals(object obj);
         public string GetDisplayName();
         public override int GetHashCode();
         public virtual IEnumerable<ModelMetadata> GetMetadataForProperties(Type modelType);
         public virtual ModelMetadata GetMetadataForType(Type modelType);
     }
     public abstract class ModelMetadataProvider : IModelMetadataProvider {
         protected ModelMetadataProvider();
         public abstract ModelMetadata GetMetadataForParameter(ParameterInfo parameter);
         public virtual ModelMetadata GetMetadataForParameter(ParameterInfo parameter, Type modelType);
         public abstract IEnumerable<ModelMetadata> GetMetadataForProperties(Type modelType);
         public virtual ModelMetadata GetMetadataForProperty(PropertyInfo propertyInfo, Type modelType);
         public abstract ModelMetadata GetMetadataForType(Type modelType);
     }
     public static class ModelMetadataProviderExtensions {
         public static ModelMetadata GetMetadataForProperty(this IModelMetadataProvider provider, Type containerType, string propertyName);
     }
     public static class ModelNames {
         public static string CreateIndexModelName(string parentName, int index);
         public static string CreateIndexModelName(string parentName, string index);
         public static string CreatePropertyModelName(string prefix, string propertyName);
     }
     public class ModelPropertyCollection : ReadOnlyCollection<ModelMetadata> {
         public ModelPropertyCollection(IEnumerable<ModelMetadata> properties);
         public ModelMetadata this[string propertyName] { get; }
     }
     public class ModelStateDictionary : IEnumerable, IEnumerable<KeyValuePair<string, ModelStateEntry>>, IReadOnlyCollection<KeyValuePair<string, ModelStateEntry>>, IReadOnlyDictionary<string, ModelStateEntry> {
         public static readonly int DefaultMaxAllowedErrors;
         public ModelStateDictionary();
         public ModelStateDictionary(ModelStateDictionary dictionary);
         public ModelStateDictionary(int maxAllowedErrors);
         public int Count { get; private set; }
         public int ErrorCount { get; private set; }
         public bool HasReachedMaxErrors { get; }
         public bool IsValid { get; }
         public ModelStateDictionary.KeyEnumerable Keys { get; }
         public int MaxAllowedErrors { get; set; }
         public ModelStateEntry Root { get; }
         IEnumerable<string> System.Collections.Generic.IReadOnlyDictionary<System.String,Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry>.Keys { get; }
         IEnumerable<ModelStateEntry> System.Collections.Generic.IReadOnlyDictionary<System.String,Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry>.Values { get; }
         public ModelStateEntry this[string key] { get; }
         public ModelValidationState ValidationState { get; }
         public ModelStateDictionary.ValueEnumerable Values { get; }
         public void AddModelError(string key, Exception exception, ModelMetadata metadata);
         public void AddModelError(string key, string errorMessage);
         public void Clear();
         public void ClearValidationState(string key);
         public bool ContainsKey(string key);
         public ModelStateDictionary.PrefixEnumerable FindKeysWithPrefix(string prefix);
         public ModelStateDictionary.Enumerator GetEnumerator();
         public ModelValidationState GetFieldValidationState(string key);
         public ModelValidationState GetValidationState(string key);
         public void MarkFieldSkipped(string key);
         public void MarkFieldValid(string key);
         public void Merge(ModelStateDictionary dictionary);
         public bool Remove(string key);
         public void SetModelValue(string key, ValueProviderResult valueProviderResult);
         public void SetModelValue(string key, object rawValue, string attemptedValue);
         public static bool StartsWithPrefix(string prefix, string key);
         IEnumerator<KeyValuePair<string, ModelStateEntry>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry>>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
         public bool TryAddModelError(string key, Exception exception, ModelMetadata metadata);
         public bool TryAddModelError(string key, string errorMessage);
         public bool TryAddModelException(string key, Exception exception);
         public bool TryGetValue(string key, out ModelStateEntry value);
         public struct Enumerator : IDisposable, IEnumerator, IEnumerator<KeyValuePair<string, ModelStateEntry>> {
             public Enumerator(ModelStateDictionary dictionary, string prefix);
             public KeyValuePair<string, ModelStateEntry> Current { get; }
             object System.Collections.IEnumerator.Current { get; }
             public void Dispose();
             public bool MoveNext();
             public void Reset();
         }
         public readonly struct KeyEnumerable : IEnumerable, IEnumerable<string> {
             public KeyEnumerable(ModelStateDictionary dictionary);
             public ModelStateDictionary.KeyEnumerator GetEnumerator();
             IEnumerator<string> System.Collections.Generic.IEnumerable<System.String>.GetEnumerator();
             IEnumerator System.Collections.IEnumerable.GetEnumerator();
         }
         public struct KeyEnumerator : IDisposable, IEnumerator, IEnumerator<string> {
             public KeyEnumerator(ModelStateDictionary dictionary, string prefix);
             public string Current { get; private set; }
             object System.Collections.IEnumerator.Current { get; }
             public void Dispose();
             public bool MoveNext();
             public void Reset();
         }
         public readonly struct PrefixEnumerable : IEnumerable, IEnumerable<KeyValuePair<string, ModelStateEntry>> {
             public PrefixEnumerable(ModelStateDictionary dictionary, string prefix);
             public ModelStateDictionary.Enumerator GetEnumerator();
             IEnumerator<KeyValuePair<string, ModelStateEntry>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry>>.GetEnumerator();
             IEnumerator System.Collections.IEnumerable.GetEnumerator();
         }
         public readonly struct ValueEnumerable : IEnumerable, IEnumerable<ModelStateEntry> {
             public ValueEnumerable(ModelStateDictionary dictionary);
             public ModelStateDictionary.ValueEnumerator GetEnumerator();
             IEnumerator<ModelStateEntry> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry>.GetEnumerator();
             IEnumerator System.Collections.IEnumerable.GetEnumerator();
         }
         public struct ValueEnumerator : IDisposable, IEnumerator, IEnumerator<ModelStateEntry> {
             public ValueEnumerator(ModelStateDictionary dictionary, string prefix);
             public ModelStateEntry Current { get; private set; }
             object System.Collections.IEnumerator.Current { get; }
             public void Dispose();
             public bool MoveNext();
             public void Reset();
         }
     }
     public static class ModelStateDictionaryExtensions {
         public static void AddModelError<TModel>(this ModelStateDictionary modelState, Expression<Func<TModel, object>> expression, Exception exception, ModelMetadata metadata);
         public static void AddModelError<TModel>(this ModelStateDictionary modelState, Expression<Func<TModel, object>> expression, string errorMessage);
         public static bool Remove<TModel>(this ModelStateDictionary modelState, Expression<Func<TModel, object>> expression);
         public static void RemoveAll<TModel>(this ModelStateDictionary modelState, Expression<Func<TModel, object>> expression);
         public static void TryAddModelException<TModel>(this ModelStateDictionary modelState, Expression<Func<TModel, object>> expression, Exception exception);
     }
     public abstract class ModelStateEntry {
         protected ModelStateEntry();
         public string AttemptedValue { get; set; }
         public abstract IReadOnlyList<ModelStateEntry> Children { get; }
         public ModelErrorCollection Errors { get; }
         public abstract bool IsContainerNode { get; }
         public object RawValue { get; set; }
         public ModelValidationState ValidationState { get; set; }
         public abstract ModelStateEntry GetModelStateForProperty(string propertyName);
     }
     public enum ModelValidationState {
         Invalid = 1,
         Skipped = 3,
         Unvalidated = 0,
         Valid = 2,
     }
     public abstract class ObjectModelValidator : IObjectModelValidator {
         public ObjectModelValidator(IModelMetadataProvider modelMetadataProvider, IList<IModelValidatorProvider> validatorProviders);
-        public abstract ValidationVisitor GetValidationVisitor(ActionContext actionContext, IModelValidatorProvider validatorProvider, ValidatorCache validatorCache, IModelMetadataProvider metadataProvider, ValidationStateDictionary validationState);

+        public abstract ValidationVisitor GetValidationVisitor(ActionContext actionContext, IModelValidatorProvider validatorProvider, ValidatorCache validatorCache, IModelMetadataProvider metadataProvider, ValidationStateDictionary validationState);
         public virtual void Validate(ActionContext actionContext, ValidationStateDictionary validationState, string prefix, object model);
         public virtual void Validate(ActionContext actionContext, ValidationStateDictionary validationState, string prefix, object model, ModelMetadata metadata);
     }
     public class ParameterBinder {
-        public ParameterBinder(IModelMetadataProvider modelMetadataProvider, IModelBinderFactory modelBinderFactory, IObjectModelValidator validator);

         public ParameterBinder(IModelMetadataProvider modelMetadataProvider, IModelBinderFactory modelBinderFactory, IObjectModelValidator validator, IOptions<MvcOptions> mvcOptions, ILoggerFactory loggerFactory);
         protected ILogger Logger { get; }
         public virtual Task<ModelBindingResult> BindModelAsync(ActionContext actionContext, IModelBinder modelBinder, IValueProvider valueProvider, ParameterDescriptor parameter, ModelMetadata metadata, object value);
-        public Task<ModelBindingResult> BindModelAsync(ActionContext actionContext, IValueProvider valueProvider, ParameterDescriptor parameter);

-        public virtual Task<ModelBindingResult> BindModelAsync(ActionContext actionContext, IValueProvider valueProvider, ParameterDescriptor parameter, object value);

     }
+    public class PrefixContainer {
+        public PrefixContainer(ICollection<string> values);
+        public bool ContainsPrefix(string prefix);
+        public IDictionary<string, string> GetKeysFromPrefix(string prefix);
+    }
     public class QueryStringValueProvider : BindingSourceValueProvider, IEnumerableValueProvider, IValueProvider {
         public QueryStringValueProvider(BindingSource bindingSource, IQueryCollection values, CultureInfo culture);
         public CultureInfo Culture { get; }
         protected PrefixContainer PrefixContainer { get; }
         public override bool ContainsPrefix(string prefix);
         public virtual IDictionary<string, string> GetKeysFromPrefix(string prefix);
         public override ValueProviderResult GetValue(string key);
     }
     public class QueryStringValueProviderFactory : IValueProviderFactory {
         public QueryStringValueProviderFactory();
         public Task CreateValueProviderAsync(ValueProviderFactoryContext context);
     }
     public class RouteValueProvider : BindingSourceValueProvider {
         public RouteValueProvider(BindingSource bindingSource, RouteValueDictionary values);
         public RouteValueProvider(BindingSource bindingSource, RouteValueDictionary values, CultureInfo culture);
         protected CultureInfo Culture { get; }
         protected PrefixContainer PrefixContainer { get; }
         public override bool ContainsPrefix(string key);
         public override ValueProviderResult GetValue(string key);
     }
     public class RouteValueProviderFactory : IValueProviderFactory {
         public RouteValueProviderFactory();
         public Task CreateValueProviderAsync(ValueProviderFactoryContext context);
     }
     public class SuppressChildValidationMetadataProvider : IMetadataDetailsProvider, IValidationMetadataProvider {
         public SuppressChildValidationMetadataProvider(string fullTypeName);
         public SuppressChildValidationMetadataProvider(Type type);
         public string FullTypeName { get; }
         public Type Type { get; }
         public void CreateValidationMetadata(ValidationMetadataProviderContext context);
     }
     public class TooManyModelErrorsException : Exception {
         public TooManyModelErrorsException(string message);
     }
     public class UnsupportedContentTypeException : Exception {
         public UnsupportedContentTypeException(string message);
     }
     public class UnsupportedContentTypeFilter : IActionFilter, IFilterMetadata, IOrderedFilter {
         public UnsupportedContentTypeFilter();
         public int Order { get; set; }
         public void OnActionExecuted(ActionExecutedContext context);
         public void OnActionExecuting(ActionExecutingContext context);
     }
     public class ValueProviderFactoryContext {
         public ValueProviderFactoryContext(ActionContext context);
         public ActionContext ActionContext { get; }
         public IList<IValueProvider> ValueProviders { get; }
     }
     public static class ValueProviderFactoryExtensions {
         public static void RemoveType(this IList<IValueProviderFactory> list, Type type);
         public static void RemoveType<TValueProviderFactory>(this IList<IValueProviderFactory> list) where TValueProviderFactory : IValueProviderFactory;
     }
     public readonly struct ValueProviderResult : IEnumerable, IEnumerable<string>, IEquatable<ValueProviderResult> {
         public static ValueProviderResult None;
         public ValueProviderResult(StringValues values);
         public ValueProviderResult(StringValues values, CultureInfo culture);
         public CultureInfo Culture { get; }
         public string FirstValue { get; }
         public int Length { get; }
         public StringValues Values { get; }
         public bool Equals(ValueProviderResult other);
         public override bool Equals(object obj);
         public IEnumerator<string> GetEnumerator();
         public override int GetHashCode();
         public static bool operator ==(ValueProviderResult x, ValueProviderResult y);
         public static explicit operator string (ValueProviderResult result);
         public static explicit operator string[] (ValueProviderResult result);
         public static bool operator !=(ValueProviderResult x, ValueProviderResult y);
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
         public override string ToString();
     }
 }
```

