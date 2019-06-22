# Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal {
 {
-    public class BackingFieldConvention : INavigationAddedConvention, IPropertyAddedConvention {
 {
-        public BackingFieldConvention();

-        public virtual InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder);

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder, Navigation navigation);

-        protected virtual void Apply(PropertyBase propertyBase);

-    }
-    public class BaseTypeDiscoveryConvention : InheritanceDiscoveryConventionBase, IEntityTypeAddedConvention {
 {
-        public BaseTypeDiscoveryConvention();

-        public virtual InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder);

-    }
-    public class CacheCleanupConvention : IModelBuiltConvention {
 {
-        public CacheCleanupConvention();

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-    }
-    public class CascadeDeleteConvention : IForeignKeyAddedConvention, IForeignKeyRequirednessChangedConvention {
 {
-        public CascadeDeleteConvention();

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-        protected virtual DeleteBehavior TargetDeleteBehavior(ForeignKey foreignKey);

-    }
-    public class ChangeTrackingStrategyConvention : IModelBuiltConvention {
 {
-        public ChangeTrackingStrategyConvention();

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-    }
-    public class CompositeConventionSetBuilder : IConventionSetBuilder {
 {
-        public CompositeConventionSetBuilder(IReadOnlyList<IConventionSetBuilder> builders);

-        public virtual IReadOnlyList<IConventionSetBuilder> Builders { get; }

-        public virtual ConventionSet AddConventions(ConventionSet conventionSet);

-    }
-    public class ConcurrencyCheckAttributeConvention : PropertyAttributeConvention<ConcurrencyCheckAttribute> {
 {
-        public ConcurrencyCheckAttributeConvention();

-        public override InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder, ConcurrencyCheckAttribute attribute, MemberInfo clrMember);

-    }
-    public class ConstructorBindingConvention : IModelBuiltConvention {
 {
-        public ConstructorBindingConvention(IConstructorBindingFactory bindingFactory);

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-    }
-    public static class ConventionBatchExtensions {
 {
-        public static InternalRelationshipBuilder Run(this IConventionBatch batch, InternalRelationshipBuilder relationshipBuilder);

-    }
-    public class ConventionDispatcher {
 {
-        public ConventionDispatcher(ConventionSet conventionSet);

-        public virtual MetadataTracker Tracker { get; }

-        public virtual InternalEntityTypeBuilder OnBaseEntityTypeChanged(InternalEntityTypeBuilder entityTypeBuilder, EntityType previousBaseType);

-        public virtual InternalEntityTypeBuilder OnEntityTypeAdded(InternalEntityTypeBuilder entityTypeBuilder);

-        public virtual Annotation OnEntityTypeAnnotationChanged(InternalEntityTypeBuilder entityTypeBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-        public virtual bool OnEntityTypeIgnored(InternalModelBuilder modelBuilder, string name, Type type);

-        public virtual InternalEntityTypeBuilder OnEntityTypeMemberIgnored(InternalEntityTypeBuilder entityTypeBuilder, string ignoredMemberName);

-        public virtual bool OnEntityTypeRemoved(InternalModelBuilder modelBuilder, EntityType type);

-        public virtual InternalRelationshipBuilder OnForeignKeyAdded(InternalRelationshipBuilder relationshipBuilder);

-        public virtual InternalRelationshipBuilder OnForeignKeyOwnershipChanged(InternalRelationshipBuilder relationshipBuilder);

-        public virtual void OnForeignKeyRemoved(InternalEntityTypeBuilder entityTypeBuilder, ForeignKey foreignKey);

-        public virtual InternalRelationshipBuilder OnForeignKeyRequirednessChanged(InternalRelationshipBuilder relationshipBuilder);

-        public virtual InternalRelationshipBuilder OnForeignKeyUniquenessChanged(InternalRelationshipBuilder relationshipBuilder);

-        public virtual InternalIndexBuilder OnIndexAdded(InternalIndexBuilder indexBuilder);

-        public virtual Annotation OnIndexAnnotationChanged(InternalIndexBuilder indexBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-        public virtual void OnIndexRemoved(InternalEntityTypeBuilder entityTypeBuilder, Index index);

-        public virtual bool OnIndexUniquenessChanged(InternalIndexBuilder indexBuilder);

-        public virtual InternalKeyBuilder OnKeyAdded(InternalKeyBuilder keyBuilder);

-        public virtual void OnKeyRemoved(InternalEntityTypeBuilder entityTypeBuilder, Key key);

-        public virtual Annotation OnModelAnnotationChanged(InternalModelBuilder modelBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-        public virtual InternalModelBuilder OnModelBuilt(InternalModelBuilder modelBuilder);

-        public virtual InternalModelBuilder OnModelInitialized(InternalModelBuilder modelBuilder);

-        public virtual InternalRelationshipBuilder OnNavigationAdded(InternalRelationshipBuilder relationshipBuilder, Navigation navigation);

-        public virtual void OnNavigationRemoved(InternalEntityTypeBuilder sourceEntityTypeBuilder, InternalEntityTypeBuilder targetEntityTypeBuilder, string navigationName, MemberInfo memberInfo);

-        public virtual void OnPrimaryKeyChanged(InternalEntityTypeBuilder entityTypeBuilder, Key previousPrimaryKey);

-        public virtual InternalRelationshipBuilder OnPrincipalEndChanged(InternalRelationshipBuilder relationshipBuilder);

-        public virtual InternalPropertyBuilder OnPropertyAdded(InternalPropertyBuilder propertyBuilder);

-        public virtual Annotation OnPropertyAnnotationChanged(InternalPropertyBuilder propertyBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-        public virtual bool OnPropertyFieldChanged(InternalPropertyBuilder propertyBuilder, FieldInfo oldFieldInfo);

-        public virtual bool OnPropertyNullableChanged(InternalPropertyBuilder propertyBuilder);

-        public virtual IConventionBatch StartBatch();

-    }
-    public class CoreConventionSetBuilder : ICoreConventionSetBuilder {
 {
-        public CoreConventionSetBuilder(CoreConventionSetBuilderDependencies dependencies);

-        protected virtual CoreConventionSetBuilderDependencies Dependencies { get; }

-        public virtual ConventionSet CreateConventionSet();

-    }
-    public sealed class CoreConventionSetBuilderDependencies {
 {
-        public CoreConventionSetBuilderDependencies(ITypeMapper typeMapper);

-        public CoreConventionSetBuilderDependencies(ITypeMappingSource typeMappingSource, IConstructorBindingFactory constructorBindingFactory, IParameterBindingFactories parameterBindingFactories, IMemberClassifier memberClassifier, IDiagnosticsLogger<DbLoggerCategory.Model> logger, ITypeMapper _ = null);

-        public IConstructorBindingFactory ConstructorBindingFactory { get; }

-        public IDiagnosticsLogger<DbLoggerCategory.Model> Logger { get; }

-        public IMemberClassifier MemberClassifier { get; }

-        public IParameterBindingFactories ParameterBindingFactories { get; }

-        public ITypeMappingSource TypeMappingSource { get; }

-        public CoreConventionSetBuilderDependencies With(IDiagnosticsLogger<DbLoggerCategory.Model> logger);

-        public CoreConventionSetBuilderDependencies With(IConstructorBindingFactory constructorBindingFactory);

-        public CoreConventionSetBuilderDependencies With(IMemberClassifier memberClassifier);

-        public CoreConventionSetBuilderDependencies With(IParameterBindingFactories parameterBindingFactories);

-        public CoreConventionSetBuilderDependencies With(ITypeMapper typeMapper);

-        public CoreConventionSetBuilderDependencies With(ITypeMappingSource typeMappingSource);

-    }
-    public class DatabaseGeneratedAttributeConvention : PropertyAttributeConvention<DatabaseGeneratedAttribute> {
 {
-        public DatabaseGeneratedAttributeConvention();

-        public override InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder, DatabaseGeneratedAttribute attribute, MemberInfo clrMember);

-    }
-    public class DerivedTypeDiscoveryConvention : InheritanceDiscoveryConventionBase, IEntityTypeAddedConvention {
 {
-        public DerivedTypeDiscoveryConvention();

-        public virtual InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder);

-    }
-    public class DiscriminatorConvention : IBaseTypeChangedConvention, IEntityTypeRemovedConvention {
 {
-        public DiscriminatorConvention();

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType);

-        public virtual bool Apply(InternalModelBuilder modelBuilder, EntityType type);

-    }
-    public abstract class EntityTypeAttributeConvention<TAttribute> : IEntityTypeAddedConvention where TAttribute : Attribute {
 {
-        protected EntityTypeAttributeConvention();

-        public virtual InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder);

-        public abstract InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder, TAttribute attribute);

-    }
-    public class ForeignKeyAttributeConvention : IForeignKeyAddedConvention, IModelBuiltConvention {
 {
-        public ForeignKeyAttributeConvention(IMemberClassifier memberClassifier, IDiagnosticsLogger<DbLoggerCategory.Model> logger);

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-        protected virtual Type FindCandidateNavigationPropertyType(PropertyInfo propertyInfo);

-    }
-    public class ForeignKeyIndexConvention : IBaseTypeChangedConvention, IForeignKeyAddedConvention, IForeignKeyRemovedConvention, IForeignKeyUniquenessChangedConvention, IIndexAddedConvention, IIndexRemovedConvention, IIndexUniquenessChangedConvention, IKeyAddedConvention, IKeyRemovedConvention, IModelBuiltConvention {
 {
-        public ForeignKeyIndexConvention(IDiagnosticsLogger<DbLoggerCategory.Model> logger);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType);

-        public virtual void Apply(InternalEntityTypeBuilder entityTypeBuilder, ForeignKey foreignKey);

-        public virtual void Apply(InternalEntityTypeBuilder entityTypeBuilder, Index index);

-        public virtual void Apply(InternalEntityTypeBuilder entityTypeBuilder, Key key);

-        public virtual InternalIndexBuilder Apply(InternalIndexBuilder indexBuilder);

-        public virtual InternalKeyBuilder Apply(InternalKeyBuilder keyBuilder);

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-        protected virtual bool AreIndexedBy(IReadOnlyList<Property> properties, bool unique, IReadOnlyList<Property> coveringIndexProperties, bool coveringIndexUniqueness);

-        protected virtual Index CreateIndex(IReadOnlyList<Property> properties, bool unique, InternalEntityTypeBuilder entityTypeBuilder);

-        InternalRelationshipBuilder Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal.IForeignKeyUniquenessChangedConvention.Apply(InternalRelationshipBuilder relationshipBuilder);

-        bool Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal.IIndexUniquenessChangedConvention.Apply(InternalIndexBuilder indexBuilder);

-    }
-    public class ForeignKeyPropertyDiscoveryConvention : IEntityTypeMemberIgnoredConvention, IForeignKeyAddedConvention, IForeignKeyRequirednessChangedConvention, IForeignKeyUniquenessChangedConvention, IKeyAddedConvention, IKeyRemovedConvention, IModelBuiltConvention, INavigationAddedConvention, IPrimaryKeyChangedConvention, IPrincipalEndChangedConvention, IPropertyAddedConvention, IPropertyFieldChangedConvention {
 {
-        public ForeignKeyPropertyDiscoveryConvention(IDiagnosticsLogger<DbLoggerCategory.Model> logger);

-        public virtual void Apply(InternalEntityTypeBuilder entityTypeBuilder, Key key);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, string ignoredMemberName);

-        public virtual InternalKeyBuilder Apply(InternalKeyBuilder keyBuilder);

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-        public virtual InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder);

-        public virtual bool Apply(InternalPropertyBuilder propertyBuilder, FieldInfo oldFieldInfo);

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder, Navigation navigation);

-        InternalRelationshipBuilder Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal.IForeignKeyRequirednessChangedConvention.Apply(InternalRelationshipBuilder relationshipBuilder);

-        InternalRelationshipBuilder Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal.IForeignKeyUniquenessChangedConvention.Apply(InternalRelationshipBuilder relationshipBuilder);

-        bool Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal.IPrimaryKeyChangedConvention.Apply(InternalEntityTypeBuilder entityTypeBuilder, Key previousPrimaryKey);

-    }
-    public interface IBaseTypeChangedConvention {
 {
-        bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType);

-    }
-    public interface IConventionBatch : IDisposable {
 {
-        ForeignKey Run(ForeignKey foreignKey);

-    }
-    public interface IConventionSetBuilder {
 {
-        ConventionSet AddConventions(ConventionSet conventionSet);

-    }
-    public interface ICoreConventionSetBuilder {
 {
-        ConventionSet CreateConventionSet();

-    }
-    public interface IEntityTypeAddedConvention {
 {
-        InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder);

-    }
-    public interface IEntityTypeAnnotationChangedConvention {
 {
-        Annotation Apply(InternalEntityTypeBuilder entityTypeBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-    }
-    public interface IEntityTypeIgnoredConvention {
 {
-        bool Apply(InternalModelBuilder modelBuilder, string name, Type type);

-    }
-    public interface IEntityTypeMemberIgnoredConvention {
 {
-        bool Apply(InternalEntityTypeBuilder entityTypeBuilder, string ignoredMemberName);

-    }
-    public interface IEntityTypeRemovedConvention {
 {
-        bool Apply(InternalModelBuilder modelBuilder, EntityType type);

-    }
-    public interface IForeignKeyAddedConvention {
 {
-        InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-    }
-    public interface IForeignKeyOwnershipChangedConvention {
 {
-        InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-    }
-    public interface IForeignKeyRemovedConvention {
 {
-        void Apply(InternalEntityTypeBuilder entityTypeBuilder, ForeignKey foreignKey);

-    }
-    public interface IForeignKeyRequirednessChangedConvention {
 {
-        InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-    }
-    public interface IForeignKeyUniquenessChangedConvention {
 {
-        InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-    }
-    public class IgnoredMembersValidationConvention : IModelBuiltConvention {
 {
-        public IgnoredMembersValidationConvention();

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-    }
-    public interface IIndexAddedConvention {
 {
-        InternalIndexBuilder Apply(InternalIndexBuilder indexBuilder);

-    }
-    public interface IIndexAnnotationChangedConvention {
 {
-        Annotation Apply(InternalIndexBuilder indexBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-    }
-    public interface IIndexRemovedConvention {
 {
-        void Apply(InternalEntityTypeBuilder entityTypeBuilder, Index index);

-    }
-    public interface IIndexUniquenessChangedConvention {
 {
-        bool Apply(InternalIndexBuilder indexBuilder);

-    }
-    public interface IKeyAddedConvention {
 {
-        InternalKeyBuilder Apply(InternalKeyBuilder keyBuilder);

-    }
-    public interface IKeyRemovedConvention {
 {
-        void Apply(InternalEntityTypeBuilder entityTypeBuilder, Key key);

-    }
-    public interface IModelAnnotationChangedConvention {
 {
-        Annotation Apply(InternalModelBuilder modelBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-    }
-    public interface IModelBuiltConvention {
 {
-        InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-    }
-    public interface IModelInitializedConvention {
 {
-        InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-    }
-    public interface INavigationAddedConvention {
 {
-        InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder, Navigation navigation);

-    }
-    public interface INavigationRemovedConvention {
 {
-        bool Apply(InternalEntityTypeBuilder sourceEntityTypeBuilder, InternalEntityTypeBuilder targetEntityTypeBuilder, string navigationName, MemberInfo memberInfo);

-    }
-    public abstract class InheritanceDiscoveryConventionBase {
 {
-        protected InheritanceDiscoveryConventionBase();

-        protected virtual EntityType FindClosestBaseType(EntityType entityType);

-    }
-    public class InversePropertyAttributeConvention : NavigationAttributeEntityTypeConvention<InversePropertyAttribute>, IModelBuiltConvention {
 {
-        public const string InverseNavigationsAnnotationName = "InversePropertyAttributeConvention:InverseNavigations";

-        public InversePropertyAttributeConvention(IMemberClassifier memberClassifier, IDiagnosticsLogger<DbLoggerCategory.Model> logger);

-        public override bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType, PropertyInfo navigationPropertyInfo, Type targetClrType, InversePropertyAttribute attribute);

-        public override InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder, PropertyInfo navigationPropertyInfo, Type targetClrType, InversePropertyAttribute attribute);

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-        public override bool Apply(InternalModelBuilder modelBuilder, Type type, PropertyInfo navigationPropertyInfo, Type targetClrType, InversePropertyAttribute attribute);

-        public override InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder, Navigation navigation, InversePropertyAttribute attribute);

-        public override bool ApplyIgnored(InternalEntityTypeBuilder entityTypeBuilder, PropertyInfo navigationPropertyInfo, Type targetClrType, InversePropertyAttribute attribute);

-        public static bool IsAmbiguous(EntityType entityType, MemberInfo navigation, EntityType targetEntityType);

-    }
-    public interface IPrimaryKeyChangedConvention {
 {
-        bool Apply(InternalEntityTypeBuilder entityTypeBuilder, Key previousPrimaryKey);

-    }
-    public interface IPrincipalEndChangedConvention {
 {
-        InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-    }
-    public interface IPropertyAddedConvention {
 {
-        InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder);

-    }
-    public interface IPropertyAnnotationChangedConvention {
 {
-        Annotation Apply(InternalPropertyBuilder propertyBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-    }
-    public interface IPropertyFieldChangedConvention {
 {
-        bool Apply(InternalPropertyBuilder propertyBuilder, FieldInfo oldFieldInfo);

-    }
-    public interface IPropertyNullabilityChangedConvention {
 {
-        bool Apply(InternalPropertyBuilder propertyBuilder);

-    }
-    public class KeyAttributeConvention : PropertyAttributeConvention<KeyAttribute>, IModelBuiltConvention {
 {
-        public KeyAttributeConvention();

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-        public override InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder, KeyAttribute attribute, MemberInfo clrMember);

-    }
-    public class KeyDiscoveryConvention : IBaseTypeChangedConvention, IEntityTypeAddedConvention, IForeignKeyAddedConvention, IForeignKeyOwnershipChangedConvention, IForeignKeyRemovedConvention, IForeignKeyUniquenessChangedConvention, IKeyRemovedConvention, IPropertyAddedConvention, IPropertyFieldChangedConvention {
 {
-        public KeyDiscoveryConvention(IDiagnosticsLogger<DbLoggerCategory.Model> logger);

-        public virtual InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType);

-        public virtual void Apply(InternalEntityTypeBuilder entityTypeBuilder, ForeignKey foreignKey);

-        public virtual void Apply(InternalEntityTypeBuilder entityTypeBuilder, Key key);

-        public virtual InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder);

-        public virtual bool Apply(InternalPropertyBuilder propertyBuilder, FieldInfo oldFieldInfo);

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-        public virtual IEnumerable<Property> DiscoverKeyProperties(EntityType entityType, IReadOnlyList<Property> candidateProperties);

-        InternalRelationshipBuilder Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal.IForeignKeyOwnershipChangedConvention.Apply(InternalRelationshipBuilder relationshipBuilder);

-    }
-    public class MaxLengthAttributeConvention : PropertyAttributeConvention<MaxLengthAttribute> {
 {
-        public MaxLengthAttributeConvention();

-        public override InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder, MaxLengthAttribute attribute, MemberInfo clrMember);

-    }
-    public class MetadataTracker : IReferenceRoot<ForeignKey> {
 {
-        public MetadataTracker();

-        void Microsoft.EntityFrameworkCore.Internal.IReferenceRoot<Microsoft.EntityFrameworkCore.Metadata.Internal.ForeignKey>.Release(Reference<ForeignKey> foreignKeyReference);

-        public virtual Reference<ForeignKey> Track(ForeignKey foreignKey);

-        public virtual void Update(ForeignKey oldForeignKey, ForeignKey newForeignKey);

-    }
-    public class ModelCleanupConvention : IModelBuiltConvention {
 {
-        public ModelCleanupConvention();

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-    }
-    public abstract class NavigationAttributeEntityTypeConvention<TAttribute> : IBaseTypeChangedConvention, IEntityTypeAddedConvention, IEntityTypeIgnoredConvention, IEntityTypeMemberIgnoredConvention, INavigationAddedConvention where TAttribute : Attribute {
 {
-        protected NavigationAttributeEntityTypeConvention(IMemberClassifier memberClassifier);

-        public virtual InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType, PropertyInfo navigationPropertyInfo, Type targetClrType, TAttribute attribute);

-        public virtual InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder, PropertyInfo navigationPropertyInfo, Type targetClrType, TAttribute attribute);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, string ignoredMemberName);

-        public virtual bool Apply(InternalModelBuilder modelBuilder, string name, Type type);

-        public virtual bool Apply(InternalModelBuilder modelBuilder, Type type, PropertyInfo navigationPropertyInfo, Type targetClrType, TAttribute attribute);

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder, Navigation navigation);

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder, Navigation navigation, TAttribute attribute);

-        public virtual bool ApplyIgnored(InternalEntityTypeBuilder entityTypeBuilder, PropertyInfo navigationPropertyInfo, Type targetClrType, TAttribute attribute);

-        protected virtual Type FindCandidateNavigationPropertyType(PropertyInfo propertyInfo);

-    }
-    public abstract class NavigationAttributeNavigationConvention<TAttribute> : INavigationAddedConvention where TAttribute : Attribute {
 {
-        protected NavigationAttributeNavigationConvention();

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder, Navigation navigation);

-        public abstract InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder, Navigation navigation, TAttribute attribute);

-        protected static IEnumerable<TCustomAttribute> GetAttributes<TCustomAttribute>(EntityType entityType, string propertyName) where TCustomAttribute : Attribute;

-    }
-    public class NavigationEagerLoadingConvention : IForeignKeyOwnershipChangedConvention {
 {
-        public NavigationEagerLoadingConvention();

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-    }
-    public class NotMappedEntityTypeAttributeConvention : EntityTypeAttributeConvention<NotMappedAttribute> {
 {
-        public NotMappedEntityTypeAttributeConvention();

-        public override InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder, NotMappedAttribute attribute);

-    }
-    public class NotMappedMemberAttributeConvention : IEntityTypeAddedConvention {
 {
-        public NotMappedMemberAttributeConvention();

-        public virtual InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder);

-    }
-    public class NullConventionSetBuilder : IConventionSetBuilder {
 {
-        public NullConventionSetBuilder();

-        public virtual ConventionSet AddConventions(ConventionSet conventionSet);

-    }
-    public class OwnedEntityTypeAttributeConvention : EntityTypeAttributeConvention<OwnedAttribute> {
 {
-        public OwnedEntityTypeAttributeConvention();

-        public override InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder, OwnedAttribute attribute);

-    }
-    public class OwnedTypesConvention : IEntityTypeRemovedConvention {
 {
-        public OwnedTypesConvention();

-        public virtual bool Apply(InternalModelBuilder modelBuilder, EntityType type);

-    }
-    public abstract class PropertyAttributeConvention<TAttribute> : IPropertyAddedConvention, IPropertyFieldChangedConvention where TAttribute : Attribute {
 {
-        protected PropertyAttributeConvention();

-        public virtual InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder);

-        public virtual bool Apply(InternalPropertyBuilder propertyBuilder, FieldInfo oldFieldInfo);

-        public abstract InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder, TAttribute attribute, MemberInfo clrMember);

-    }
-    public class PropertyDiscoveryConvention : IBaseTypeChangedConvention, IEntityTypeAddedConvention {
 {
-        public PropertyDiscoveryConvention(ITypeMappingSource typeMappingSource);

-        public virtual InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType);

-        protected virtual bool IsCandidatePrimitiveProperty(PropertyInfo propertyInfo);

-    }
-    public class PropertyMappingValidationConvention : IModelBuiltConvention {
 {
-        public PropertyMappingValidationConvention(ITypeMappingSource typeMappingSource, IMemberClassifier memberClassifier);

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-        protected virtual Type FindCandidateNavigationPropertyType(PropertyInfo propertyInfo);

-        protected virtual bool IsMappedPrimitiveProperty(IProperty property);

-    }
-    public class RelationalColumnAttributeConvention : PropertyAttributeConvention<ColumnAttribute> {
 {
-        public RelationalColumnAttributeConvention();

-        public override InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder, ColumnAttribute attribute, MemberInfo clrMember);

-    }
-    public abstract class RelationalConventionSetBuilder : IConventionSetBuilder {
 {
-        protected RelationalConventionSetBuilder(RelationalConventionSetBuilderDependencies dependencies);

-        protected virtual RelationalConventionSetBuilderDependencies Dependencies { get; }

-        public virtual ConventionSet AddConventions(ConventionSet conventionSet);

-        protected virtual void ReplaceConvention<T1, T2>(IList<T1> conventionsList, T2 newConvention) where T2 : T1;

-    }
-    public sealed class RelationalConventionSetBuilderDependencies {
 {
-        public RelationalConventionSetBuilderDependencies(IRelationalTypeMapper typeMapper, ICurrentDbContext currentContext, IDbSetFinder setFinder);

-        public RelationalConventionSetBuilderDependencies(IRelationalTypeMappingSource typeMappingSource, IDiagnosticsLogger<DbLoggerCategory.Model> logger, ICurrentDbContext currentContext, IDbSetFinder setFinder, IRelationalTypeMapper typeMapper);

-        public ICurrentDbContext Context { get; }

-        public IDiagnosticsLogger<DbLoggerCategory.Model> Logger { get; }

-        public IDbSetFinder SetFinder { get; }

-        public IRelationalTypeMapper TypeMapper { get; }

-        public IRelationalTypeMappingSource TypeMappingSource { get; }

-        public RelationalConventionSetBuilderDependencies With(IDiagnosticsLogger<DbLoggerCategory.Model> logger);

-        public RelationalConventionSetBuilderDependencies With(ICurrentDbContext currentContext);

-        public RelationalConventionSetBuilderDependencies With(IDbSetFinder setFinder);

-        public RelationalConventionSetBuilderDependencies With(IRelationalTypeMapper typeMapper);

-        public RelationalConventionSetBuilderDependencies With(IRelationalTypeMappingSource typeMappingSource);

-    }
-    public class RelationalDbFunctionConvention : IModelAnnotationChangedConvention {
 {
-        public RelationalDbFunctionConvention();

-        public virtual Annotation Apply(InternalModelBuilder modelBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-        protected virtual void ApplyCustomizations(InternalModelBuilder modelBuilder, string name, Annotation annotation);

-    }
-    public class RelationalMaxIdentifierLengthConvention : IModelInitializedConvention {
 {
-        public RelationalMaxIdentifierLengthConvention(int maxIdentifierLength);

-        public virtual int MaxIdentifierLength { get; }

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-    }
-    public class RelationalTableAttributeConvention : EntityTypeAttributeConvention<TableAttribute> {
 {
-        public RelationalTableAttributeConvention();

-        public override InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder, TableAttribute attribute);

-    }
-    public class RelationalValueGeneratorConvention : ValueGeneratorConvention, IPropertyAnnotationChangedConvention {
 {
-        public RelationalValueGeneratorConvention();

-        public virtual Annotation Apply(InternalPropertyBuilder propertyBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-        public override Nullable<ValueGenerated> GetValueGenerated(Property property);

-    }
-    public class RelationshipDiscoveryConvention : IBaseTypeChangedConvention, IEntityTypeAddedConvention, IEntityTypeMemberIgnoredConvention, IForeignKeyOwnershipChangedConvention, INavigationAddedConvention, INavigationRemovedConvention {
 {
-        public const string AmbiguousNavigationsAnnotationName = "RelationshipDiscoveryConvention:AmbiguousNavigations";

-        public const string NavigationCandidatesAnnotationName = "RelationshipDiscoveryConvention:NavigationCandidates";

-        public RelationshipDiscoveryConvention(IMemberClassifier memberClassifier, IDiagnosticsLogger<DbLoggerCategory.Model> logger);

-        public virtual InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType);

-        public virtual bool Apply(InternalEntityTypeBuilder sourceEntityTypeBuilder, InternalEntityTypeBuilder targetEntityTypeBuilder, string navigationName, MemberInfo propertyInfo);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, string ignoredMemberName);

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder, Navigation navigation);

-        public virtual Type FindCandidateNavigationPropertyType(PropertyInfo propertyInfo);

-        public static InternalEntityTypeBuilder GetTargetEntityTypeBuilder(InternalEntityTypeBuilder entityTypeBuilder, Type targetClrType, MemberInfo navigationInfo, Nullable<ConfigurationSource> configurationSource);

-        InternalRelationshipBuilder Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal.IForeignKeyOwnershipChangedConvention.Apply(InternalRelationshipBuilder relationshipBuilder);

-    }
-    public class RelationshipValidationConvention : IModelBuiltConvention {
 {
-        public RelationshipValidationConvention();

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-    }
-    public class RequiredNavigationAttributeConvention : NavigationAttributeNavigationConvention<RequiredAttribute> {
 {
-        public RequiredNavigationAttributeConvention(IDiagnosticsLogger<DbLoggerCategory.Model> logger);

-        public override InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder, Navigation navigation, RequiredAttribute attribute);

-    }
-    public class RequiredPropertyAttributeConvention : PropertyAttributeConvention<RequiredAttribute> {
 {
-        public RequiredPropertyAttributeConvention();

-        public override InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder, RequiredAttribute attribute, MemberInfo clrMember);

-    }
-    public class ServicePropertyDiscoveryConvention : IBaseTypeChangedConvention, IEntityTypeAddedConvention, IEntityTypeMemberIgnoredConvention, IModelBuiltConvention {
 {
-        public ServicePropertyDiscoveryConvention(ITypeMappingSource typeMappingSource, IParameterBindingFactories parameterBindingFactories);

-        public virtual InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, string ignoredMemberName);

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-    }
-    public class SharedTableConvention : IEntityTypeAddedConvention, IEntityTypeAnnotationChangedConvention, IForeignKeyOwnershipChangedConvention, IForeignKeyUniquenessChangedConvention, IModelBuiltConvention {
 {
-        public SharedTableConvention();

-        public virtual InternalEntityTypeBuilder Apply(InternalEntityTypeBuilder entityTypeBuilder);

-        public virtual Annotation Apply(InternalEntityTypeBuilder entityTypeBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-    }
-    public class StringLengthAttributeConvention : PropertyAttributeConvention<StringLengthAttribute> {
 {
-        public StringLengthAttributeConvention();

-        public override InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder, StringLengthAttribute attribute, MemberInfo clrMember);

-    }
-    public class TableNameFromDbSetConvention : IBaseTypeChangedConvention {
 {
-        public TableNameFromDbSetConvention(DbContext context, IDbSetFinder setFinder);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType);

-    }
-    public class TimestampAttributeConvention : PropertyAttributeConvention<TimestampAttribute> {
 {
-        public TimestampAttributeConvention();

-        public override InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder, TimestampAttribute attribute, MemberInfo clrMember);

-    }
-    public class TypeMappingConvention : IModelBuiltConvention {
 {
-        public TypeMappingConvention(ITypeMappingSource typeMappingSource);

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-    }
-    public class ValidatingConvention : IModelBuiltConvention {
 {
-        public ValidatingConvention(IModelValidator validator);

-        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder);

-    }
-    public class ValueGeneratorConvention : IBaseTypeChangedConvention, IForeignKeyAddedConvention, IForeignKeyRemovedConvention, IPrimaryKeyChangedConvention {
 {
-        public ValueGeneratorConvention();

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType);

-        public virtual void Apply(InternalEntityTypeBuilder entityTypeBuilder, ForeignKey foreignKey);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, Key previousPrimaryKey);

-        public virtual InternalRelationshipBuilder Apply(InternalRelationshipBuilder relationshipBuilder);

-        public virtual Nullable<ValueGenerated> GetValueGenerated(Property property);

-    }
-}
```

