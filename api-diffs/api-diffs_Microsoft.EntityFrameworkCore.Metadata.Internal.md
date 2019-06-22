# Microsoft.EntityFrameworkCore.Metadata.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Metadata.Internal {
 {
-    public static class AnnotatableExtensions {
 {
-        public static string AnnotationsToDebugString(this IAnnotatable annotatable, string indent = "");

-    }
-    public abstract class ClrAccessorFactory<TAccessor> where TAccessor : class {
 {
-        protected ClrAccessorFactory();

-        public virtual TAccessor Create(IPropertyBase property);

-        public virtual TAccessor Create(PropertyInfo propertyInfo);

-        protected abstract TAccessor CreateGeneric<TEntity, TValue, TNonNullableEnumValue>(PropertyInfo propertyInfo, IPropertyBase propertyBase) where TEntity : class;

-    }
-    public class ClrCollectionAccessorFactory {
 {
-        public ClrCollectionAccessorFactory();

-        public virtual IClrCollectionAccessor Create(INavigation navigation);

-    }
-    public class ClrICollectionAccessor<TEntity, TCollection, TElement> : IClrCollectionAccessor where TEntity : class where TCollection : class, IEnumerable<TElement> {
 {
-        public ClrICollectionAccessor(string propertyName, Func<TEntity, TCollection> getCollection, Action<TEntity, TCollection> setCollection, Func<TEntity, Action<TEntity, TCollection>, TCollection> createAndSetCollection, Func<TCollection> createCollection);

-        public virtual Type CollectionType { get; }

-        public virtual bool Add(object instance, object value);

-        public virtual void AddRange(object instance, IEnumerable<object> values);

-        public virtual bool Contains(object instance, object value);

-        public virtual object Create();

-        public virtual object Create(IEnumerable<object> values);

-        public virtual object GetOrCreate(object instance);

-        public virtual void Remove(object instance, object value);

-    }
-    public sealed class ClrPropertyGetter<TEntity, TValue> : IClrPropertyGetter where TEntity : class {
 {
-        public ClrPropertyGetter(Func<TEntity, TValue> getter, Func<TEntity, bool> hasDefaultValue);

-        public object GetClrValue(object instance);

-        public bool HasDefaultValue(object instance);

-    }
-    public class ClrPropertyGetterFactory : ClrAccessorFactory<IClrPropertyGetter> {
 {
-        public ClrPropertyGetterFactory();

-        protected override IClrPropertyGetter CreateGeneric<TEntity, TValue, TNonNullableEnumValue>(PropertyInfo propertyInfo, IPropertyBase propertyBase);

-    }
-    public sealed class ClrPropertySetter<TEntity, TValue> : IClrPropertySetter where TEntity : class {
 {
-        public ClrPropertySetter(Action<TEntity, TValue> setter);

-        public void SetClrValue(object instance, object value);

-    }
-    public class ClrPropertySetterFactory : ClrAccessorFactory<IClrPropertySetter> {
 {
-        public ClrPropertySetterFactory();

-        protected override IClrPropertySetter CreateGeneric<TEntity, TValue, TNonNullableEnumValue>(PropertyInfo propertyInfo, IPropertyBase propertyBase);

-    }
-    public class CollectionTypeFactory {
 {
-        public CollectionTypeFactory();

-        public virtual Type TryFindTypeToInstantiate(Type entityType, Type collectionType);

-    }
-    public enum ConfigurationSource {
 {
-        Convention = 2,

-        DataAnnotation = 1,

-        Explicit = 0,

-    }
-    public static class ConfigurationSourceExtensions {
 {
-        public static ConfigurationSource Max(this ConfigurationSource left, Nullable<ConfigurationSource> right);

-        public static Nullable<ConfigurationSource> Max(this Nullable<ConfigurationSource> left, Nullable<ConfigurationSource> right);

-        public static bool Overrides(this ConfigurationSource newConfigurationSource, Nullable<ConfigurationSource> oldConfigurationSource);

-        public static bool Overrides(this Nullable<ConfigurationSource> newConfigurationSource, Nullable<ConfigurationSource> oldConfigurationSource);

-        public static bool OverridesStrictly(this ConfigurationSource newConfigurationSource, Nullable<ConfigurationSource> oldConfigurationSource);

-        public static bool OverridesStrictly(this Nullable<ConfigurationSource> newConfigurationSource, Nullable<ConfigurationSource> oldConfigurationSource);

-    }
-    public static class ConstraintNamer {
 {
-        public static string GetDefaultName(IForeignKey foreignKey);

-        public static string GetDefaultName(IIndex index);

-        public static string GetDefaultName(IKey key);

-        public static string GetDefaultName(IProperty property);

-        public static string Truncate(string name, Nullable<int> uniquifier, int maxLength);

-    }
-    public abstract class ConstructorBinding {
 {
-        protected ConstructorBinding(IReadOnlyList<ParameterBinding> parameterBindings);

-        public virtual IReadOnlyList<ParameterBinding> ParameterBindings { get; }

-        public abstract Type RuntimeType { get; }

-        public abstract Expression CreateConstructorExpression(ParameterBindingInfo bindingInfo);

-    }
-    public class ConstructorBindingFactory : IConstructorBindingFactory {
 {
-        public ConstructorBindingFactory(IPropertyParameterBindingFactory propertyFactory, IParameterBindingFactories factories);

-        public virtual bool TryBindConstructor(IMutableEntityType entityType, ConstructorInfo constructor, out ConstructorBinding binding, out IEnumerable<ParameterInfo> failedBindings);

-    }
-    public class ContextParameterBinding : ServiceParameterBinding {
 {
-        public ContextParameterBinding(Type contextType, IPropertyBase consumedProperty = null);

-        public override Expression BindToParameter(Expression materializationExpression, Expression entityTypeExpression, Expression entityExpression);

-    }
-    public class ContextParameterBindingFactory : IParameterBindingFactory {
 {
-        public ContextParameterBindingFactory();

-        public virtual ParameterBinding Bind(IMutableEntityType entityType, Type parameterType, string parameterName);

-        public virtual bool CanBind(Type parameterType, string parameterName);

-    }
-    public class ConventionalAnnotatable : Annotatable {
 {
-        public ConventionalAnnotatable();

-        public virtual new ConventionalAnnotation AddAnnotation(string name, object value);

-        public virtual ConventionalAnnotation AddAnnotation(string name, object value, ConfigurationSource configurationSource);

-        protected override Annotation CreateAnnotation(string name, object value);

-        public virtual new ConventionalAnnotation FindAnnotation(string name);

-        public virtual new IEnumerable<ConventionalAnnotation> GetAnnotations();

-        public virtual new ConventionalAnnotation GetOrAddAnnotation(string name, object value);

-        public virtual new ConventionalAnnotation RemoveAnnotation(string name);

-        public virtual ConventionalAnnotation SetAnnotation(string name, object value, ConfigurationSource configurationSource);

-    }
-    public class ConventionalAnnotation : Annotation {
 {
-        public ConventionalAnnotation(string name, object value, ConfigurationSource configurationSource);

-        public virtual ConfigurationSource GetConfigurationSource();

-        public virtual ConfigurationSource UpdateConfigurationSource(ConfigurationSource configurationSource);

-    }
-    public static class CoreAnnotationNames {
 {
-        public const string ConstructorBinding = "ConstructorBinding";

-        public const string KeyValueComparer = "KeyValueComparer";

-        public const string MaxLengthAnnotation = "MaxLength";

-        public const string NavigationAccessModeAnnotation = "NavigationAccessMode";

-        public const string OwnedTypesAnnotation = "OwnedTypes";

-        public const string ProductVersionAnnotation = "ProductVersion";

-        public const string PropertyAccessModeAnnotation = "PropertyAccessMode";

-        public const string ProviderClrType = "ProviderClrType";

-        public const string StructuralValueComparer = "StructuralValueComparer";

-        public const string TypeMapping = "Relational:TypeMapping";

-        public const string UnicodeAnnotation = "Unicode";

-        public const string ValueComparer = "ValueComparer";

-        public const string ValueConverter = "ValueConverter";

-        public const string ValueGeneratorFactoryAnnotation = "ValueGeneratorFactory";

-    }
-    public class DbFunction : IDbFunction, IMethodCallTranslator, IMutableDbFunction {
 {
-        public virtual string DefaultSchema { get; set; }

-        public virtual string FunctionName { get; set; }

-        public virtual MethodInfo MethodInfo { get; }

-        public virtual string Schema { get; set; }

-        public virtual Func<IReadOnlyCollection<Expression>, Expression> Translation { get; set; }

-        public static DbFunction FindDbFunction(IModel model, string annotationPrefix, MethodInfo methodInfo);

-        public static IEnumerable<IDbFunction> GetDbFunctions(IModel model, string annotationPrefix);

-        public virtual Nullable<ConfigurationSource> GetNameConfigurationSource();

-        public static DbFunction GetOrAddDbFunction(IMutableModel model, MethodInfo methodInfo, string annotationPrefix);

-        public virtual Nullable<ConfigurationSource> GetSchemaConfigurationSource();

-        Expression Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.IMethodCallTranslator.Translate(MethodCallExpression methodCallExpression);

-        public virtual void SetFunctionName(string functionName, ConfigurationSource configurationSource);

-        public virtual void SetSchema(string schema, ConfigurationSource configurationSource);

-    }
-    public class DebugView<TMetadata> {
 {
-        public DebugView(TMetadata metadata, Func<TMetadata, string> toDebugString);

-        public virtual string View { get; }

-    }
-    public class DefaultServiceParameterBinding : ServiceParameterBinding {
 {
-        public DefaultServiceParameterBinding(Type parameterType, Type serviceType, IPropertyBase consumedProperty = null);

-        public override Expression BindToParameter(Expression materializationExpression, Expression entityTypeExpression, Expression entityExpression);

-    }
-    public class DirectConstructorBinding : ConstructorBinding {
 {
-        public DirectConstructorBinding(ConstructorInfo constructor, IReadOnlyList<ParameterBinding> parameterBindings);

-        public virtual ConstructorInfo Constructor { get; }

-        public override Type RuntimeType { get; }

-        public override Expression CreateConstructorExpression(ParameterBindingInfo bindingInfo);

-    }
-    public class EntityMaterializerSource : IEntityMaterializerSource {
 {
-        public static readonly MethodInfo TryReadValueMethod;

-        public EntityMaterializerSource();

-        public virtual Expression CreateMaterializeExpression(IEntityType entityType, Expression materializationExpression, int[] indexMap = null);

-        public virtual Expression CreateReadValueCallExpression(Expression valueBuffer, int index);

-        public virtual Expression CreateReadValueExpression(Expression valueBuffer, Type type, int index, IPropertyBase property);

-        public virtual Func<MaterializationContext, object> GetMaterializer(IEntityType entityType);

-    }
-    public class EntityType : TypeBase, IAnnotatable, IEntityType, IMutableAnnotatable, IMutableEntityType, IMutableTypeBase, ITypeBase {
 {
-        public EntityType(string name, Model model, ConfigurationSource configurationSource);

-        public EntityType(string name, Model model, string definingNavigationName, EntityType definingEntityType, ConfigurationSource configurationSource);

-        public EntityType(Type clrType, Model model, ConfigurationSource configurationSource);

-        public EntityType(Type clrType, Model model, string definingNavigationName, EntityType definingEntityType, ConfigurationSource configurationSource);

-        public virtual EntityType BaseType { get; }

-        public virtual InternalEntityTypeBuilder Builder { get; set; }

-        public virtual ChangeTrackingStrategy ChangeTrackingStrategy { get; set; }

-        public virtual PropertyCounts Counts { get; }

-        public virtual DebugView<EntityType> DebugView { get; }

-        public virtual EntityType DefiningEntityType { get; }

-        public virtual string DefiningNavigationName { get; }

-        public virtual LambdaExpression DefiningQuery { get; set; }

-        public virtual Func<ISnapshot> EmptyShadowValuesFactory { get; }

-        public virtual bool IsQueryType { get; set; }

-        IEntityType Microsoft.EntityFrameworkCore.Metadata.IEntityType.BaseType { get; }

-        IEntityType Microsoft.EntityFrameworkCore.Metadata.IEntityType.DefiningEntityType { get; }

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.BaseType { get; set; }

-        IMutableModel Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.Model { get; }

-        LambdaExpression Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.QueryFilter { get; set; }

-        IMutableModel Microsoft.EntityFrameworkCore.Metadata.IMutableTypeBase.Model { get; }

-        IModel Microsoft.EntityFrameworkCore.Metadata.ITypeBase.Model { get; }

-        public virtual Func<InternalEntityEntry, ISnapshot> OriginalValuesFactory { get; }

-        public virtual LambdaExpression QueryFilter { get; set; }

-        public virtual Func<InternalEntityEntry, ISnapshot> RelationshipSnapshotFactory { get; }

-        public virtual Func<ValueBuffer, ISnapshot> ShadowValuesFactory { get; }

-        public virtual void AddData(IEnumerable<object> data);

-        public virtual ForeignKey AddForeignKey(Property property, Key principalKey, EntityType principalEntityType, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual ForeignKey AddForeignKey(IReadOnlyList<Property> properties, Key principalKey, EntityType principalEntityType, Nullable<ConfigurationSource> configurationSource = 0);

-        public virtual Index AddIndex(Property property, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual Index AddIndex(IReadOnlyList<Property> properties, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual Key AddKey(Property property, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual Key AddKey(IReadOnlyList<Property> properties, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual Navigation AddNavigation(MemberInfo navigationProperty, ForeignKey foreignKey, bool pointsToPrincipal);

-        public virtual Navigation AddNavigation(string name, ForeignKey foreignKey, bool pointsToPrincipal);

-        public virtual Property AddProperty(MemberInfo memberInfo, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual Property AddProperty(string name, Type propertyType = null, ConfigurationSource configurationSource = ConfigurationSource.Explicit, Nullable<ConfigurationSource> typeConfigurationSource = 0);

-        public virtual ServiceProperty AddServiceProperty(MemberInfo memberInfo, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public override void ClearCaches();

-        public virtual ForeignKey FindDeclaredForeignKey(IReadOnlyList<IProperty> properties, IKey principalKey, IEntityType principalEntityType);

-        public virtual IEnumerable<ForeignKey> FindDeclaredForeignKeys(IReadOnlyList<IProperty> properties);

-        public virtual Index FindDeclaredIndex(IReadOnlyList<IProperty> properties);

-        public virtual Key FindDeclaredKey(IReadOnlyList<IProperty> properties);

-        public virtual Navigation FindDeclaredNavigation(string name);

-        public virtual Key FindDeclaredPrimaryKey();

-        public virtual Property FindDeclaredProperty(string name);

-        public virtual ServiceProperty FindDeclaredServiceProperty(string name);

-        public virtual IEnumerable<ForeignKey> FindDerivedForeignKeys(IReadOnlyList<IProperty> properties);

-        public virtual IEnumerable<ForeignKey> FindDerivedForeignKeys(IReadOnlyList<IProperty> properties, IKey principalKey, IEntityType principalEntityType);

-        public virtual IEnumerable<Index> FindDerivedIndexes(IReadOnlyList<IProperty> properties);

-        public virtual IEnumerable<Navigation> FindDerivedNavigations(string navigationName);

-        public virtual IEnumerable<Property> FindDerivedProperties(string propertyName);

-        public virtual IEnumerable<Property> FindDerivedPropertiesInclusive(string propertyName);

-        public virtual IEnumerable<ServiceProperty> FindDerivedServiceProperties(string propertyName);

-        public virtual IEnumerable<ServiceProperty> FindDerivedServicePropertiesInclusive(string propertyName);

-        public virtual ForeignKey FindForeignKey(IProperty property, IKey principalKey, IEntityType principalEntityType);

-        public virtual ForeignKey FindForeignKey(IReadOnlyList<IProperty> properties, IKey principalKey, IEntityType principalEntityType);

-        public virtual IEnumerable<ForeignKey> FindForeignKeys(IProperty property);

-        public virtual IEnumerable<ForeignKey> FindForeignKeys(IReadOnlyList<IProperty> properties);

-        public virtual IEnumerable<ForeignKey> FindForeignKeysInHierarchy(IReadOnlyList<IProperty> properties);

-        public virtual IEnumerable<ForeignKey> FindForeignKeysInHierarchy(IReadOnlyList<IProperty> properties, IKey principalKey, IEntityType principalEntityType);

-        public override Nullable<ConfigurationSource> FindIgnoredMemberConfigurationSource(string name);

-        public virtual Index FindIndex(IProperty property);

-        public virtual Index FindIndex(IReadOnlyList<IProperty> properties);

-        public virtual IEnumerable<Index> FindIndexesInHierarchy(IReadOnlyList<IProperty> properties);

-        public virtual Key FindKey(IProperty property);

-        public virtual Key FindKey(IReadOnlyList<IProperty> properties);

-        public virtual IEnumerable<PropertyBase> FindMembersInHierarchy(string name);

-        public virtual Navigation FindNavigation(MemberInfo memberInfo);

-        public virtual Navigation FindNavigation(string name);

-        public virtual IEnumerable<Navigation> FindNavigationsInHierarchy(string navigationName);

-        public virtual Key FindPrimaryKey();

-        public virtual Key FindPrimaryKey(IReadOnlyList<Property> properties);

-        public virtual IEnumerable<Property> FindPropertiesInHierarchy(string propertyName);

-        public virtual Property FindProperty(PropertyInfo propertyInfo);

-        public virtual Property FindProperty(string name);

-        public virtual IEnumerable<ServiceProperty> FindServicePropertiesInHierarchy(string propertyName);

-        public virtual Property FindServiceProperty(MemberInfo memberInfo);

-        public virtual ServiceProperty FindServiceProperty(string name);

-        public virtual Nullable<ConfigurationSource> GetBaseTypeConfigurationSource();

-        public virtual IEnumerable<IDictionary<string, object>> GetData(bool providerValues = false);

-        public virtual IEnumerable<ForeignKey> GetDeclaredForeignKeys();

-        public virtual IEnumerable<Index> GetDeclaredIndexes();

-        public virtual IEnumerable<Key> GetDeclaredKeys();

-        public virtual IEnumerable<Navigation> GetDeclaredNavigations();

-        public virtual IEnumerable<Property> GetDeclaredProperties();

-        public virtual IEnumerable<ForeignKey> GetDeclaredReferencingForeignKeys();

-        public virtual IEnumerable<ServiceProperty> GetDeclaredServiceProperties();

-        public virtual IEnumerable<ForeignKey> GetDerivedForeignKeys();

-        public virtual IEnumerable<ForeignKey> GetDerivedForeignKeysInclusive();

-        public virtual IEnumerable<Index> GetDerivedIndexes();

-        public virtual IEnumerable<Index> GetDerivedIndexesInclusive();

-        public virtual IEnumerable<Navigation> GetDerivedNavigations();

-        public virtual IEnumerable<Navigation> GetDerivedNavigationsInclusive();

-        public virtual IEnumerable<ForeignKey> GetDerivedReferencingForeignKeys();

-        public virtual IEnumerable<ForeignKey> GetDerivedReferencingForeignKeysInclusive();

-        public virtual IEnumerable<EntityType> GetDerivedTypes();

-        public virtual IEnumerable<EntityType> GetDerivedTypesInclusive();

-        public virtual ISet<EntityType> GetDirectlyDerivedTypes();

-        public virtual IEnumerable<ForeignKey> GetForeignKeys();

-        public virtual IEnumerable<ForeignKey> GetForeignKeysInHierarchy();

-        public virtual IEnumerable<Index> GetIndexes();

-        public virtual IEnumerable<Key> GetKeys();

-        public virtual IEnumerable<Navigation> GetNavigations();

-        public virtual ForeignKey GetOrAddForeignKey(Property property, Key principalKey, EntityType principalEntityType);

-        public virtual ForeignKey GetOrAddForeignKey(IReadOnlyList<Property> properties, Key principalKey, EntityType principalEntityType);

-        public virtual Index GetOrAddIndex(Property property);

-        public virtual Index GetOrAddIndex(IReadOnlyList<Property> properties);

-        public virtual Key GetOrAddKey(Property property);

-        public virtual Key GetOrAddKey(IReadOnlyList<Property> properties);

-        public virtual Property GetOrAddProperty(PropertyInfo propertyInfo);

-        public virtual Property GetOrAddProperty(string name, Type propertyType);

-        public virtual ServiceProperty GetOrAddServiceProperty(MemberInfo memberInfo);

-        public virtual Key GetOrSetPrimaryKey(Property property);

-        public virtual Key GetOrSetPrimaryKey(IReadOnlyList<Property> properties);

-        public virtual Nullable<ConfigurationSource> GetPrimaryKeyConfigurationSource();

-        public virtual IEnumerable<Property> GetProperties();

-        public virtual IEnumerable<ForeignKey> GetReferencingForeignKeys();

-        public virtual IEnumerable<ServiceProperty> GetServiceProperties();

-        public virtual void HasBaseType(EntityType entityType, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        IForeignKey Microsoft.EntityFrameworkCore.Metadata.IEntityType.FindForeignKey(IReadOnlyList<IProperty> properties, IKey principalKey, IEntityType principalEntityType);

-        IIndex Microsoft.EntityFrameworkCore.Metadata.IEntityType.FindIndex(IReadOnlyList<IProperty> properties);

-        IKey Microsoft.EntityFrameworkCore.Metadata.IEntityType.FindKey(IReadOnlyList<IProperty> properties);

-        IKey Microsoft.EntityFrameworkCore.Metadata.IEntityType.FindPrimaryKey();

-        IProperty Microsoft.EntityFrameworkCore.Metadata.IEntityType.FindProperty(string name);

-        IServiceProperty Microsoft.EntityFrameworkCore.Metadata.IEntityType.FindServiceProperty(string name);

-        IEnumerable<IForeignKey> Microsoft.EntityFrameworkCore.Metadata.IEntityType.GetForeignKeys();

-        IEnumerable<IIndex> Microsoft.EntityFrameworkCore.Metadata.IEntityType.GetIndexes();

-        IEnumerable<IKey> Microsoft.EntityFrameworkCore.Metadata.IEntityType.GetKeys();

-        IEnumerable<IProperty> Microsoft.EntityFrameworkCore.Metadata.IEntityType.GetProperties();

-        IEnumerable<IServiceProperty> Microsoft.EntityFrameworkCore.Metadata.IEntityType.GetServiceProperties();

-        IMutableForeignKey Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.AddForeignKey(IReadOnlyList<IMutableProperty> properties, IMutableKey principalKey, IMutableEntityType principalEntityType);

-        IMutableIndex Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.AddIndex(IReadOnlyList<IMutableProperty> properties);

-        IMutableKey Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.AddKey(IReadOnlyList<IMutableProperty> properties);

-        IMutableProperty Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.AddProperty(string name, Type propertyType);

-        IMutableServiceProperty Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.AddServiceProperty(MemberInfo memberInfo);

-        IMutableForeignKey Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.FindForeignKey(IReadOnlyList<IProperty> properties, IKey principalKey, IEntityType principalEntityType);

-        IMutableIndex Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.FindIndex(IReadOnlyList<IProperty> properties);

-        IMutableKey Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.FindKey(IReadOnlyList<IProperty> properties);

-        IMutableKey Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.FindPrimaryKey();

-        IMutableProperty Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.FindProperty(string name);

-        IMutableServiceProperty Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.FindServiceProperty(string name);

-        IEnumerable<IMutableForeignKey> Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.GetForeignKeys();

-        IEnumerable<IMutableIndex> Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.GetIndexes();

-        IEnumerable<IMutableKey> Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.GetKeys();

-        IEnumerable<IMutableProperty> Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.GetProperties();

-        IEnumerable<IMutableServiceProperty> Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.GetServiceProperties();

-        IMutableForeignKey Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.RemoveForeignKey(IReadOnlyList<IProperty> properties, IKey principalKey, IEntityType principalEntityType);

-        IMutableIndex Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.RemoveIndex(IReadOnlyList<IProperty> properties);

-        IMutableKey Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.RemoveKey(IReadOnlyList<IProperty> properties);

-        IMutableProperty Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.RemoveProperty(string name);

-        IMutableServiceProperty Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.RemoveServiceProperty(string name);

-        IMutableKey Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType.SetPrimaryKey(IReadOnlyList<IMutableProperty> properties);

-        protected override Annotation OnAnnotationSet(string name, Annotation annotation, Annotation oldAnnotation);

-        public override void OnTypeMemberIgnored(string name);

-        public virtual void OnTypeRemoved();

-        public override void PropertyMetadataChanged();

-        public virtual ForeignKey RemoveForeignKey(IReadOnlyList<IProperty> properties, IKey principalKey, IEntityType principalEntityType);

-        public virtual Index RemoveIndex(IReadOnlyList<IProperty> properties);

-        public virtual Key RemoveKey(IReadOnlyList<IProperty> properties);

-        public virtual Navigation RemoveNavigation(string name);

-        public virtual Property RemoveProperty(string name);

-        public virtual ServiceProperty RemoveServiceProperty(string name);

-        public virtual EntityType RootType();

-        public virtual Key SetPrimaryKey(Property property);

-        public virtual Key SetPrimaryKey(IReadOnlyList<Property> properties, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public override string ToString();

-        public class Snapshot {
 {
-            public Snapshot(EntityType entityType, PropertiesSnapshot properties, List<InternalIndexBuilder> indexes, List<ValueTuple<InternalKeyBuilder, Nullable<ConfigurationSource>>> keys, List<RelationshipSnapshot> relationships);

-            public virtual void Attach(InternalEntityTypeBuilder entityTypeBuilder);

-        }
-    }
-    public static class EntityTypeExtensions {
 {
-        public static EntityType AsEntityType(this IEntityType entityType, string methodName = "");

-        public static PropertyCounts CalculateCounts(this IEntityType entityType);

-        public static string CheckChangeTrackingStrategy(this IEntityType entityType, ChangeTrackingStrategy value);

-        public static IProperty CheckPropertyBelongsToType(this IEntityType entityType, IProperty property);

-        public static string DisplayName(this IEntityType type);

-        public static IForeignKey FindDeclaredOwnership(this IEntityType entityType);

-        public static ForeignKey FindDeclaredOwnership(this EntityType entityType);

-        public static IKey FindDeclaredPrimaryKey(this IEntityType entityType);

-        public static INavigation FindDefiningNavigation(this IEntityType entityType);

-        public static Navigation FindDefiningNavigation(this EntityType entityType);

-        public static IEnumerable<INavigation> FindDerivedNavigations(this IEntityType entityType, string navigationName);

-        public static IEnumerable<IProperty> FindDerivedProperties(this IEntityType entityType, string propertyName);

-        public static IEntityType FindInDefinitionPath(this IEntityType entityType, string targetTypeName);

-        public static IEntityType FindInDefinitionPath(this IEntityType entityType, Type targetType);

-        public static EntityType FindInDefinitionPath(this EntityType entityType, string targetTypeName);

-        public static EntityType FindInDefinitionPath(this EntityType entityType, Type targetType);

-        public static IEntityType FindInOwnershipPath(this IEntityType entityType, Type targetType);

-        public static IForeignKey FindOwnership(this IEntityType entityType);

-        public static ForeignKey FindOwnership(this EntityType entityType);

-        public static IEnumerable<IEntityType> GetAllBaseTypes(this IEntityType entityType);

-        public static IEnumerable<IEntityType> GetAllBaseTypesInclusive(this IEntityType entityType);

-        public static IEnumerable<IEntityType> GetConcreteTypesInHierarchy(this IEntityType entityType);

-        public static PropertyCounts GetCounts(this IEntityType entityType);

-        public static IEnumerable<IDictionary<string, object>> GetData(this IEntityType entityType, bool providerValues = false);

-        public static IEnumerable<IForeignKey> GetDeclaredForeignKeys(this IEntityType entityType);

-        public static IEnumerable<IIndex> GetDeclaredIndexes(this IEntityType entityType);

-        public static IEnumerable<IKey> GetDeclaredKeys(this IEntityType entityType);

-        public static IEnumerable<INavigation> GetDeclaredNavigations(this IEntityType entityType);

-        public static IEnumerable<IProperty> GetDeclaredProperties(this IEntityType entityType);

-        public static IEnumerable<IForeignKey> GetDeclaredReferencingForeignKeys(this IEntityType entityType);

-        public static IEnumerable<IServiceProperty> GetDeclaredServiceProperties(this IEntityType entityType);

-        public static IEnumerable<Navigation> GetDerivedNavigations(this IEntityType entityType);

-        public static IEnumerable<Navigation> GetDerivedNavigationsInclusive(this IEntityType entityType);

-        public static IEnumerable<IEntityType> GetDerivedTypesInclusive(this IEntityType entityType);

-        public static IEnumerable<IEntityType> GetDirectlyDerivedTypes(this IEntityType entityType);

-        public static Func<ISnapshot> GetEmptyShadowValuesFactory(this IEntityType entityType);

-        public static IEnumerable<IPropertyBase> GetNotificationProperties(this IEntityType entityType, string propertyName);

-        public static Func<InternalEntityEntry, ISnapshot> GetOriginalValuesFactory(this IEntityType entityType);

-        public static IEnumerable<IPropertyBase> GetPropertiesAndNavigations(this IEntityType entityType);

-        public static IProperty GetProperty(this IEntityType entityType, string name);

-        public static Func<InternalEntityEntry, ISnapshot> GetRelationshipSnapshotFactory(this IEntityType entityType);

-        public static Func<ValueBuffer, ISnapshot> GetShadowValuesFactory(this IEntityType entityType);

-        public static bool IsInDefinitionPath(this IEntityType entityType, string targetTypeName);

-        public static bool IsInDefinitionPath(this IEntityType entityType, Type targetType);

-        public static bool IsInOwnershipPath(this EntityType entityType, EntityType targetType);

-        public static bool IsInOwnershipPath(this EntityType entityType, Type targetType);

-        public static bool IsSameHierarchy(this IEntityType firstEntityType, IEntityType secondEntityType);

-        public static EntityType LeastDerivedType(this EntityType entityType, EntityType otherEntityType);

-        public static int NavigationCount(this IEntityType entityType);

-        public static int OriginalValueCount(this IEntityType entityType);

-        public static int PropertyCount(this IEntityType entityType);

-        public static int RelationshipPropertyCount(this IEntityType entityType);

-        public static int ShadowPropertyCount(this IEntityType entityType);

-        public static string ShortName(this IEntityType type);

-        public static int StoreGeneratedCount(this IEntityType entityType);

-        public static string ToDebugString(this IEntityType entityType, bool singleLine = true, string indent = "");

-        public static bool UseEagerSnapshots(this IEntityType entityType);

-    }
-    public class EntityTypeParameterBinding : ServiceParameterBinding {
 {
-        public EntityTypeParameterBinding(IPropertyBase consumedProperty = null);

-        public override Expression BindToParameter(Expression materializationExpression, Expression entityTypeExpression, Expression entityExpression);

-    }
-    public class EntityTypeParameterBindingFactory : IParameterBindingFactory {
 {
-        public EntityTypeParameterBindingFactory();

-        public virtual ParameterBinding Bind(IMutableEntityType entityType, Type parameterType, string parameterName);

-        public virtual bool CanBind(Type parameterType, string parameterName);

-    }
-    public class EntityTypePathComparer : IComparer<IEntityType> {
 {
-        public static readonly EntityTypePathComparer Instance;

-        public virtual int Compare(IEntityType x, IEntityType y);

-        public virtual int GetHashCode(IEntityType entityType);

-    }
-    public class FactoryMethodConstructorBinding : ConstructorBinding {
 {
-        public FactoryMethodConstructorBinding(object factoryInstance, MethodInfo factoryMethod, IReadOnlyList<ParameterBinding> parameterBindings, Type runtimeType);

-        public FactoryMethodConstructorBinding(MethodInfo factoryMethod, IReadOnlyList<ParameterBinding> parameterBindings, Type runtimeType);

-        public override Type RuntimeType { get; }

-        public override Expression CreateConstructorExpression(ParameterBindingInfo bindingInfo);

-    }
-    public class ForeignKey : ConventionalAnnotatable, IAnnotatable, IForeignKey, IMutableAnnotatable, IMutableForeignKey {
 {
-        public ForeignKey(IReadOnlyList<Property> dependentProperties, Key principalKey, EntityType dependentEntityType, EntityType principalEntityType, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Builder { get; set; }

-        public virtual DebugView<ForeignKey> DebugView { get; }

-        public virtual EntityType DeclaringEntityType { get; }

-        public virtual DeleteBehavior DeleteBehavior { get; set; }

-        public virtual object DependentKeyValueFactory { get; set; }

-        public virtual Func<IDependentsMap> DependentsMapFactory { get; set; }

-        public virtual Navigation DependentToPrincipal { get; private set; }

-        public virtual bool IsOwnership { get; set; }

-        public virtual bool IsRequired { get; set; }

-        public virtual bool IsUnique { get; set; }

-        IEntityType Microsoft.EntityFrameworkCore.Metadata.IForeignKey.DeclaringEntityType { get; }

-        INavigation Microsoft.EntityFrameworkCore.Metadata.IForeignKey.DependentToPrincipal { get; }

-        IEntityType Microsoft.EntityFrameworkCore.Metadata.IForeignKey.PrincipalEntityType { get; }

-        IKey Microsoft.EntityFrameworkCore.Metadata.IForeignKey.PrincipalKey { get; }

-        INavigation Microsoft.EntityFrameworkCore.Metadata.IForeignKey.PrincipalToDependent { get; }

-        IReadOnlyList<IProperty> Microsoft.EntityFrameworkCore.Metadata.IForeignKey.Properties { get; }

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey.DeclaringEntityType { get; }

-        IMutableNavigation Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey.DependentToPrincipal { get; }

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey.PrincipalEntityType { get; }

-        IMutableKey Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey.PrincipalKey { get; }

-        IMutableNavigation Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey.PrincipalToDependent { get; }

-        IReadOnlyList<IMutableProperty> Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey.Properties { get; }

-        public virtual EntityType PrincipalEntityType { get; }

-        public virtual Key PrincipalKey { get; }

-        public virtual Navigation PrincipalToDependent { get; private set; }

-        public virtual IReadOnlyList<Property> Properties { get; }

-        public static bool AreCompatible(EntityType principalEntityType, EntityType dependentEntityType, MemberInfo navigationToPrincipal, MemberInfo navigationToDependent, IReadOnlyList<Property> dependentProperties, IReadOnlyList<Property> principalProperties, Nullable<bool> unique, Nullable<bool> required, bool shouldThrow);

-        public static bool AreCompatible(IReadOnlyList<Property> principalProperties, IReadOnlyList<Property> dependentProperties, EntityType principalEntityType, EntityType dependentEntityType, bool shouldThrow);

-        public static bool CanPropertiesBeRequired(IReadOnlyList<Property> properties, Nullable<bool> required, EntityType entityType, bool shouldThrow);

-        public virtual IEnumerable<Navigation> FindNavigationsFrom(EntityType entityType);

-        public virtual IEnumerable<Navigation> FindNavigationsFromInHierarchy(EntityType entityType);

-        public virtual IEnumerable<Navigation> FindNavigationsTo(EntityType entityType);

-        public virtual IEnumerable<Navigation> FindNavigationsToInHierarchy(EntityType entityType);

-        public virtual ConfigurationSource GetConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetDeleteBehaviorConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetDependentToPrincipalConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetForeignKeyPropertiesConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetIsOwnershipConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetIsRequiredConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetIsUniqueConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetPrincipalEndConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetPrincipalKeyConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetPrincipalToDependentConfigurationSource();

-        public virtual Navigation HasDependentToPrincipal(MemberInfo property, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual Navigation HasDependentToPrincipal(string name, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual Navigation HasPrincipalToDependent(MemberInfo property, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual Navigation HasPrincipalToDependent(string name, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        IMutableNavigation Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey.HasDependentToPrincipal(PropertyInfo property);

-        IMutableNavigation Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey.HasDependentToPrincipal(string name);

-        IMutableNavigation Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey.HasPrincipalToDependent(PropertyInfo property);

-        IMutableNavigation Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey.HasPrincipalToDependent(string name);

-        public virtual EntityType ResolveEntityTypeInHierarchy(EntityType entityType);

-        public virtual EntityType ResolveOtherEntityType(EntityType entityType);

-        public virtual EntityType ResolveOtherEntityTypeInHierarchy(EntityType entityType);

-        public virtual void SetDeleteBehavior(DeleteBehavior deleteBehavior, ConfigurationSource configurationSource);

-        public virtual ForeignKey SetIsOwnership(bool ownership, ConfigurationSource configurationSource);

-        public virtual void SetIsRequired(bool required, ConfigurationSource configurationSource);

-        public virtual void SetIsRequiredConfigurationSource(Nullable<ConfigurationSource> configurationSource);

-        public virtual ForeignKey SetIsUnique(bool unique, ConfigurationSource configurationSource);

-        public virtual void SetPrincipalEndConfigurationSource(Nullable<ConfigurationSource> configurationSource);

-        public override string ToString();

-        public virtual void UpdateConfigurationSource(ConfigurationSource configurationSource);

-        public virtual void UpdateDeleteBehaviorConfigurationSource(ConfigurationSource configurationSource);

-        public virtual void UpdateDependentToPrincipalConfigurationSource(Nullable<ConfigurationSource> configurationSource);

-        public virtual void UpdateForeignKeyPropertiesConfigurationSource(ConfigurationSource configurationSource);

-        public virtual void UpdateIsOwnershipConfigurationSource(ConfigurationSource configurationSource);

-        public virtual void UpdateIsRequiredConfigurationSource(ConfigurationSource configurationSource);

-        public virtual void UpdateIsUniqueConfigurationSource(ConfigurationSource configurationSource);

-        public virtual void UpdatePrincipalEndConfigurationSource(ConfigurationSource configurationSource);

-        public virtual void UpdatePrincipalKeyConfigurationSource(ConfigurationSource configurationSource);

-        public virtual void UpdatePrincipalToDependentConfigurationSource(Nullable<ConfigurationSource> configurationSource);

-    }
-    public class ForeignKeyComparer : IComparer<IForeignKey>, IEqualityComparer<IForeignKey> {
 {
-        public static readonly ForeignKeyComparer Instance;

-        public virtual int Compare(IForeignKey x, IForeignKey y);

-        public virtual bool Equals(IForeignKey x, IForeignKey y);

-        public virtual int GetHashCode(IForeignKey obj);

-    }
-    public static class ForeignKeyExtensions {
 {
-        public static bool AreCompatible(this IForeignKey foreignKey, IForeignKey duplicateForeignKey, bool shouldThrow);

-        public static ForeignKey AsForeignKey(this IForeignKey foreignKey, string methodName = "");

-        public static IDependentsMap CreateDependentsMapFactory(this IForeignKey foreignKey);

-        public static IEnumerable<INavigation> FindNavigationsFrom(this IForeignKey foreignKey, IEntityType entityType);

-        public static IEnumerable<INavigation> FindNavigationsFromInHierarchy(this IForeignKey foreignKey, IEntityType entityType);

-        public static IEnumerable<INavigation> FindNavigationsTo(this IForeignKey foreignKey, IEntityType entityType);

-        public static IEnumerable<INavigation> FindNavigationsToInHierarchy(this IForeignKey foreignKey, IEntityType entityType);

-        public static IDependentKeyValueFactory<TKey> GetDependentKeyValueFactory<TKey>(this IForeignKey foreignKey);

-        public static IEnumerable<INavigation> GetNavigations(this IForeignKey foreignKey);

-        public static bool IsIntraHierarchical(this IForeignKey foreignKey);

-        public static bool IsSelfPrimaryKeyReferencing(this IForeignKey foreignKey);

-        public static bool IsSelfReferencing(this IForeignKey foreignKey);

-        public static IEntityType ResolveEntityTypeInHierarchy(this IForeignKey foreignKey, IEntityType entityType);

-        public static IEntityType ResolveOtherEntityType(this IForeignKey foreignKey, IEntityType entityType);

-        public static IEntityType ResolveOtherEntityTypeInHierarchy(this IForeignKey foreignKey, IEntityType entityType);

-        public static string ToDebugString(this IForeignKey foreignKey, bool singleLine = true, string indent = "");

-    }
-    public interface IClrCollectionAccessor {
 {
-        Type CollectionType { get; }

-        bool Add(object instance, object value);

-        void AddRange(object instance, IEnumerable<object> values);

-        bool Contains(object instance, object value);

-        object Create();

-        object Create(IEnumerable<object> values);

-        object GetOrCreate(object instance);

-        void Remove(object instance, object value);

-    }
-    public interface IClrPropertyGetter {
 {
-        object GetClrValue(object instance);

-        bool HasDefaultValue(object instance);

-    }
-    public interface IClrPropertySetter {
 {
-        void SetClrValue(object instance, object value);

-    }
-    public interface IConstructorBindingFactory {
 {
-        bool TryBindConstructor(IMutableEntityType entityType, ConstructorInfo constructor, out ConstructorBinding binding, out IEnumerable<ParameterInfo> failedBindings);

-    }
-    public interface IEntityMaterializerSource {
 {
-        Expression CreateMaterializeExpression(IEntityType entityType, Expression materializationExpression, int[] indexMap = null);

-        Expression CreateReadValueCallExpression(Expression valueBuffer, int index);

-        Expression CreateReadValueExpression(Expression valueBuffer, Type type, int index, IPropertyBase property);

-        Func<MaterializationContext, object> GetMaterializer(IEntityType entityType);

-    }
-    public interface IMemberClassifier {
 {
-        Type FindCandidateNavigationPropertyType(PropertyInfo propertyInfo);

-    }
-    public class Index : ConventionalAnnotatable, IAnnotatable, IIndex, IMutableAnnotatable, IMutableIndex {
 {
-        public Index(IReadOnlyList<Property> properties, EntityType declaringEntityType, ConfigurationSource configurationSource);

-        public virtual InternalIndexBuilder Builder { get; set; }

-        public virtual DebugView<Index> DebugView { get; }

-        public virtual EntityType DeclaringEntityType { get; }

-        public virtual bool IsUnique { get; set; }

-        IEntityType Microsoft.EntityFrameworkCore.Metadata.IIndex.DeclaringEntityType { get; }

-        IReadOnlyList<IProperty> Microsoft.EntityFrameworkCore.Metadata.IIndex.Properties { get; }

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableIndex.DeclaringEntityType { get; }

-        IReadOnlyList<IMutableProperty> Microsoft.EntityFrameworkCore.Metadata.IMutableIndex.Properties { get; }

-        public virtual IReadOnlyList<Property> Properties { get; }

-        public virtual ConfigurationSource GetConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetIsUniqueConfigurationSource();

-        public virtual INullableValueFactory<TKey> GetNullableValueFactory<TKey>();

-        protected override Annotation OnAnnotationSet(string name, Annotation annotation, Annotation oldAnnotation);

-        public virtual void SetIsUnique(bool unique, ConfigurationSource configurationSource);

-        public override string ToString();

-        public virtual void UpdateConfigurationSource(ConfigurationSource configurationSource);

-    }
-    public static class IndexExtensions {
 {
-        public static bool AreCompatible(this IIndex index, IIndex duplicateIndex, bool shouldThrow);

-        public static Index AsIndex(this IIndex index, string methodName = "");

-        public static INullableValueFactory<TKey> GetNullableValueFactory<TKey>(this IIndex index);

-        public static string ToDebugString(this IIndex index, bool singleLine = true, string indent = "");

-    }
-    public class InternalDbFunctionBuilder {
 {
-        public InternalDbFunctionBuilder(DbFunction function);

-        public virtual IMutableDbFunction Metadata { get; }

-        public virtual InternalDbFunctionBuilder HasName(string name, ConfigurationSource configurationSource);

-        public virtual InternalDbFunctionBuilder HasSchema(string schema, ConfigurationSource configurationSource);

-        public virtual InternalDbFunctionBuilder HasTranslation(Func<IReadOnlyCollection<Expression>, Expression> translation);

-    }
-    public class InternalEntityTypeBuilder : InternalMetadataItemBuilder<EntityType> {
 {
-        public InternalEntityTypeBuilder(EntityType metadata, InternalModelBuilder modelBuilder);

-        public virtual bool CanAddNavigation(string navigationName, ConfigurationSource configurationSource);

-        public virtual bool CanAddOrReplaceNavigation(string navigationName, ConfigurationSource configurationSource);

-        public virtual bool CanRemoveForeignKey(ForeignKey foreignKey, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder CreateForeignKey(InternalEntityTypeBuilder principalEntityTypeBuilder, IReadOnlyList<Property> dependentProperties, Key principalKey, string navigationToPrincipalName, Nullable<bool> isRequired, ConfigurationSource configurationSource);

-        public static EntityType.Snapshot DetachAllMembers(EntityType entityType);

-        public static List<InternalIndexBuilder> DetachIndexes(IEnumerable<Index> indexesToDetach);

-        public static List<ValueTuple<InternalKeyBuilder, Nullable<ConfigurationSource>>> DetachKeys(IEnumerable<Key> keysToDetach);

-        public static RelationshipSnapshot DetachRelationship(ForeignKey foreignKey);

-        public virtual IReadOnlyList<Property> GetActualProperties(IReadOnlyList<Property> properties, Nullable<ConfigurationSource> configurationSource);

-        public virtual IReadOnlyList<Property> GetOrCreateProperties(IEnumerable<MemberInfo> clrProperties, ConfigurationSource configurationSource);

-        public virtual IReadOnlyList<Property> GetOrCreateProperties(IReadOnlyList<string> propertyNames, Nullable<ConfigurationSource> configurationSource, IReadOnlyList<Property> referencedProperties = null, bool required = false, bool useDefaultType = false);

-        public virtual InternalEntityTypeBuilder HasBaseType(EntityType baseEntityType, ConfigurationSource configurationSource);

-        public virtual InternalEntityTypeBuilder HasBaseType(string baseEntityTypeName, ConfigurationSource configurationSource);

-        public virtual InternalEntityTypeBuilder HasBaseType(Type baseEntityType, ConfigurationSource configurationSource);

-        public virtual void HasDefiningQuery(LambdaExpression query);

-        public virtual InternalRelationshipBuilder HasForeignKey(InternalEntityTypeBuilder principalEntityTypeBuilder, IReadOnlyList<Property> dependentProperties, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder HasForeignKey(InternalEntityTypeBuilder principalEntityTypeBuilder, IReadOnlyList<Property> dependentProperties, Key principalKey, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder HasForeignKey(string principalEntityTypeName, IReadOnlyList<string> propertyNames, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder HasForeignKey(string principalEntityTypeName, IReadOnlyList<string> propertyNames, Key principalKey, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder HasForeignKey(Type principalClrType, IReadOnlyList<PropertyInfo> clrProperties, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder HasForeignKey(Type principalClrType, IReadOnlyList<PropertyInfo> clrProperties, Key principalKey, ConfigurationSource configurationSource);

-        public virtual InternalIndexBuilder HasIndex(IReadOnlyList<Property> properties, ConfigurationSource configurationSource);

-        public virtual InternalIndexBuilder HasIndex(IReadOnlyList<PropertyInfo> clrProperties, ConfigurationSource configurationSource);

-        public virtual InternalIndexBuilder HasIndex(IReadOnlyList<string> propertyNames, ConfigurationSource configurationSource);

-        public virtual InternalKeyBuilder HasKey(IReadOnlyList<Property> properties, Nullable<ConfigurationSource> configurationSource);

-        public virtual InternalKeyBuilder HasKey(IReadOnlyList<PropertyInfo> clrProperties, ConfigurationSource configurationSource);

-        public virtual InternalKeyBuilder HasKey(IReadOnlyList<string> propertyNames, ConfigurationSource configurationSource);

-        public virtual void HasQueryFilter(LambdaExpression filter);

-        public virtual bool Ignore(string name, ConfigurationSource configurationSource);

-        public virtual bool IsIgnored(string name, Nullable<ConfigurationSource> configurationSource);

-        public virtual InternalRelationshipBuilder Navigation(InternalEntityTypeBuilder targetEntityTypeBuilder, PropertyInfo navigationProperty, ConfigurationSource configurationSource, bool setTargetAsPrincipal = false);

-        public virtual InternalRelationshipBuilder Navigation(InternalEntityTypeBuilder targetEntityTypeBuilder, string navigationName, ConfigurationSource configurationSource, bool setTargetAsPrincipal = false);

-        public virtual InternalRelationshipBuilder Owns(string targetEntityTypeName, PropertyInfo navigationProperty, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Owns(string targetEntityTypeName, string navigationName, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Owns(Type targetEntityType, MemberInfo navigationProperty, MemberInfo inverseProperty, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Owns(Type targetEntityType, PropertyInfo navigationProperty, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Owns(Type targetEntityType, string navigationName, ConfigurationSource configurationSource);

-        public virtual InternalKeyBuilder PrimaryKey(IReadOnlyList<Property> properties, ConfigurationSource configurationSource);

-        public virtual InternalKeyBuilder PrimaryKey(IReadOnlyList<PropertyInfo> clrProperties, ConfigurationSource configurationSource);

-        public virtual InternalKeyBuilder PrimaryKey(IReadOnlyList<string> propertyNames, ConfigurationSource configurationSource);

-        public virtual InternalPropertyBuilder Property(MemberInfo clrProperty, ConfigurationSource configurationSource);

-        public virtual InternalPropertyBuilder Property(string propertyName, ConfigurationSource configurationSource);

-        public virtual InternalPropertyBuilder Property(string propertyName, Type propertyType, ConfigurationSource configurationSource);

-        public virtual InternalPropertyBuilder Property(string propertyName, Type propertyType, ConfigurationSource configurationSource, Nullable<ConfigurationSource> typeConfigurationSource);

-        public virtual InternalRelationshipBuilder Relationship(EntityType principalEntityType, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Relationship(EntityType principalEntityType, Key principalKey, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Relationship(InternalEntityTypeBuilder principalEntityTypeBuilder, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Relationship(InternalEntityTypeBuilder principalEntityTypeBuilder, Key principalKey, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Relationship(InternalEntityTypeBuilder targetEntityTypeBuilder, MemberInfo navigationToTarget, MemberInfo inverseNavigation, ConfigurationSource configurationSource, bool setTargetAsPrincipal = false);

-        public virtual InternalRelationshipBuilder Relationship(InternalEntityTypeBuilder targetEntityTypeBuilder, string navigationToTargetName, string inverseNavigationName, ConfigurationSource configurationSource, bool setTargetAsPrincipal = false);

-        public virtual Nullable<ConfigurationSource> RemoveForeignKey(ForeignKey foreignKey, ConfigurationSource configurationSource, bool canOverrideSameSource = true);

-        public virtual Nullable<ConfigurationSource> RemoveIndex(Index index, ConfigurationSource configurationSource);

-        public virtual Nullable<ConfigurationSource> RemoveKey(Key key, ConfigurationSource configurationSource);

-        public virtual bool RemoveNonOwnershipRelationships(ForeignKey ownership, ConfigurationSource configurationSource);

-        public virtual void RemoveShadowPropertiesIfUnused(IEnumerable<Property> properties);

-        public virtual InternalServicePropertyBuilder ServiceProperty(MemberInfo memberInfo, ConfigurationSource configurationSource);

-        public virtual bool ShouldReuniquifyTemporaryProperties(IReadOnlyList<Property> currentProperties, IReadOnlyList<Property> principalProperties, bool isRequired, string baseName);

-        public virtual bool UsePropertyAccessMode(PropertyAccessMode propertyAccessMode, ConfigurationSource configurationSource);

-    }
-    public class InternalIndexBuilder : InternalMetadataItemBuilder<Index> {
 {
-        public InternalIndexBuilder(Index index, InternalModelBuilder modelBuilder);

-        public virtual InternalIndexBuilder Attach(InternalEntityTypeBuilder entityTypeBuilder);

-        public virtual bool IsUnique(bool isUnique, ConfigurationSource configurationSource);

-    }
-    public class InternalKeyBuilder : InternalMetadataItemBuilder<Key> {
 {
-        public InternalKeyBuilder(Key key, InternalModelBuilder modelBuilder);

-        public virtual InternalKeyBuilder Attach(InternalEntityTypeBuilder entityTypeBuilder, Nullable<ConfigurationSource> primaryKeyConfigurationSource);

-    }
-    public abstract class InternalMetadataBuilder {
 {
-        protected InternalMetadataBuilder(ConventionalAnnotatable metadata);

-        public virtual ConventionalAnnotatable Metadata { get; }

-        public abstract InternalModelBuilder ModelBuilder { get; }

-        public virtual bool CanSetAnnotation(string name, object value, ConfigurationSource configurationSource);

-        public virtual bool HasAnnotation(string name, object value, ConfigurationSource configurationSource);

-        public virtual void MergeAnnotationsFrom(ConventionalAnnotatable annotatable);

-        public virtual void MergeAnnotationsFrom(ConventionalAnnotatable annotatable, ConfigurationSource minimalConfigurationSource);

-        public virtual bool RemoveAnnotation(string name, ConfigurationSource configurationSource);

-    }
-    public abstract class InternalMetadataBuilder<TMetadata> : InternalMetadataBuilder where TMetadata : ConventionalAnnotatable {
 {
-        protected InternalMetadataBuilder(TMetadata metadata);

-        public virtual new TMetadata Metadata { get; }

-    }
-    public abstract class InternalMetadataItemBuilder<TMetadata> : InternalMetadataBuilder<TMetadata> where TMetadata : ConventionalAnnotatable {
 {
-        protected InternalMetadataItemBuilder(TMetadata metadata, InternalModelBuilder modelBuilder);

-        public override InternalModelBuilder ModelBuilder { get; }

-    }
-    public class InternalModelBuilder : InternalMetadataBuilder<Model> {
 {
-        public InternalModelBuilder(Model metadata);

-        public override InternalModelBuilder ModelBuilder { get; }

-        public virtual InternalEntityTypeBuilder Entity(string name, ConfigurationSource configurationSource, bool allowOwned = false, bool throwOnQuery = false);

-        public virtual InternalEntityTypeBuilder Entity(string name, string definingNavigationName, EntityType definingEntityType, ConfigurationSource configurationSource);

-        public virtual InternalEntityTypeBuilder Entity(Type type, ConfigurationSource configurationSource, bool allowOwned = false, bool throwOnQuery = false);

-        public virtual InternalEntityTypeBuilder Entity(Type type, string definingNavigationName, EntityType definingEntityType, ConfigurationSource configurationSource);

-        public virtual IReadOnlyList<InternalEntityTypeBuilder> FindLeastDerivedEntityTypes(Type type, Func<InternalEntityTypeBuilder, bool> condition = null);

-        public virtual bool Ignore(string name, ConfigurationSource configurationSource);

-        public virtual bool Ignore(Type type, ConfigurationSource configurationSource);

-        public virtual bool IsIgnored(string name, ConfigurationSource configurationSource);

-        public virtual bool IsIgnored(Type type, ConfigurationSource configurationSource);

-        public virtual bool Owned(string name, ConfigurationSource configurationSource);

-        public virtual bool Owned(Type type, ConfigurationSource configurationSource);

-        public virtual InternalEntityTypeBuilder Query(string name, ConfigurationSource configurationSource);

-        public virtual InternalEntityTypeBuilder Query(Type clrType, ConfigurationSource configurationSource);

-        public virtual bool RemoveEntityType(EntityType entityType, ConfigurationSource configurationSource);

-        public virtual void RemoveEntityTypesUnreachableByNavigations(ConfigurationSource configurationSource);

-        public virtual bool UsePropertyAccessMode(PropertyAccessMode propertyAccessMode, ConfigurationSource configurationSource);

-    }
-    public class InternalNavigationBuilder : InternalMetadataItemBuilder<Navigation> {
 {
-        public InternalNavigationBuilder(Navigation metadata, InternalModelBuilder modelBuilder);

-    }
-    public class InternalPropertyBuilder : InternalMetadataItemBuilder<Property> {
 {
-        public InternalPropertyBuilder(Property property, InternalModelBuilder modelBuilder);

-        public virtual bool AfterSave(Nullable<PropertySaveBehavior> behavior, ConfigurationSource configurationSource);

-        public virtual InternalPropertyBuilder Attach(InternalEntityTypeBuilder entityTypeBuilder);

-        public virtual bool BeforeSave(Nullable<PropertySaveBehavior> behavior, ConfigurationSource configurationSource);

-        public virtual bool CanSetRequired(bool isRequired, Nullable<ConfigurationSource> configurationSource);

-        public virtual bool HasConversion(ValueConverter valueConverter, ConfigurationSource configurationSource);

-        public virtual bool HasConversion(Type providerClrType, ConfigurationSource configurationSource);

-        public virtual bool HasField(string fieldName, ConfigurationSource configurationSource);

-        public virtual bool HasFieldInfo(FieldInfo fieldInfo, ConfigurationSource configurationSource);

-        public virtual bool HasMaxLength(int maxLength, ConfigurationSource configurationSource);

-        public virtual bool HasValueGenerator(Func<IProperty, IEntityType, ValueGenerator> factory, ConfigurationSource configurationSource);

-        public virtual bool HasValueGenerator(Type valueGeneratorType, ConfigurationSource configurationSource);

-        public virtual bool IsConcurrencyToken(bool concurrencyToken, ConfigurationSource configurationSource);

-        public virtual bool IsRequired(bool isRequired, ConfigurationSource configurationSource);

-        public virtual bool IsUnicode(bool unicode, ConfigurationSource configurationSource);

-        public virtual bool UsePropertyAccessMode(PropertyAccessMode propertyAccessMode, ConfigurationSource configurationSource);

-        public virtual bool ValueGenerated(Nullable<ValueGenerated> valueGenerated, ConfigurationSource configurationSource);

-    }
-    public class InternalRelationshipBuilder : InternalMetadataItemBuilder<ForeignKey> {
 {
-        public InternalRelationshipBuilder(ForeignKey foreignKey, InternalModelBuilder modelBuilder);

-        public static bool AreCompatible(EntityType principalEntityType, EntityType dependentEntityType, MemberInfo navigationToPrincipal, MemberInfo navigationToDependent, IReadOnlyList<Property> dependentProperties, IReadOnlyList<Property> principalProperties, Nullable<bool> isUnique, Nullable<bool> isRequired, Nullable<ConfigurationSource> configurationSource);

-        public virtual InternalRelationshipBuilder Attach(InternalEntityTypeBuilder entityTypeBuilder);

-        public virtual bool CanInvert(IReadOnlyList<Property> newForeignKeyProperties, ConfigurationSource configurationSource);

-        public virtual bool CanSetDeleteBehavior(DeleteBehavior deleteBehavior, Nullable<ConfigurationSource> configurationSource);

-        public virtual bool CanSetNavigation(PropertyInfo navigationProperty, bool pointsToPrincipal, Nullable<ConfigurationSource> configurationSource, bool overrideSameSource = true);

-        public virtual bool CanSetNavigation(string navigationName, bool pointsToPrincipal, Nullable<ConfigurationSource> configurationSource, bool overrideSameSource = true);

-        public virtual bool CanSetRequired(bool isRequired, Nullable<ConfigurationSource> configurationSource);

-        public virtual InternalRelationshipBuilder DeleteBehavior(DeleteBehavior deleteBehavior, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder DependentEntityType(EntityType dependentEntityType, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder DependentEntityType(InternalEntityTypeBuilder dependentEntityTypeBuilder, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder DependentEntityType(string dependentTypeName, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder DependentEntityType(Type dependentType, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder DependentToPrincipal(MemberInfo property, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder DependentToPrincipal(string name, ConfigurationSource configurationSource);

-        public static InternalRelationshipBuilder FindCurrentRelationshipBuilder(EntityType principalEntityType, EntityType dependentEntityType, Nullable<PropertyIdentity> navigationToPrincipal, Nullable<PropertyIdentity> navigationToDependent, IReadOnlyList<Property> dependentProperties, IReadOnlyList<Property> principalProperties);

-        public virtual InternalRelationshipBuilder HasForeignKey(IReadOnlyList<Property> properties, EntityType dependentEntityType, Nullable<ConfigurationSource> configurationSource);

-        public virtual InternalRelationshipBuilder HasForeignKey(IReadOnlyList<Property> properties, Nullable<ConfigurationSource> configurationSource);

-        public virtual InternalRelationshipBuilder HasForeignKey(IReadOnlyList<MemberInfo> properties, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder HasForeignKey(IReadOnlyList<MemberInfo> properties, EntityType dependentEntityType, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder HasForeignKey(IReadOnlyList<string> propertyNames, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder HasForeignKey(IReadOnlyList<string> propertyNames, EntityType dependentEntityType, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder HasPrincipalKey(IReadOnlyList<Property> properties, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder HasPrincipalKey(IReadOnlyList<PropertyInfo> properties, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder HasPrincipalKey(IReadOnlyList<string> propertyNames, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder IsOwnership(bool ownership, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder IsRequired(bool isRequired, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder IsUnique(bool unique, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder IsWeakTypeDefinition(ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Navigations(Nullable<PropertyIdentity> navigationToPrincipal, Nullable<PropertyIdentity> navigationToDependent, Nullable<ConfigurationSource> configurationSource);

-        public virtual InternalRelationshipBuilder Navigations(MemberInfo navigationToPrincipalProperty, MemberInfo navigationToDependentProperty, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Navigations(MemberInfo navigationToPrincipalProperty, MemberInfo navigationToDependentProperty, EntityType principalEntityType, EntityType dependentEntityType, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Navigations(string navigationToPrincipalName, string navigationToDependentName, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder Navigations(string navigationToPrincipalName, string navigationToDependentName, EntityType principalEntityType, EntityType dependentEntityType, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder PrincipalEntityType(EntityType principalEntityType, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder PrincipalEntityType(InternalEntityTypeBuilder principalEntityTypeBuilder, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder PrincipalEntityType(string principalTypeName, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder PrincipalEntityType(Type principalType, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder PrincipalToDependent(MemberInfo property, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder PrincipalToDependent(string name, ConfigurationSource configurationSource);

-        public virtual InternalRelationshipBuilder RelatedEntityTypes(EntityType principalEntityType, EntityType dependentEntityType, Nullable<ConfigurationSource> configurationSource);

-    }
-    public class InternalServicePropertyBuilder : InternalMetadataItemBuilder<ServiceProperty> {
 {
-        public InternalServicePropertyBuilder(ServiceProperty property, InternalModelBuilder modelBuilder);

-        public virtual bool HasField(string fieldName, ConfigurationSource configurationSource);

-        public virtual bool HasFieldInfo(FieldInfo fieldInfo, ConfigurationSource configurationSource);

-        public virtual bool SetParameterBinding(ServiceParameterBinding parameterBinding, ConfigurationSource configurationSource);

-        public virtual bool UsePropertyAccessMode(PropertyAccessMode propertyAccessMode, ConfigurationSource configurationSource);

-    }
-    public interface IParameterBindingFactories {
 {
-        IParameterBindingFactory FindFactory(Type type, string name);

-    }
-    public interface IParameterBindingFactory {
 {
-        ParameterBinding Bind(IMutableEntityType entityType, Type parameterType, string parameterName);

-        bool CanBind(Type parameterType, string parameterName);

-    }
-    public interface IPropertyParameterBindingFactory {
 {
-        ParameterBinding TryBindParameter(IMutableEntityType entityType, Type parameterType, string parameterName);

-    }
-    public class Key : ConventionalAnnotatable, IAnnotatable, IKey, IMutableAnnotatable, IMutableKey {
 {
-        public Key(IReadOnlyList<Property> properties, ConfigurationSource configurationSource);

-        public virtual InternalKeyBuilder Builder { get; set; }

-        public virtual DebugView<Key> DebugView { get; }

-        public virtual EntityType DeclaringEntityType { get; }

-        public virtual Func<bool, IIdentityMap> IdentityMapFactory { get; }

-        IEntityType Microsoft.EntityFrameworkCore.Metadata.IKey.DeclaringEntityType { get; }

-        IReadOnlyList<IProperty> Microsoft.EntityFrameworkCore.Metadata.IKey.Properties { get; }

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableKey.DeclaringEntityType { get; }

-        IReadOnlyList<IMutableProperty> Microsoft.EntityFrameworkCore.Metadata.IMutableKey.Properties { get; }

-        public virtual IReadOnlyList<Property> Properties { get; }

-        public virtual ISet<ForeignKey> ReferencingForeignKeys { get; set; }

-        public virtual Func<IWeakReferenceIdentityMap> WeakReferenceIdentityMapFactory { get; }

-        public virtual ConfigurationSource GetConfigurationSource();

-        public virtual IPrincipalKeyValueFactory<TKey> GetPrincipalKeyValueFactory<TKey>();

-        public virtual IEnumerable<ForeignKey> GetReferencingForeignKeys();

-        public override string ToString();

-        public virtual void UpdateConfigurationSource(ConfigurationSource configurationSource);

-    }
-    public static class KeyExtensions {
 {
-        public static Key AsKey(this IKey key, string methodName = "");

-        public static Func<bool, IIdentityMap> GetIdentityMapFactory(this IKey key);

-        public static IPrincipalKeyValueFactory<TKey> GetPrincipalKeyValueFactory<TKey>(this IKey key);

-        public static Func<IWeakReferenceIdentityMap> GetWeakReferenceIdentityMapFactory(this IKey key);

-        public static int IndexOf(this IKey key, IProperty property);

-        public static bool IsPrimaryKey(this IKey key);

-        public static string ToDebugString(this IKey key, bool singleLine = true, string indent = "");

-    }
-    public class LazyLoaderParameterBindingFactory : ServiceParameterBindingFactory {
 {
-        public LazyLoaderParameterBindingFactory();

-        public override ParameterBinding Bind(IMutableEntityType entityType, Type parameterType, string parameterName);

-        public override bool CanBind(Type parameterType, string parameterName);

-    }
-    public class MemberClassifier : IMemberClassifier {
 {
-        public MemberClassifier(ITypeMappingSource typeMappingSource, IParameterBindingFactories parameterBindingFactories);

-        public virtual Type FindCandidateNavigationPropertyType(PropertyInfo propertyInfo);

-    }
-    public static class MetadataExtensions {
 {
-        public static TConcrete AsConcreteMetadataType<TInterface, TConcrete>(TInterface @interface, string methodName) where TConcrete : class;

-    }
-    public class Model : ConventionalAnnotatable, IAnnotatable, IModel, IMutableAnnotatable, IMutableModel {
 {
-        public Model();

-        public Model(ConventionSet conventions);

-        public virtual InternalModelBuilder Builder { get; }

-        public virtual ChangeTrackingStrategy ChangeTrackingStrategy { get; set; }

-        public virtual ConventionDispatcher ConventionDispatcher { get; }

-        public virtual DebugView<Model> DebugView { get; }

-        public virtual void AddDetachedEntityType(string name, string definingNavigationName, string definingEntityTypeName);

-        public virtual EntityType AddEntityType(string name, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual EntityType AddEntityType(string name, string definingNavigationName, EntityType definingEntityType, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual EntityType AddEntityType(Type type, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual EntityType AddEntityType(Type type, string definingNavigationName, EntityType definingEntityType, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual EntityType AddQueryType(string name, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual EntityType AddQueryType(Type type, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual bool EntityTypeShouldHaveDefiningNavigation(string name);

-        public virtual bool EntityTypeShouldHaveDefiningNavigation(Type clrType);

-        public virtual EntityType FindActualEntityType(EntityType entityType);

-        public virtual Type FindClrType(string name);

-        public virtual EntityType FindEntityType(string name);

-        public virtual EntityType FindEntityType(string name, string definingNavigationName, EntityType definingEntityType);

-        public virtual EntityType FindEntityType(string name, string definingNavigationName, string definingEntityTypeName);

-        public virtual EntityType FindEntityType(Type type);

-        public virtual EntityType FindEntityType(Type type, string definingNavigationName, EntityType definingEntityType);

-        public virtual Nullable<ConfigurationSource> FindIgnoredTypeConfigurationSource(string name);

-        public virtual Nullable<ConfigurationSource> FindIgnoredTypeConfigurationSource(Type type);

-        public virtual string GetDisplayName(Type type);

-        public virtual IEnumerable<EntityType> GetEntityTypes();

-        public virtual IReadOnlyCollection<EntityType> GetEntityTypes(string name);

-        public virtual IReadOnlyCollection<EntityType> GetEntityTypes(Type type);

-        public virtual EntityType GetOrAddEntityType(string name);

-        public virtual EntityType GetOrAddEntityType(Type type);

-        public virtual bool HasEntityTypeWithDefiningNavigation(string name);

-        public virtual bool HasEntityTypeWithDefiningNavigation(Type clrType);

-        public virtual bool HasOtherEntityTypesWithDefiningNavigation(EntityType entityType);

-        public virtual void Ignore(string name, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public virtual void Ignore(Type type, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        IEntityType Microsoft.EntityFrameworkCore.Metadata.IModel.FindEntityType(string name);

-        IEntityType Microsoft.EntityFrameworkCore.Metadata.IModel.FindEntityType(string name, string definingNavigationName, IEntityType definingEntityType);

-        IEnumerable<IEntityType> Microsoft.EntityFrameworkCore.Metadata.IModel.GetEntityTypes();

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableModel.AddEntityType(string name);

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableModel.AddEntityType(string name, string definingNavigationName, IMutableEntityType definingEntityType);

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableModel.AddEntityType(Type type);

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableModel.AddEntityType(Type type, string definingNavigationName, IMutableEntityType definingEntityType);

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableModel.AddQueryType(Type type);

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableModel.FindEntityType(string name);

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableModel.FindEntityType(string name, string definingNavigationName, IMutableEntityType definingEntityType);

-        IEnumerable<IMutableEntityType> Microsoft.EntityFrameworkCore.Metadata.IMutableModel.GetEntityTypes();

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableModel.RemoveEntityType(string name);

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableModel.RemoveEntityType(string name, string definingNavigationName, IMutableEntityType definingEntityType);

-        protected override Annotation OnAnnotationSet(string name, Annotation annotation, Annotation oldAnnotation);

-        public virtual EntityType RemoveEntityType(EntityType entityType);

-        public virtual EntityType RemoveEntityType(string name);

-        public virtual EntityType RemoveEntityType(string name, string definingNavigationName, EntityType definingEntityType);

-        public virtual EntityType RemoveEntityType(Type type);

-        public virtual EntityType RemoveEntityType(Type type, string definingNavigationName, EntityType definingEntityType);

-        public virtual void Unignore(string name);

-        public virtual void Unignore(Type type);

-        public virtual InternalModelBuilder Validate();

-    }
-    public static class ModelExtensions {
 {
-        public static Model AsModel(this IModel model, string methodName = "");

-        public static string GetProductVersion(this IModel model);

-        public static IEnumerable<IEntityType> GetRootEntityTypes(this IModel model);

-        public static void MarkAsOwnedType(this Model model, string value);

-        public static void MarkAsOwnedType(this Model model, Type clrType);

-        public static void SetProductVersion(this Model model, string value);

-        public static bool ShouldBeOwnedType(this IModel model, string value);

-        public static bool ShouldBeOwnedType(this IModel model, Type clrType);

-        public static string ToDebugString(this IModel model, string indent = "");

-        public static void UnmarkAsOwnedType(this Model model, string value);

-        public static void UnmarkAsOwnedType(this Model model, Type clrType);

-    }
-    public class ModelNavigationsGraphAdapter : Graph<EntityType> {
 {
-        public ModelNavigationsGraphAdapter(Model model);

-        public override IEnumerable<EntityType> Vertices { get; }

-        public override IEnumerable<EntityType> GetIncomingNeighbours(EntityType to);

-        public override IEnumerable<EntityType> GetOutgoingNeighbours(EntityType from);

-    }
-    public static class MutableEntityTypeExtensions {
 {
-        public static void AddData(this IMutableEntityType entityType, IEnumerable<object> data);

-        public static void AddData(this IMutableEntityType entityType, params object[] data);

-        public static IEnumerable<IMutableForeignKey> GetDeclaredForeignKeys(this IMutableEntityType entityType);

-        public static IEnumerable<IMutableProperty> GetDeclaredProperties(this IMutableEntityType entityType);

-        public static IEnumerable<IMutableEntityType> GetDerivedTypesInclusive(this IMutableEntityType entityType);

-    }
-    public static class MutableServicePropertyExtensions {
 {
-        public static ServiceProperty AsServiceProperty(this IMutableServiceProperty serviceProperty, string methodName = "");

-        public static void SetParameterBinding(this IMutableServiceProperty serviceProperty, ServiceParameterBinding parameterBinding);

-    }
-    public class Navigation : PropertyBase, IAnnotatable, IMutableAnnotatable, IMutableNavigation, IMutablePropertyBase, INavigation, IPropertyBase {
 {
-        public Navigation(string name, PropertyInfo propertyInfo, FieldInfo fieldInfo, ForeignKey foreignKey);

-        public virtual InternalNavigationBuilder Builder { get; set; }

-        public override Type ClrType { get; }

-        public virtual IClrCollectionAccessor CollectionAccessor { get; }

-        public virtual DebugView<Navigation> DebugView { get; }

-        public virtual EntityType DeclaringEntityType { get; }

-        public override TypeBase DeclaringType { get; }

-        public virtual ForeignKey ForeignKey { get; }

-        public virtual bool IsEagerLoaded { get; set; }

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableNavigation.DeclaringEntityType { get; }

-        IMutableForeignKey Microsoft.EntityFrameworkCore.Metadata.IMutableNavigation.ForeignKey { get; }

-        IEntityType Microsoft.EntityFrameworkCore.Metadata.INavigation.DeclaringEntityType { get; }

-        IForeignKey Microsoft.EntityFrameworkCore.Metadata.INavigation.ForeignKey { get; }

-        public virtual Navigation FindInverse();

-        public static MemberInfo GetClrMember(string navigationName, EntityType sourceType, EntityType targetType, bool shouldThrow);

-        public virtual EntityType GetTargetType();

-        public static bool IsCompatible(MemberInfo navigationProperty, Type sourceClrType, Type targetClrType, Nullable<bool> shouldBeCollection, bool shouldThrow);

-        public static bool IsCompatible(string navigationName, MemberInfo navigationProperty, EntityType sourceType, EntityType targetType, Nullable<bool> shouldBeCollection, bool shouldThrow);

-        protected override void PropertyMetadataChanged();

-        public override string ToString();

-    }
-    public static class NavigationExtensions {
 {
-        public static Navigation AsNavigation(this INavigation navigation, string methodName = "");

-        public static IClrCollectionAccessor GetCollectionAccessor(this INavigation navigation);

-        public static string ToDebugString(this INavigation navigation, bool singleLine = true, string indent = "");

-    }
-    public class NullableEnumClrPropertySetter<TEntity, TValue, TNonNullableEnumValue> : IClrPropertySetter where TEntity : class {
 {
-        public NullableEnumClrPropertySetter(Action<TEntity, TValue> setter);

-        public virtual void SetClrValue(object instance, object value);

-    }
-    public class ObjectArrayParameterBinding : ParameterBinding {
 {
-        public ObjectArrayParameterBinding(IReadOnlyList<ParameterBinding> bindings);

-        public override Expression BindToParameter(ParameterBindingInfo bindingInfo);

-    }
-    public abstract class ParameterBinding {
 {
-        protected ParameterBinding(Type parameterType, params IPropertyBase[] consumedProperties);

-        public virtual IReadOnlyList<IPropertyBase> ConsumedProperties { get; }

-        public virtual Type ParameterType { get; }

-        public abstract Expression BindToParameter(ParameterBindingInfo bindingInfo);

-    }
-    public class ParameterBindingFactories : IParameterBindingFactories {
 {
-        public ParameterBindingFactories(IEnumerable<IParameterBindingFactory> registeredFactories, IRegisteredServices registeredServices);

-        public virtual IParameterBindingFactory FindFactory(Type type, string name);

-    }
-    public readonly struct ParameterBindingInfo {
 {
-        public ParameterBindingInfo(IEntityType entityType, Expression materializationContextExpression, int[] indexMap);

-        public IEntityType EntityType { get; }

-        public Expression MaterializationContextExpression { get; }

-        public int GetValueBufferIndex(IPropertyBase property);

-    }
-    public class PropertiesSnapshot {
 {
-        public PropertiesSnapshot(List<InternalPropertyBuilder> properties, List<InternalIndexBuilder> indexes, List<ValueTuple<InternalKeyBuilder, Nullable<ConfigurationSource>>> keys, List<RelationshipSnapshot> relationships);

-        public virtual void Add(List<InternalIndexBuilder> indexes);

-        public virtual void Add(List<RelationshipSnapshot> relationships);

-        public virtual void Add(List<ValueTuple<InternalKeyBuilder, Nullable<ConfigurationSource>>> keys);

-        public virtual void Attach(InternalEntityTypeBuilder entityTypeBuilder);

-    }
-    public class Property : PropertyBase, IAnnotatable, IMutableAnnotatable, IMutableProperty, IMutablePropertyBase, IProperty, IPropertyBase {
 {
-        public Property(string name, Type clrType, PropertyInfo propertyInfo, FieldInfo fieldInfo, EntityType declaringEntityType, ConfigurationSource configurationSource, Nullable<ConfigurationSource> typeConfigurationSource);

-        public virtual PropertySaveBehavior AfterSaveBehavior { get; set; }

-        public virtual PropertySaveBehavior BeforeSaveBehavior { get; set; }

-        public virtual InternalPropertyBuilder Builder { get; set; }

-        public override Type ClrType { get; }

-        public virtual DebugView<Property> DebugView { get; }

-        public virtual EntityType DeclaringEntityType { get; }

-        public override TypeBase DeclaringType { get; }

-        public virtual List<IForeignKey> ForeignKeys { get; set; }

-        public virtual List<IIndex> Indexes { get; set; }

-        public virtual bool IsConcurrencyToken { get; set; }

-        public virtual bool IsNullable { get; set; }

-        public virtual bool IsReadOnlyAfterSave { get; set; }

-        public virtual bool IsReadOnlyBeforeSave { get; set; }

-        public virtual bool IsStoreGeneratedAlways { get; set; }

-        public virtual List<IKey> Keys { get; set; }

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableProperty.DeclaringEntityType { get; }

-        IEntityType Microsoft.EntityFrameworkCore.Metadata.IProperty.DeclaringEntityType { get; }

-        public virtual IKey PrimaryKey { get; set; }

-        public virtual ValueGenerated ValueGenerated { get; set; }

-        public static bool AreCompatible(IReadOnlyList<Property> properties, EntityType entityType);

-        public static string Format(IEnumerable<IPropertyBase> properties, bool includeTypes = false);

-        public static string Format(IEnumerable<string> properties);

-        public virtual Nullable<ConfigurationSource> GetAfterSaveBehaviorConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetBeforeSaveBehaviorConfigurationSource();

-        public virtual ConfigurationSource GetConfigurationSource();

-        public virtual IEnumerable<ForeignKey> GetContainingForeignKeys();

-        public virtual IEnumerable<Index> GetContainingIndexes();

-        public virtual IEnumerable<Key> GetContainingKeys();

-        public virtual Nullable<ConfigurationSource> GetIsConcurrencyTokenConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetIsNullableConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetTypeConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetValueGeneratedConfigurationSource();

-        protected override Annotation OnAnnotationSet(string name, Annotation annotation, Annotation oldAnnotation);

-        protected override void OnFieldInfoSet(FieldInfo oldFieldInfo);

-        protected override void PropertyMetadataChanged();

-        public virtual void SetAfterSaveBehavior(Nullable<PropertySaveBehavior> afterSaveBehavior, ConfigurationSource configurationSource);

-        public virtual void SetBeforeSaveBehavior(Nullable<PropertySaveBehavior> beforeSaveBehavior, ConfigurationSource configurationSource);

-        public virtual void SetConfigurationSource(ConfigurationSource configurationSource);

-        public virtual void SetIsConcurrencyToken(bool concurrencyToken, ConfigurationSource configurationSource);

-        public virtual void SetIsNullable(bool nullable, ConfigurationSource configurationSource);

-        public virtual void SetValueGenerated(Nullable<ValueGenerated> valueGenerated, ConfigurationSource configurationSource);

-        public override string ToString();

-        public virtual void UpdateConfigurationSource(ConfigurationSource configurationSource);

-        public virtual void UpdateTypeConfigurationSource(ConfigurationSource configurationSource);

-    }
-    public sealed class PropertyAccessors {
 {
-        public PropertyAccessors(Delegate currentValueGetter, Delegate preStoreGeneratedCurrentValueGetter, Delegate originalValueGetter, Delegate relationshipSnapshotGetter, Func<ValueBuffer, object> valueBufferGetter);

-        public Delegate CurrentValueGetter { get; }

-        public Delegate OriginalValueGetter { get; }

-        public Delegate PreStoreGeneratedCurrentValueGetter { get; }

-        public Delegate RelationshipSnapshotGetter { get; }

-        public Func<ValueBuffer, object> ValueBufferGetter { get; }

-    }
-    public class PropertyAccessorsFactory {
 {
-        public PropertyAccessorsFactory();

-        public virtual PropertyAccessors Create(IPropertyBase propertyBase);

-    }
-    public abstract class PropertyBase : ConventionalAnnotatable, IAnnotatable, IMutableAnnotatable, IMutablePropertyBase, IPropertyBase {
 {
-        protected PropertyBase(string name, PropertyInfo propertyInfo, FieldInfo fieldInfo);

-        public virtual PropertyAccessors Accessors { get; }

-        public abstract Type ClrType { get; }

-        public abstract TypeBase DeclaringType { get; }

-        public virtual FieldInfo FieldInfo { get; set; }

-        public virtual IClrPropertyGetter Getter { get; }

-        public virtual bool IsShadowProperty { get; }

-        IMutableTypeBase Microsoft.EntityFrameworkCore.Metadata.IMutablePropertyBase.DeclaringType { get; }

-        ITypeBase Microsoft.EntityFrameworkCore.Metadata.IPropertyBase.DeclaringType { get; }

-        public virtual string Name { get; }

-        public virtual PropertyIndexes PropertyIndexes { get; set; }

-        public virtual PropertyInfo PropertyInfo { get; }

-        public virtual IClrPropertySetter Setter { get; }

-        public static FieldInfo GetFieldInfo(string fieldName, TypeBase type, string propertyName, bool shouldThrow);

-        public virtual Nullable<ConfigurationSource> GetFieldInfoConfigurationSource();

-        public static bool IsCompatible(FieldInfo fieldInfo, Type propertyType, Type entityClrType, string propertyName, bool shouldThrow);

-        protected virtual void OnFieldInfoSet(FieldInfo oldFieldInfo);

-        protected abstract void PropertyMetadataChanged();

-        public virtual void SetField(string fieldName, ConfigurationSource configurationSource);

-        public virtual void SetFieldInfo(FieldInfo fieldInfo, ConfigurationSource configurationSource);

-    }
-    public static class PropertyBaseExtensions {
 {
-        public static PropertyBase AsPropertyBase(this IPropertyBase propertyBase, string methodName = "");

-        public static IClrPropertyGetter GetGetter(this IPropertyBase propertyBase);

-        public static MemberInfo GetIdentifyingMemberInfo(this IPropertyBase propertyBase);

-        public static int GetIndex(this IPropertyBase property);

-        public static MemberInfo GetMemberInfo(this IPropertyBase propertyBase, bool forConstruction, bool forSet);

-        public static int GetOriginalValueIndex(this IPropertyBase propertyBase);

-        public static PropertyAccessors GetPropertyAccessors(this IPropertyBase propertyBase);

-        public static PropertyIndexes GetPropertyIndexes(this IPropertyBase propertyBase);

-        public static int GetRelationshipIndex(this IPropertyBase propertyBase);

-        public static IClrPropertySetter GetSetter(this IPropertyBase propertyBase);

-        public static int GetShadowIndex(this IPropertyBase property);

-        public static int GetStoreGeneratedIndex(this IPropertyBase propertyBase);

-        public static void SetIndexes(this IPropertyBase propertyBase, PropertyIndexes indexes);

-        public static bool TryGetMemberInfo(this IPropertyBase propertyBase, bool forConstruction, bool forSet, out MemberInfo memberInfo, out string errorMessage);

-    }
-    public class PropertyCounts {
 {
-        public PropertyCounts(int propertyCount, int navigationCount, int originalValueCount, int shadowCount, int relationshipCount, int storeGeneratedCount);

-        public virtual int NavigationCount { get; }

-        public virtual int OriginalValueCount { get; }

-        public virtual int PropertyCount { get; }

-        public virtual int RelationshipCount { get; }

-        public virtual int ShadowCount { get; }

-        public virtual int StoreGeneratedCount { get; }

-    }
-    public static class PropertyExtensions {
 {
-        public static Property AsProperty(this IProperty property, string methodName = "");

-        public static CoreTypeMapping FindMapping(this IProperty property);

-        public static IProperty FindPrincipal(this IProperty property);

-        public static IReadOnlyList<IProperty> FindPrincipals(this IProperty property);

-        public static IForeignKey FindSharedTableLink(this IProperty property);

-        public static IProperty FindSharedTableRootPrimaryKeyProperty(this IProperty property);

-        public static bool ForAdd(this ValueGenerated valueGenerated);

-        public static bool ForUpdate(this ValueGenerated valueGenerated);

-        public static IEnumerable<IEntityType> GetContainingEntityTypes(this IProperty property);

-        public static IProperty GetGenerationProperty(this IProperty property);

-        public static IEnumerable<IForeignKey> GetReferencingForeignKeys(this IProperty property);

-        public static bool IsKeyOrForeignKey(this IProperty property);

-        public static bool MayBeStoreGenerated(this IProperty property);

-        public static bool RequiresOriginalValue(this IProperty property);

-        public static bool RequiresValueGenerator(this IProperty property);

-        public static string ToDebugString(this IProperty property, bool singleLine = true, string indent = "");

-    }
-    public readonly struct PropertyIdentity {
 {
-        public static readonly PropertyIdentity None;

-        public PropertyIdentity(MemberInfo property);

-        public PropertyIdentity(string name);

-        public string Name { get; }

-        public MemberInfo Property { get; }

-        public static PropertyIdentity Create(Navigation navigation);

-        public static PropertyIdentity Create(MemberInfo property);

-        public static PropertyIdentity Create(string name);

-        public bool IsNone();

-    }
-    public class PropertyIndexes {
 {
-        public PropertyIndexes(int index, int originalValueIndex, int shadowIndex, int relationshipIndex, int storeGenerationIndex);

-        public virtual int Index { get; }

-        public virtual int OriginalValueIndex { get; }

-        public virtual int RelationshipIndex { get; }

-        public virtual int ShadowIndex { get; }

-        public virtual int StoreGenerationIndex { get; }

-    }
-    public class PropertyListComparer : IComparer<IReadOnlyList<IProperty>>, IEqualityComparer<IReadOnlyList<IProperty>> {
 {
-        public static readonly PropertyListComparer Instance;

-        public int Compare(IReadOnlyList<IProperty> x, IReadOnlyList<IProperty> y);

-        public bool Equals(IReadOnlyList<IProperty> x, IReadOnlyList<IProperty> y);

-        public int GetHashCode(IReadOnlyList<IProperty> obj);

-    }
-    public class PropertyParameterBinding : ParameterBinding {
 {
-        public PropertyParameterBinding(IProperty consumedProperty);

-        public override Expression BindToParameter(ParameterBindingInfo bindingInfo);

-    }
-    public class PropertyParameterBindingFactory : IPropertyParameterBindingFactory {
 {
-        public PropertyParameterBindingFactory();

-        public virtual ParameterBinding TryBindParameter(IMutableEntityType entityType, Type parameterType, string parameterName);

-    }
-    public class RelationalAnnotationsBuilder : RelationalAnnotations {
 {
-        public RelationalAnnotationsBuilder(InternalMetadataBuilder internalBuilder, ConfigurationSource configurationSource);

-        public virtual ConfigurationSource ConfigurationSource { get; }

-        public virtual InternalMetadataBuilder MetadataBuilder { get; }

-        public override bool CanSetAnnotation(string relationalAnnotationName, object value);

-        public override bool RemoveAnnotation(string annotationName);

-        public override bool SetAnnotation(string relationalAnnotationName, object value);

-    }
-    public class RelationalEntityTypeBuilderAnnotations : RelationalEntityTypeAnnotations {
 {
-        protected readonly string DefaultDiscriminatorName;

-        public RelationalEntityTypeBuilderAnnotations(InternalEntityTypeBuilder internalBuilder, ConfigurationSource configurationSource);

-        protected virtual new RelationalAnnotationsBuilder Annotations { get; }

-        protected virtual InternalEntityTypeBuilder EntityTypeBuilder { get; }

-        protected override RelationalEntityTypeAnnotations GetAnnotations(IEntityType entityType);

-        protected override RelationalModelAnnotations GetAnnotations(IModel model);

-        public virtual DiscriminatorBuilder HasDiscriminator();

-        public virtual DiscriminatorBuilder HasDiscriminator(PropertyInfo propertyInfo);

-        public virtual DiscriminatorBuilder HasDiscriminator(string name, Type discriminatorType);

-        public virtual DiscriminatorBuilder HasDiscriminator(Type discriminatorType);

-        public virtual bool HasDiscriminatorValue(object value);

-        public virtual bool ToSchema(string name);

-        public virtual bool ToTable(string name);

-        public virtual bool ToTable(string name, string schema);

-    }
-    public class RelationalForeignKeyBuilderAnnotations : RelationalForeignKeyAnnotations {
 {
-        public RelationalForeignKeyBuilderAnnotations(InternalRelationshipBuilder internalBuilder, ConfigurationSource configurationSource);

-        public virtual bool CanSetName(string value);

-        public virtual bool HasConstraintName(string value);

-    }
-    public class RelationalIndexBuilderAnnotations : RelationalIndexAnnotations {
 {
-        public RelationalIndexBuilderAnnotations(InternalIndexBuilder internalBuilder, ConfigurationSource configurationSource);

-        public virtual bool CanSetName(string value);

-        public virtual bool HasFilter(string value);

-        public virtual bool HasName(string value);

-    }
-    public static class RelationalInternalMetadataBuilderExtensions {
 {
-        public static RelationalEntityTypeBuilderAnnotations Relational(this InternalEntityTypeBuilder builder, ConfigurationSource configurationSource);

-        public static RelationalIndexBuilderAnnotations Relational(this InternalIndexBuilder builder, ConfigurationSource configurationSource);

-        public static RelationalKeyBuilderAnnotations Relational(this InternalKeyBuilder builder, ConfigurationSource configurationSource);

-        public static RelationalModelBuilderAnnotations Relational(this InternalModelBuilder builder, ConfigurationSource configurationSource);

-        public static RelationalPropertyBuilderAnnotations Relational(this InternalPropertyBuilder builder, ConfigurationSource configurationSource);

-        public static RelationalForeignKeyBuilderAnnotations Relational(this InternalRelationshipBuilder builder, ConfigurationSource configurationSource);

-    }
-    public class RelationalKeyBuilderAnnotations : RelationalKeyAnnotations {
 {
-        public RelationalKeyBuilderAnnotations(InternalKeyBuilder internalBuilder, ConfigurationSource configurationSource);

-        public virtual bool CanSetName(string value);

-        public virtual bool HasName(string value);

-    }
-    public class RelationalModelBuilderAnnotations : RelationalModelAnnotations {
 {
-        public RelationalModelBuilderAnnotations(InternalModelBuilder internalBuilder, ConfigurationSource configurationSource);

-        public virtual bool HasDefaultSchema(string value);

-        public virtual bool HasMaxIdentifierLength(Nullable<int> value);

-    }
-    public static class RelationalModelExtensions {
 {
-        public static int GetMaxIdentifierLength(this IModel model);

-    }
-    public class RelationalPropertyBuilderAnnotations : RelationalPropertyAnnotations {
 {
-        public RelationalPropertyBuilderAnnotations(InternalPropertyBuilder internalBuilder, ConfigurationSource configurationSource);

-        protected virtual new RelationalAnnotationsBuilder Annotations { get; }

-        protected override bool ShouldThrowOnConflict { get; }

-        protected override bool ShouldThrowOnInvalidConfiguration { get; }

-        public virtual bool CanSetColumnName(string value);

-        public virtual bool HasColumnName(string value);

-        public virtual bool HasColumnType(string value);

-        public virtual bool HasComputedColumnSql(string value);

-        public virtual bool HasDefaultValue(object value);

-        public virtual bool HasDefaultValueSql(string value);

-        public virtual bool IsFixedLength(bool fixedLength);

-    }
-    public class RelationshipSnapshot {
 {
-        public RelationshipSnapshot(InternalRelationshipBuilder relationship, EntityType.Snapshot weakEntityTypeSnapshot);

-        public virtual InternalRelationshipBuilder Relationship { get; }

-        public virtual EntityType.Snapshot WeakEntityTypeSnapshot { get; set; }

-        public virtual InternalRelationshipBuilder Attach(InternalEntityTypeBuilder entityTypeBuilder = null);

-    }
-    public static class ScaffoldingAnnotationNames {
 {
-        public const string ColumnOrdinal = "Scaffolding:ColumnOrdinal";

-        public const string DatabaseName = "Scaffolding:DatabaseName";

-        public const string DbSetName = "Scaffolding:DbSetName";

-        public const string EntityTypeErrors = "Scaffolding:EntityTypeErrors";

-        public const string Prefix = "Scaffolding:";

-    }
-    public class ScaffoldingEntityTypeAnnotations : RelationalEntityTypeAnnotations {
 {
-        public ScaffoldingEntityTypeAnnotations(IEntityType entity);

-        public virtual string DbSetName { get; set; }

-    }
-    public static class ScaffoldingMetadataExtensions {
 {
-        public static ScaffoldingEntityTypeAnnotations Scaffolding(this IEntityType entityType);

-        public static ScaffoldingModelAnnotations Scaffolding(this IModel model);

-        public static ScaffoldingPropertyAnnotations Scaffolding(this IProperty property);

-    }
-    public class ScaffoldingModelAnnotations : RelationalModelAnnotations {
 {
-        public ScaffoldingModelAnnotations(IModel model);

-        public virtual string DatabaseName { get; set; }

-        public virtual IDictionary<string, string> EntityTypeErrors { get; set; }

-    }
-    public class ScaffoldingPropertyAnnotations : RelationalPropertyAnnotations {
 {
-        public ScaffoldingPropertyAnnotations(IProperty property);

-        public virtual int ColumnOrdinal { get; set; }

-    }
-    public class Sequence : IMutableSequence, ISequence {
 {
-        public static readonly bool DefaultIsCyclic;

-        public const int DefaultIncrementBy = 1;

-        public const int DefaultStartValue = 1;

-        public static readonly Nullable<long> DefaultMaxValue;

-        public static readonly Nullable<long> DefaultMinValue;

-        public static readonly Type DefaultClrType;

-        public Sequence(IModel model, string annotationName);

-        public Sequence(IMutableModel model, string annotationName, string name, string schema = null);

-        public virtual Type ClrType { get; set; }

-        public virtual int IncrementBy { get; set; }

-        public virtual bool IsCyclic { get; set; }

-        public virtual Nullable<long> MaxValue { get; set; }

-        IModel Microsoft.EntityFrameworkCore.Metadata.ISequence.Model { get; }

-        public virtual Nullable<long> MinValue { get; set; }

-        public virtual Model Model { get; }

-        public virtual string Name { get; }

-        public virtual string Schema { get; }

-        public virtual long StartValue { get; set; }

-        public static IReadOnlyCollection<Type> SupportedTypes { get; }

-        public static IEnumerable<Sequence> GetSequences(IModel model, string annotationPrefix);

-    }
-    public class ServiceMethodParameterBinding : DefaultServiceParameterBinding {
 {
-        public ServiceMethodParameterBinding(Type parameterType, Type serviceType, MethodInfo method, IPropertyBase consumedProperty = null);

-        public virtual MethodInfo Method { get; }

-        public override Expression BindToParameter(Expression materializationExpression, Expression entityTypeExpression, Expression entityExpression);

-    }
-    public abstract class ServiceParameterBinding : ParameterBinding {
 {
-        protected ServiceParameterBinding(Type parameterType, Type serviceType, IPropertyBase consumedProperty = null);

-        public virtual Func<MaterializationContext, IEntityType, object, object> ServiceDelegate { get; }

-        public virtual Type ServiceType { get; }

-        public override Expression BindToParameter(ParameterBindingInfo bindingInfo);

-        public abstract Expression BindToParameter(Expression materializationExpression, Expression entityTypeExpression, Expression entityExpression);

-    }
-    public class ServiceParameterBindingFactory : IParameterBindingFactory {
 {
-        public ServiceParameterBindingFactory(Type serviceType);

-        public virtual ParameterBinding Bind(IMutableEntityType entityType, Type parameterType, string parameterName);

-        public virtual bool CanBind(Type parameterType, string parameterName);

-    }
-    public class ServiceProperty : PropertyBase, IAnnotatable, IMutableAnnotatable, IMutablePropertyBase, IMutableServiceProperty, IPropertyBase, IServiceProperty {
 {
-        public ServiceProperty(string name, PropertyInfo propertyInfo, FieldInfo fieldInfo, EntityType declaringEntityType, ConfigurationSource configurationSource);

-        public virtual InternalServicePropertyBuilder Builder { get; set; }

-        public override Type ClrType { get; }

-        public virtual DebugView<ServiceProperty> DebugView { get; }

-        public virtual EntityType DeclaringEntityType { get; }

-        public override TypeBase DeclaringType { get; }

-        IMutableEntityType Microsoft.EntityFrameworkCore.Metadata.IMutableServiceProperty.DeclaringEntityType { get; }

-        IEntityType Microsoft.EntityFrameworkCore.Metadata.IServiceProperty.DeclaringEntityType { get; }

-        public virtual ServiceParameterBinding ParameterBinding { get; set; }

-        public virtual ConfigurationSource GetConfigurationSource();

-        public virtual Nullable<ConfigurationSource> GetParameterBindingConfigurationSource();

-        protected override void PropertyMetadataChanged();

-        public virtual void SetParameterBinding(ServiceParameterBinding parameterBinding, ConfigurationSource configurationSource);

-        public override string ToString();

-        public virtual void UpdateConfigurationSource(ConfigurationSource configurationSource);

-    }
-    public static class ServicePropertyExtensions {
 {
-        public static ServiceProperty AsServiceProperty(this IServiceProperty serviceProperty, string methodName = "");

-        public static ServiceParameterBinding GetParameterBinding(this IServiceProperty serviceProperty);

-        public static string ToDebugString(this IServiceProperty serviceProperty, bool singleLine = true, string indent = "");

-    }
-    public class TableMapping {
 {
-        public TableMapping(string schema, string name, IReadOnlyList<IEntityType> entityTypes);

-        public virtual IReadOnlyList<IEntityType> EntityTypes { get; }

-        public virtual string Name { get; }

-        public virtual string Schema { get; }

-        public virtual IEnumerable<IForeignKey> GetForeignKeys();

-        public virtual IEnumerable<IIndex> GetIndexes();

-        public virtual IEnumerable<IKey> GetKeys();

-        public virtual IEnumerable<IProperty> GetProperties();

-        public virtual Dictionary<string, IProperty> GetPropertyMap();

-        public virtual IEntityType GetRootType();

-        public static TableMapping GetTableMapping(IModel model, string table, string schema);

-        public static IReadOnlyList<TableMapping> GetTableMappings(IModel model);

-    }
-    public abstract class TypeBase : ConventionalAnnotatable, IAnnotatable, IMutableAnnotatable, IMutableTypeBase, ITypeBase {
 {
-        protected TypeBase(string name, Model model, ConfigurationSource configurationSource);

-        protected TypeBase(Type clrType, Model model, ConfigurationSource configurationSource);

-        public virtual Type ClrType { get; }

-        IMutableModel Microsoft.EntityFrameworkCore.Metadata.IMutableTypeBase.Model { get; }

-        Type Microsoft.EntityFrameworkCore.Metadata.ITypeBase.ClrType { get; }

-        IModel Microsoft.EntityFrameworkCore.Metadata.ITypeBase.Model { get; }

-        public virtual Model Model { get; }

-        public virtual string Name { get; }

-        public virtual void ClearCaches();

-        public virtual Nullable<ConfigurationSource> FindDeclaredIgnoredMemberConfigurationSource(string name);

-        public virtual Nullable<ConfigurationSource> FindIgnoredMemberConfigurationSource(string name);

-        public virtual ConfigurationSource GetConfigurationSource();

-        public virtual IReadOnlyList<string> GetIgnoredMembers();

-        public virtual Dictionary<string, FieldInfo> GetRuntimeFields();

-        public virtual Dictionary<string, PropertyInfo> GetRuntimeProperties();

-        public virtual void Ignore(string name, ConfigurationSource configurationSource = ConfigurationSource.Explicit);

-        public abstract void OnTypeMemberIgnored(string name);

-        public abstract void PropertyMetadataChanged();

-        public virtual void Unignore(string name);

-        public virtual void UpdateConfigurationSource(ConfigurationSource configurationSource);

-    }
-    public static class TypeBaseExtensions {
 {
-        public static string DisplayName(this ITypeBase type);

-        public static bool HasClrType(this ITypeBase type);

-        public static bool IsAbstract(this ITypeBase type);

-    }
-    public readonly struct TypeIdentity {
 {
-        public TypeIdentity(string name);

-        public TypeIdentity(Type type, Model model);

-        public string Name { get; }

-        public Type Type { get; }

-    }
-}
```

