# Microsoft.EntityFrameworkCore.Metadata.Builders

``` diff
-namespace Microsoft.EntityFrameworkCore.Metadata.Builders {
 {
-    public class CollectionNavigationBuilder : IInfrastructure<InternalRelationshipBuilder> {
 {
-        public CollectionNavigationBuilder(EntityType declaringEntityType, EntityType relatedEntityType, PropertyInfo navigationProperty, InternalRelationshipBuilder builder);

-        public CollectionNavigationBuilder(EntityType declaringEntityType, EntityType relatedEntityType, string navigationName, InternalRelationshipBuilder builder);

-        protected virtual string CollectionName { get; }

-        protected virtual PropertyInfo CollectionProperty { get; }

-        protected virtual EntityType DeclaringEntityType { get; }

-        InternalRelationshipBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.Internal.InternalRelationshipBuilder>.Instance { get; }

-        protected virtual EntityType RelatedEntityType { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        public virtual ReferenceCollectionBuilder WithOne(string navigationName = null);

-        protected virtual InternalRelationshipBuilder WithOneBuilder(PropertyInfo navigationProperty);

-        protected virtual InternalRelationshipBuilder WithOneBuilder(string navigationName);

-    }
-    public class CollectionNavigationBuilder<TEntity, TRelatedEntity> : CollectionNavigationBuilder where TEntity : class where TRelatedEntity : class {
 {
-        public CollectionNavigationBuilder(EntityType declaringEntityType, EntityType relatedEntityType, PropertyInfo navigationProperty, InternalRelationshipBuilder builder);

-        public CollectionNavigationBuilder(EntityType declaringEntityType, EntityType relatedEntityType, string navigationName, InternalRelationshipBuilder builder);

-        public virtual ReferenceCollectionBuilder<TEntity, TRelatedEntity> WithOne(Expression<Func<TRelatedEntity, TEntity>> navigationExpression);

-        public virtual new ReferenceCollectionBuilder<TEntity, TRelatedEntity> WithOne(string navigationName = null);

-    }
-    public class CollectionOwnershipBuilder : ReferenceCollectionBuilderBase, IInfrastructure<InternalEntityTypeBuilder> {
 {
-        public CollectionOwnershipBuilder(EntityType declaringEntityType, EntityType relatedEntityType, InternalRelationshipBuilder builder);

-        protected CollectionOwnershipBuilder(InternalRelationshipBuilder builder, CollectionOwnershipBuilder oldBuilder, bool foreignKeySet = false, bool principalKeySet = false, bool requiredSet = false);

-        InternalEntityTypeBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.Internal.InternalEntityTypeBuilder>.Instance { get; }

-        public virtual IMutableEntityType OwnedEntityType { get; }

-        protected virtual EntityType FindRelatedEntityType(string relatedTypeName, string navigationName);

-        protected virtual EntityType FindRelatedEntityType(Type relatedType, string navigationName);

-        public virtual CollectionOwnershipBuilder HasChangeTrackingStrategy(ChangeTrackingStrategy changeTrackingStrategy);

-        public virtual DataBuilder HasData(params object[] data);

-        public virtual CollectionOwnershipBuilder HasEntityTypeAnnotation(string annotation, object value);

-        public virtual CollectionOwnershipBuilder HasForeignKey(params string[] foreignKeyPropertyNames);

-        public virtual CollectionOwnershipBuilder HasForeignKeyAnnotation(string annotation, object value);

-        public virtual IndexBuilder HasIndex(params string[] propertyNames);

-        public virtual KeyBuilder HasKey(params string[] propertyNames);

-        public virtual ReferenceNavigationBuilder HasOne(string relatedTypeName, string navigationName = null);

-        public virtual ReferenceNavigationBuilder HasOne(Type relatedType, string navigationName = null);

-        public virtual CollectionOwnershipBuilder HasPrincipalKey(params string[] keyPropertyNames);

-        public virtual CollectionOwnershipBuilder Ignore(string propertyName);

-        public virtual CollectionOwnershipBuilder OnDelete(DeleteBehavior deleteBehavior);

-        public virtual CollectionOwnershipBuilder OwnsMany(string ownedTypeName, string navigationName);

-        public virtual CollectionOwnershipBuilder OwnsMany(string ownedTypeName, string navigationName, Action<CollectionOwnershipBuilder> buildAction);

-        public virtual CollectionOwnershipBuilder OwnsMany(Type ownedType, string navigationName);

-        public virtual CollectionOwnershipBuilder OwnsMany(Type ownedType, string navigationName, Action<CollectionOwnershipBuilder> buildAction);

-        public virtual ReferenceOwnershipBuilder OwnsOne(string ownedTypeName, string navigationName);

-        public virtual CollectionOwnershipBuilder OwnsOne(string ownedTypeName, string navigationName, Action<ReferenceOwnershipBuilder> buildAction);

-        public virtual ReferenceOwnershipBuilder OwnsOne(Type ownedType, string navigationName);

-        public virtual CollectionOwnershipBuilder OwnsOne(Type ownedType, string navigationName, Action<ReferenceOwnershipBuilder> buildAction);

-        public virtual PropertyBuilder Property(string propertyName);

-        public virtual PropertyBuilder Property(Type propertyType, string propertyName);

-        public virtual PropertyBuilder<TProperty> Property<TProperty>(string propertyName);

-        public virtual CollectionOwnershipBuilder UsePropertyAccessMode(PropertyAccessMode propertyAccessMode);

-    }
-    public class CollectionOwnershipBuilder<TEntity, TDependentEntity> : CollectionOwnershipBuilder where TEntity : class where TDependentEntity : class {
 {
-        public CollectionOwnershipBuilder(EntityType declaringEntityType, EntityType relatedEntityType, InternalRelationshipBuilder builder);

-        protected CollectionOwnershipBuilder(InternalRelationshipBuilder builder, CollectionOwnershipBuilder<TEntity, TDependentEntity> oldBuilder, bool foreignKeySet = false, bool principalKeySet = false, bool requiredSet = false);

-        public virtual new CollectionOwnershipBuilder<TEntity, TDependentEntity> HasChangeTrackingStrategy(ChangeTrackingStrategy changeTrackingStrategy);

-        public virtual new DataBuilder<TDependentEntity> HasData(params object[] data);

-        public virtual DataBuilder<TDependentEntity> HasData(params TDependentEntity[] data);

-        public virtual new CollectionOwnershipBuilder<TEntity, TDependentEntity> HasEntityTypeAnnotation(string annotation, object value);

-        public virtual CollectionOwnershipBuilder<TEntity, TDependentEntity> HasForeignKey(Expression<Func<TDependentEntity, object>> foreignKeyExpression);

-        public virtual new CollectionOwnershipBuilder<TEntity, TDependentEntity> HasForeignKey(params string[] foreignKeyPropertyNames);

-        public virtual new CollectionOwnershipBuilder<TEntity, TDependentEntity> HasForeignKeyAnnotation(string annotation, object value);

-        public virtual IndexBuilder HasIndex(Expression<Func<TDependentEntity, object>> indexExpression);

-        public virtual KeyBuilder HasKey(Expression<Func<TDependentEntity, object>> keyExpression);

-        public virtual ReferenceNavigationBuilder<TDependentEntity, TNewRelatedEntity> HasOne<TNewRelatedEntity>(Expression<Func<TDependentEntity, TNewRelatedEntity>> navigationExpression = null) where TNewRelatedEntity : class;

-        public virtual ReferenceNavigationBuilder<TDependentEntity, TNewRelatedEntity> HasOne<TNewRelatedEntity>(string navigationName) where TNewRelatedEntity : class;

-        public virtual CollectionOwnershipBuilder<TEntity, TDependentEntity> HasPrincipalKey(Expression<Func<TEntity, object>> keyExpression);

-        public virtual new CollectionOwnershipBuilder<TEntity, TDependentEntity> HasPrincipalKey(params string[] keyPropertyNames);

-        public virtual CollectionOwnershipBuilder<TEntity, TDependentEntity> Ignore(Expression<Func<TDependentEntity, object>> propertyExpression);

-        public virtual new CollectionOwnershipBuilder<TEntity, TDependentEntity> Ignore(string propertyName);

-        public virtual new CollectionOwnershipBuilder<TEntity, TDependentEntity> OnDelete(DeleteBehavior deleteBehavior);

-        public virtual CollectionOwnershipBuilder<TDependentEntity, TNewDependentEntity> OwnsMany<TNewDependentEntity>(Expression<Func<TDependentEntity, IEnumerable<TNewDependentEntity>>> navigationExpression) where TNewDependentEntity : class;

-        public virtual CollectionOwnershipBuilder<TEntity, TDependentEntity> OwnsMany<TNewDependentEntity>(Expression<Func<TDependentEntity, IEnumerable<TNewDependentEntity>>> navigationExpression, Action<CollectionOwnershipBuilder<TDependentEntity, TNewDependentEntity>> buildAction) where TNewDependentEntity : class;

-        public virtual CollectionOwnershipBuilder<TDependentEntity, TNewDependentEntity> OwnsMany<TNewDependentEntity>(string navigationName) where TNewDependentEntity : class;

-        public virtual CollectionOwnershipBuilder<TEntity, TDependentEntity> OwnsMany<TNewDependentEntity>(string navigationName, Action<CollectionOwnershipBuilder<TDependentEntity, TNewDependentEntity>> buildAction) where TNewDependentEntity : class;

-        public virtual ReferenceOwnershipBuilder<TDependentEntity, TNewRelatedEntity> OwnsOne<TNewRelatedEntity>(string navigationName) where TNewRelatedEntity : class;

-        public virtual ReferenceOwnershipBuilder<TDependentEntity, TRelatedEntity> OwnsOne<TRelatedEntity>(Expression<Func<TDependentEntity, TRelatedEntity>> navigationExpression) where TRelatedEntity : class;

-        public virtual CollectionOwnershipBuilder<TEntity, TDependentEntity> OwnsOne<TRelatedEntity>(Expression<Func<TDependentEntity, TRelatedEntity>> navigationExpression, Action<ReferenceOwnershipBuilder<TDependentEntity, TRelatedEntity>> buildAction) where TRelatedEntity : class;

-        public virtual CollectionOwnershipBuilder<TEntity, TDependentEntity> OwnsOne<TRelatedEntity>(string navigationName, Action<ReferenceOwnershipBuilder<TDependentEntity, TRelatedEntity>> buildAction) where TRelatedEntity : class;

-        public virtual PropertyBuilder<TProperty> Property<TProperty>(Expression<Func<TDependentEntity, TProperty>> propertyExpression);

-        public virtual new CollectionOwnershipBuilder<TEntity, TDependentEntity> UsePropertyAccessMode(PropertyAccessMode propertyAccessMode);

-    }
-    public class DataBuilder {
 {
-        public DataBuilder();

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public class DataBuilder<TEntity> : DataBuilder {
 {
-        public DataBuilder();

-    }
-    public class DbFunctionBuilder {
 {
-        public DbFunctionBuilder(DbFunction function);

-        public virtual IMutableDbFunction Metadata { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public virtual DbFunctionBuilder HasName(string name);

-        public virtual DbFunctionBuilder HasSchema(string schema);

-        public virtual DbFunctionBuilder HasTranslation(Func<IReadOnlyCollection<Expression>, Expression> translation);

-        public override string ToString();

-    }
-    public class DiscriminatorBuilder {
 {
-        public DiscriminatorBuilder(RelationalAnnotationsBuilder annotationsBuilder, Func<InternalEntityTypeBuilder, RelationalEntityTypeBuilderAnnotations> getRelationalEntityTypeBuilderAnnotations);

-        protected virtual RelationalAnnotationsBuilder AnnotationsBuilder { get; }

-        protected virtual InternalEntityTypeBuilder EntityTypeBuilder { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public virtual DiscriminatorBuilder HasValue(object value);

-        public virtual DiscriminatorBuilder HasValue(string entityTypeName, object value);

-        public virtual DiscriminatorBuilder HasValue(Type entityType, object value);

-        public virtual DiscriminatorBuilder HasValue<TEntity>(object value);

-        public override string ToString();

-    }
-    public class DiscriminatorBuilder<TDiscriminator> {
 {
-        public DiscriminatorBuilder(DiscriminatorBuilder builder);

-        public virtual DiscriminatorBuilder<TDiscriminator> HasValue(string entityTypeName, TDiscriminator value);

-        public virtual DiscriminatorBuilder<TDiscriminator> HasValue(Type entityType, TDiscriminator value);

-        public virtual DiscriminatorBuilder<TDiscriminator> HasValue(TDiscriminator value);

-        public virtual DiscriminatorBuilder<TDiscriminator> HasValue<TEntity>(TDiscriminator value);

-    }
-    public class EntityTypeBuilder : IInfrastructure<IMutableModel>, IInfrastructure<InternalEntityTypeBuilder> {
 {
-        public EntityTypeBuilder(InternalEntityTypeBuilder builder);

-        public virtual IMutableEntityType Metadata { get; }

-        IMutableModel Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.IMutableModel>.Instance { get; }

-        InternalEntityTypeBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.Internal.InternalEntityTypeBuilder>.Instance { get; }

-        public override bool Equals(object obj);

-        protected virtual EntityType FindRelatedEntityType(string relatedTypeName, string navigationName);

-        protected virtual EntityType FindRelatedEntityType(Type relatedType, string navigationName);

-        public override int GetHashCode();

-        public virtual KeyBuilder HasAlternateKey(params string[] propertyNames);

-        public virtual EntityTypeBuilder HasAnnotation(string annotation, object value);

-        public virtual EntityTypeBuilder HasBaseType(string name);

-        public virtual EntityTypeBuilder HasBaseType(Type entityType);

-        public virtual EntityTypeBuilder HasChangeTrackingStrategy(ChangeTrackingStrategy changeTrackingStrategy);

-        public virtual DataBuilder HasData(IEnumerable<object> data);

-        public virtual DataBuilder HasData(params object[] data);

-        public virtual IndexBuilder HasIndex(params string[] propertyNames);

-        public virtual KeyBuilder HasKey(params string[] propertyNames);

-        public virtual CollectionNavigationBuilder HasMany(string relatedTypeName, string navigationName = null);

-        public virtual CollectionNavigationBuilder HasMany(Type relatedType, string navigationName = null);

-        public virtual ReferenceNavigationBuilder HasOne(string relatedTypeName, string navigationName = null);

-        public virtual ReferenceNavigationBuilder HasOne(Type relatedType, string navigationName = null);

-        public virtual EntityTypeBuilder HasQueryFilter(LambdaExpression filter);

-        public virtual EntityTypeBuilder Ignore(string propertyName);

-        public virtual CollectionOwnershipBuilder OwnsMany(string ownedTypeName, string navigationName);

-        public virtual EntityTypeBuilder OwnsMany(string ownedTypeName, string navigationName, Action<CollectionOwnershipBuilder> buildAction);

-        public virtual CollectionOwnershipBuilder OwnsMany(Type ownedType, string navigationName);

-        public virtual EntityTypeBuilder OwnsMany(Type ownedType, string navigationName, Action<CollectionOwnershipBuilder> buildAction);

-        public virtual ReferenceOwnershipBuilder OwnsOne(string ownedTypeName, string navigationName);

-        public virtual EntityTypeBuilder OwnsOne(string ownedTypeName, string navigationName, Action<ReferenceOwnershipBuilder> buildAction);

-        public virtual ReferenceOwnershipBuilder OwnsOne(Type ownedType, string navigationName);

-        public virtual EntityTypeBuilder OwnsOne(Type ownedType, string navigationName, Action<ReferenceOwnershipBuilder> buildAction);

-        public virtual PropertyBuilder Property(string propertyName);

-        public virtual PropertyBuilder Property(Type propertyType, string propertyName);

-        public virtual PropertyBuilder<TProperty> Property<TProperty>(string propertyName);

-        public override string ToString();

-        public virtual EntityTypeBuilder UsePropertyAccessMode(PropertyAccessMode propertyAccessMode);

-    }
-    public class EntityTypeBuilder<TEntity> : EntityTypeBuilder where TEntity : class {
 {
-        public EntityTypeBuilder(InternalEntityTypeBuilder builder);

-        public virtual KeyBuilder HasAlternateKey(Expression<Func<TEntity, object>> keyExpression);

-        public virtual new EntityTypeBuilder<TEntity> HasAnnotation(string annotation, object value);

-        public virtual new EntityTypeBuilder<TEntity> HasBaseType(string name);

-        public virtual new EntityTypeBuilder<TEntity> HasBaseType(Type entityType);

-        public virtual EntityTypeBuilder<TEntity> HasBaseType<TBaseType>();

-        public virtual new EntityTypeBuilder<TEntity> HasChangeTrackingStrategy(ChangeTrackingStrategy changeTrackingStrategy);

-        public virtual new DataBuilder<TEntity> HasData(IEnumerable<object> data);

-        public virtual DataBuilder<TEntity> HasData(IEnumerable<TEntity> data);

-        public virtual new DataBuilder<TEntity> HasData(params object[] data);

-        public virtual DataBuilder<TEntity> HasData(params TEntity[] data);

-        public virtual IndexBuilder HasIndex(Expression<Func<TEntity, object>> indexExpression);

-        public virtual KeyBuilder HasKey(Expression<Func<TEntity, object>> keyExpression);

-        public virtual CollectionNavigationBuilder<TEntity, TRelatedEntity> HasMany<TRelatedEntity>(Expression<Func<TEntity, IEnumerable<TRelatedEntity>>> navigationExpression = null) where TRelatedEntity : class;

-        public virtual CollectionNavigationBuilder<TEntity, TRelatedEntity> HasMany<TRelatedEntity>(string navigationName) where TRelatedEntity : class;

-        public virtual ReferenceNavigationBuilder<TEntity, TRelatedEntity> HasOne<TRelatedEntity>(Expression<Func<TEntity, TRelatedEntity>> navigationExpression = null) where TRelatedEntity : class;

-        public virtual ReferenceNavigationBuilder<TEntity, TRelatedEntity> HasOne<TRelatedEntity>(string navigationName) where TRelatedEntity : class;

-        public virtual EntityTypeBuilder<TEntity> HasQueryFilter(Expression<Func<TEntity, bool>> filter);

-        public virtual EntityTypeBuilder<TEntity> Ignore(Expression<Func<TEntity, object>> propertyExpression);

-        public virtual new EntityTypeBuilder<TEntity> Ignore(string propertyName);

-        public virtual CollectionOwnershipBuilder<TEntity, TRelatedEntity> OwnsMany<TRelatedEntity>(Expression<Func<TEntity, IEnumerable<TRelatedEntity>>> navigationExpression) where TRelatedEntity : class;

-        public virtual EntityTypeBuilder<TEntity> OwnsMany<TRelatedEntity>(Expression<Func<TEntity, IEnumerable<TRelatedEntity>>> navigationExpression, Action<CollectionOwnershipBuilder<TEntity, TRelatedEntity>> buildAction) where TRelatedEntity : class;

-        public virtual CollectionOwnershipBuilder<TEntity, TRelatedEntity> OwnsMany<TRelatedEntity>(string navigationName) where TRelatedEntity : class;

-        public virtual EntityTypeBuilder<TEntity> OwnsMany<TRelatedEntity>(string navigationName, Action<CollectionOwnershipBuilder<TEntity, TRelatedEntity>> buildAction) where TRelatedEntity : class;

-        public virtual ReferenceOwnershipBuilder<TEntity, TRelatedEntity> OwnsOne<TRelatedEntity>(Expression<Func<TEntity, TRelatedEntity>> navigationExpression) where TRelatedEntity : class;

-        public virtual EntityTypeBuilder<TEntity> OwnsOne<TRelatedEntity>(Expression<Func<TEntity, TRelatedEntity>> navigationExpression, Action<ReferenceOwnershipBuilder<TEntity, TRelatedEntity>> buildAction) where TRelatedEntity : class;

-        public virtual ReferenceOwnershipBuilder<TEntity, TRelatedEntity> OwnsOne<TRelatedEntity>(string navigationName) where TRelatedEntity : class;

-        public virtual EntityTypeBuilder<TEntity> OwnsOne<TRelatedEntity>(string navigationName, Action<ReferenceOwnershipBuilder<TEntity, TRelatedEntity>> buildAction) where TRelatedEntity : class;

-        public virtual PropertyBuilder<TProperty> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);

-        public virtual new EntityTypeBuilder<TEntity> UsePropertyAccessMode(PropertyAccessMode propertyAccessMode);

-    }
-    public class IndexBuilder : IInfrastructure<IMutableModel>, IInfrastructure<InternalIndexBuilder> {
 {
-        public IndexBuilder(InternalIndexBuilder builder);

-        public virtual IMutableIndex Metadata { get; }

-        IMutableModel Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.IMutableModel>.Instance { get; }

-        InternalIndexBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.Internal.InternalIndexBuilder>.Instance { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public virtual IndexBuilder HasAnnotation(string annotation, object value);

-        public virtual IndexBuilder IsUnique(bool unique = true);

-        public override string ToString();

-    }
-    public class IndexBuilder<T> : IndexBuilder {
 {
-        public IndexBuilder(InternalIndexBuilder builder);

-        public virtual new IndexBuilder<T> HasAnnotation(string annotation, object value);

-        public virtual new IndexBuilder<T> IsUnique(bool unique = true);

-    }
-    public class KeyBuilder : IInfrastructure<IMutableModel>, IInfrastructure<InternalKeyBuilder> {
 {
-        public KeyBuilder(InternalKeyBuilder builder);

-        public virtual IMutableKey Metadata { get; }

-        IMutableModel Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.IMutableModel>.Instance { get; }

-        InternalKeyBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.Internal.InternalKeyBuilder>.Instance { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public virtual KeyBuilder HasAnnotation(string annotation, object value);

-        public override string ToString();

-    }
-    public class OwnedEntityTypeBuilder {
 {
-        public OwnedEntityTypeBuilder();

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public class OwnedEntityTypeBuilder<T> : OwnedEntityTypeBuilder {
 {
-        public OwnedEntityTypeBuilder();

-    }
-    public class PropertyBuilder : IInfrastructure<IMutableModel>, IInfrastructure<InternalPropertyBuilder> {
 {
-        public PropertyBuilder(InternalPropertyBuilder builder);

-        public virtual IMutableProperty Metadata { get; }

-        IMutableModel Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.IMutableModel>.Instance { get; }

-        InternalPropertyBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.Internal.InternalPropertyBuilder>.Instance { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public virtual PropertyBuilder HasAnnotation(string annotation, object value);

-        public virtual PropertyBuilder HasConversion(ValueConverter converter);

-        public virtual PropertyBuilder HasConversion(Type providerClrType);

-        public virtual PropertyBuilder HasConversion<TProvider>();

-        public virtual PropertyBuilder HasField(string fieldName);

-        public virtual PropertyBuilder HasMaxLength(int maxLength);

-        public virtual PropertyBuilder HasValueGenerator(Func<IProperty, IEntityType, ValueGenerator> factory);

-        public virtual PropertyBuilder HasValueGenerator(Type valueGeneratorType);

-        public virtual PropertyBuilder HasValueGenerator<TGenerator>() where TGenerator : ValueGenerator;

-        public virtual PropertyBuilder IsConcurrencyToken(bool concurrencyToken = true);

-        public virtual PropertyBuilder IsRequired(bool required = true);

-        public virtual PropertyBuilder IsRowVersion();

-        public virtual PropertyBuilder IsUnicode(bool unicode = true);

-        public override string ToString();

-        public virtual PropertyBuilder UsePropertyAccessMode(PropertyAccessMode propertyAccessMode);

-        public virtual PropertyBuilder ValueGeneratedNever();

-        public virtual PropertyBuilder ValueGeneratedOnAdd();

-        public virtual PropertyBuilder ValueGeneratedOnAddOrUpdate();

-        public virtual PropertyBuilder ValueGeneratedOnUpdate();

-    }
-    public class PropertyBuilder<TProperty> : PropertyBuilder {
 {
-        public PropertyBuilder(InternalPropertyBuilder builder);

-        public virtual new PropertyBuilder<TProperty> HasAnnotation(string annotation, object value);

-        public virtual new PropertyBuilder<TProperty> HasConversion(ValueConverter converter);

-        public virtual new PropertyBuilder<TProperty> HasConversion(Type providerClrType);

-        public virtual new PropertyBuilder<TProperty> HasConversion<TProvider>();

-        public virtual PropertyBuilder<TProperty> HasConversion<TProvider>(ValueConverter<TProperty, TProvider> converter);

-        public virtual PropertyBuilder<TProperty> HasConversion<TProvider>(Expression<Func<TProperty, TProvider>> convertToProviderExpression, Expression<Func<TProvider, TProperty>> convertFromProviderExpression);

-        public virtual new PropertyBuilder<TProperty> HasField(string fieldName);

-        public virtual new PropertyBuilder<TProperty> HasMaxLength(int maxLength);

-        public virtual new PropertyBuilder<TProperty> HasValueGenerator(Func<IProperty, IEntityType, ValueGenerator> factory);

-        public virtual new PropertyBuilder<TProperty> HasValueGenerator(Type valueGeneratorType);

-        public virtual new PropertyBuilder<TProperty> HasValueGenerator<TGenerator>() where TGenerator : ValueGenerator;

-        public virtual new PropertyBuilder<TProperty> IsConcurrencyToken(bool concurrencyToken = true);

-        public virtual new PropertyBuilder<TProperty> IsRequired(bool required = true);

-        public virtual new PropertyBuilder<TProperty> IsRowVersion();

-        public virtual new PropertyBuilder<TProperty> IsUnicode(bool unicode = true);

-        public virtual new PropertyBuilder<TProperty> UsePropertyAccessMode(PropertyAccessMode propertyAccessMode);

-        public virtual new PropertyBuilder<TProperty> ValueGeneratedNever();

-        public virtual new PropertyBuilder<TProperty> ValueGeneratedOnAdd();

-        public virtual new PropertyBuilder<TProperty> ValueGeneratedOnAddOrUpdate();

-        public virtual new PropertyBuilder<TProperty> ValueGeneratedOnUpdate();

-    }
-    public class QueryTypeBuilder : IInfrastructure<IMutableModel>, IInfrastructure<InternalEntityTypeBuilder> {
 {
-        public QueryTypeBuilder(InternalEntityTypeBuilder builder);

-        public virtual IMutableEntityType Metadata { get; }

-        IMutableModel Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.IMutableModel>.Instance { get; }

-        InternalEntityTypeBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.Internal.InternalEntityTypeBuilder>.Instance { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public virtual QueryTypeBuilder HasAnnotation(string annotation, object value);

-        public virtual QueryTypeBuilder HasBaseType(string name);

-        public virtual QueryTypeBuilder HasBaseType(Type queryType);

-        public virtual ReferenceNavigationBuilder HasOne(string relatedTypeName, string navigationName = null);

-        public virtual ReferenceNavigationBuilder HasOne(Type relatedType, string navigationName = null);

-        public virtual QueryTypeBuilder HasQueryFilter(LambdaExpression filter);

-        public virtual QueryTypeBuilder Ignore(string propertyName);

-        public virtual PropertyBuilder Property(string propertyName);

-        public virtual PropertyBuilder Property(Type propertyType, string propertyName);

-        public virtual PropertyBuilder<TProperty> Property<TProperty>(string propertyName);

-        public override string ToString();

-        public virtual QueryTypeBuilder UsePropertyAccessMode(PropertyAccessMode propertyAccessMode);

-    }
-    public class QueryTypeBuilder<TQuery> : QueryTypeBuilder where TQuery : class {
 {
-        public QueryTypeBuilder(InternalEntityTypeBuilder builder);

-        public virtual new QueryTypeBuilder<TQuery> HasAnnotation(string annotation, object value);

-        public virtual new QueryTypeBuilder<TQuery> HasBaseType(string name);

-        public virtual new QueryTypeBuilder<TQuery> HasBaseType(Type queryType);

-        public virtual QueryTypeBuilder<TQuery> HasBaseType<TBaseType>();

-        public virtual ReferenceNavigationBuilder<TQuery, TRelatedEntity> HasOne<TRelatedEntity>(Expression<Func<TQuery, TRelatedEntity>> navigationExpression = null) where TRelatedEntity : class;

-        public virtual QueryTypeBuilder<TQuery> HasQueryFilter(Expression<Func<TQuery, bool>> filter);

-        public virtual QueryTypeBuilder<TQuery> Ignore(Expression<Func<TQuery, object>> propertyExpression);

-        public virtual new QueryTypeBuilder<TQuery> Ignore(string propertyName);

-        public virtual PropertyBuilder<TProperty> Property<TProperty>(Expression<Func<TQuery, TProperty>> propertyExpression);

-        public virtual QueryTypeBuilder<TQuery> ToQuery(Expression<Func<IQueryable<TQuery>>> query);

-        public virtual new QueryTypeBuilder<TQuery> UsePropertyAccessMode(PropertyAccessMode propertyAccessMode);

-    }
-    public class ReferenceCollectionBuilder : ReferenceCollectionBuilderBase {
 {
-        public ReferenceCollectionBuilder(EntityType principalEntityType, EntityType dependentEntityType, InternalRelationshipBuilder builder);

-        protected ReferenceCollectionBuilder(InternalRelationshipBuilder builder, ReferenceCollectionBuilder oldBuilder, bool foreignKeySet = false, bool principalKeySet = false, bool requiredSet = false);

-        public virtual ReferenceCollectionBuilder HasAnnotation(string annotation, object value);

-        public virtual ReferenceCollectionBuilder HasForeignKey(params string[] foreignKeyPropertyNames);

-        protected virtual InternalRelationshipBuilder HasForeignKeyBuilder(IReadOnlyList<PropertyInfo> foreignKeyProperties);

-        protected virtual InternalRelationshipBuilder HasForeignKeyBuilder(IReadOnlyList<string> foreignKeyPropertyNames);

-        public virtual ReferenceCollectionBuilder HasPrincipalKey(params string[] keyPropertyNames);

-        protected virtual InternalRelationshipBuilder HasPrincipalKeyBuilder(IReadOnlyList<PropertyInfo> keyProperties);

-        protected virtual InternalRelationshipBuilder HasPrincipalKeyBuilder(IReadOnlyList<string> keyPropertyNames);

-        public virtual ReferenceCollectionBuilder IsRequired(bool required = true);

-        public virtual ReferenceCollectionBuilder OnDelete(DeleteBehavior deleteBehavior);

-    }
-    public class ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> : ReferenceCollectionBuilder where TPrincipalEntity : class where TDependentEntity : class {
 {
-        public ReferenceCollectionBuilder(EntityType principalEntityType, EntityType dependentEntityType, InternalRelationshipBuilder builder);

-        protected ReferenceCollectionBuilder(InternalRelationshipBuilder builder, ReferenceCollectionBuilder oldBuilder, bool foreignKeySet = false, bool principalKeySet = false, bool requiredSet = false);

-        public virtual new ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasAnnotation(string annotation, object value);

-        public virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasForeignKey(Expression<Func<TDependentEntity, object>> foreignKeyExpression);

-        public virtual new ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasForeignKey(params string[] foreignKeyPropertyNames);

-        public virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasPrincipalKey(Expression<Func<TPrincipalEntity, object>> keyExpression);

-        public virtual new ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasPrincipalKey(params string[] keyPropertyNames);

-        public virtual new ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> IsRequired(bool required = true);

-        public virtual new ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> OnDelete(DeleteBehavior deleteBehavior);

-    }
-    public class ReferenceCollectionBuilderBase : IInfrastructure<IMutableModel>, IInfrastructure<InternalRelationshipBuilder> {
 {
-        public ReferenceCollectionBuilderBase(EntityType principalEntityType, EntityType dependentEntityType, InternalRelationshipBuilder builder);

-        protected ReferenceCollectionBuilderBase(InternalRelationshipBuilder builder, ReferenceCollectionBuilderBase oldBuilder, bool foreignKeySet = false, bool principalKeySet = false, bool requiredSet = false);

-        protected virtual InternalRelationshipBuilder Builder { get; set; }

-        protected virtual EntityType DependentEntityType { get; }

-        public virtual IMutableForeignKey Metadata { get; }

-        IMutableModel Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.IMutableModel>.Instance { get; }

-        InternalRelationshipBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.Internal.InternalRelationshipBuilder>.Instance { get; }

-        protected virtual EntityType PrincipalEntityType { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public class ReferenceNavigationBuilder : IInfrastructure<InternalRelationshipBuilder> {
 {
-        public ReferenceNavigationBuilder(EntityType declaringEntityType, EntityType relatedEntityType, PropertyInfo navigationProperty, InternalRelationshipBuilder builder);

-        public ReferenceNavigationBuilder(EntityType declaringEntityType, EntityType relatedEntityType, string navigationName, InternalRelationshipBuilder builder);

-        protected virtual EntityType DeclaringEntityType { get; }

-        InternalRelationshipBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.Internal.InternalRelationshipBuilder>.Instance { get; }

-        protected virtual string ReferenceName { get; }

-        protected virtual PropertyInfo ReferenceProperty { get; }

-        protected virtual EntityType RelatedEntityType { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        public virtual ReferenceCollectionBuilder WithMany(string collection = null);

-        protected virtual InternalRelationshipBuilder WithManyBuilder(PropertyInfo navigationProperty);

-        protected virtual InternalRelationshipBuilder WithManyBuilder(string navigationName);

-        public virtual ReferenceReferenceBuilder WithOne(string reference = null);

-        protected virtual InternalRelationshipBuilder WithOneBuilder(PropertyInfo navigationProperty);

-        protected virtual InternalRelationshipBuilder WithOneBuilder(string navigationName);

-    }
-    public class ReferenceNavigationBuilder<TEntity, TRelatedEntity> : ReferenceNavigationBuilder where TEntity : class where TRelatedEntity : class {
 {
-        public ReferenceNavigationBuilder(EntityType declaringEntityType, EntityType relatedEntityType, PropertyInfo navigationProperty, InternalRelationshipBuilder builder);

-        public ReferenceNavigationBuilder(EntityType declaringEntityType, EntityType relatedEntityType, string navigationName, InternalRelationshipBuilder builder);

-        public virtual ReferenceCollectionBuilder<TRelatedEntity, TEntity> WithMany(Expression<Func<TRelatedEntity, IEnumerable<TEntity>>> navigationExpression);

-        public virtual new ReferenceCollectionBuilder<TRelatedEntity, TEntity> WithMany(string navigationName = null);

-        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> WithOne(Expression<Func<TRelatedEntity, TEntity>> navigationExpression);

-        public virtual new ReferenceReferenceBuilder<TEntity, TRelatedEntity> WithOne(string navigationName = null);

-    }
-    public class ReferenceOwnershipBuilder : ReferenceReferenceBuilderBase, IInfrastructure<InternalEntityTypeBuilder> {
 {
-        public ReferenceOwnershipBuilder(EntityType declaringEntityType, EntityType relatedEntityType, InternalRelationshipBuilder builder);

-        protected ReferenceOwnershipBuilder(InternalRelationshipBuilder builder, ReferenceOwnershipBuilder oldBuilder, bool inverted = false, bool foreignKeySet = false, bool principalKeySet = false, bool requiredSet = false);

-        InternalEntityTypeBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.Internal.InternalEntityTypeBuilder>.Instance { get; }

-        public virtual IMutableEntityType OwnedEntityType { get; }

-        protected virtual EntityType FindRelatedEntityType(string relatedTypeName, string navigationName);

-        protected virtual EntityType FindRelatedEntityType(Type relatedType, string navigationName);

-        public virtual ReferenceOwnershipBuilder HasChangeTrackingStrategy(ChangeTrackingStrategy changeTrackingStrategy);

-        public virtual DataBuilder HasData(params object[] data);

-        public virtual ReferenceOwnershipBuilder HasEntityTypeAnnotation(string annotation, object value);

-        public virtual ReferenceOwnershipBuilder HasForeignKey(params string[] foreignKeyPropertyNames);

-        public virtual ReferenceOwnershipBuilder HasForeignKeyAnnotation(string annotation, object value);

-        public virtual IndexBuilder HasIndex(params string[] propertyNames);

-        public virtual KeyBuilder HasKey(params string[] propertyNames);

-        public virtual CollectionNavigationBuilder HasMany(string relatedTypeName, string navigationName = null);

-        public virtual CollectionNavigationBuilder HasMany(Type relatedType, string navigationName = null);

-        public virtual ReferenceNavigationBuilder HasOne(string relatedTypeName, string navigationName = null);

-        public virtual ReferenceNavigationBuilder HasOne(Type relatedType, string navigationName = null);

-        public virtual ReferenceOwnershipBuilder HasPrincipalKey(params string[] keyPropertyNames);

-        public virtual ReferenceOwnershipBuilder Ignore(string propertyName);

-        public virtual ReferenceOwnershipBuilder OnDelete(DeleteBehavior deleteBehavior);

-        public virtual CollectionOwnershipBuilder OwnsMany(string ownedTypeName, string navigationName);

-        public virtual ReferenceOwnershipBuilder OwnsMany(string ownedTypeName, string navigationName, Action<CollectionOwnershipBuilder> buildAction);

-        public virtual CollectionOwnershipBuilder OwnsMany(Type ownedType, string navigationName);

-        public virtual ReferenceOwnershipBuilder OwnsMany(Type ownedType, string navigationName, Action<CollectionOwnershipBuilder> buildAction);

-        public virtual ReferenceOwnershipBuilder OwnsOne(string ownedTypeName, string navigationName);

-        public virtual ReferenceOwnershipBuilder OwnsOne(string ownedTypeName, string navigationName, Action<ReferenceOwnershipBuilder> buildAction);

-        public virtual ReferenceOwnershipBuilder OwnsOne(Type ownedType, string navigationName);

-        public virtual ReferenceOwnershipBuilder OwnsOne(Type ownedType, string navigationName, Action<ReferenceOwnershipBuilder> buildAction);

-        public virtual PropertyBuilder Property(string propertyName);

-        public virtual PropertyBuilder Property(Type propertyType, string propertyName);

-        public virtual PropertyBuilder<TProperty> Property<TProperty>(string propertyName);

-        public virtual ReferenceOwnershipBuilder UsePropertyAccessMode(PropertyAccessMode propertyAccessMode);

-    }
-    public class ReferenceOwnershipBuilder<TEntity, TRelatedEntity> : ReferenceOwnershipBuilder where TEntity : class where TRelatedEntity : class {
 {
-        public ReferenceOwnershipBuilder(EntityType declaringEntityType, EntityType relatedEntityType, InternalRelationshipBuilder builder);

-        protected ReferenceOwnershipBuilder(InternalRelationshipBuilder builder, ReferenceOwnershipBuilder oldBuilder, bool inverted = false, bool foreignKeySet = false, bool principalKeySet = false, bool requiredSet = false);

-        public virtual new ReferenceOwnershipBuilder<TEntity, TRelatedEntity> HasChangeTrackingStrategy(ChangeTrackingStrategy changeTrackingStrategy);

-        public virtual new DataBuilder<TRelatedEntity> HasData(params object[] data);

-        public virtual DataBuilder<TRelatedEntity> HasData(params TRelatedEntity[] data);

-        public virtual new ReferenceOwnershipBuilder<TEntity, TRelatedEntity> HasEntityTypeAnnotation(string annotation, object value);

-        public virtual ReferenceOwnershipBuilder<TEntity, TRelatedEntity> HasForeignKey(Expression<Func<TRelatedEntity, object>> foreignKeyExpression);

-        public virtual new ReferenceOwnershipBuilder<TEntity, TRelatedEntity> HasForeignKey(params string[] foreignKeyPropertyNames);

-        public virtual new ReferenceOwnershipBuilder<TEntity, TRelatedEntity> HasForeignKeyAnnotation(string annotation, object value);

-        public virtual IndexBuilder HasIndex(Expression<Func<TRelatedEntity, object>> indexExpression);

-        public virtual KeyBuilder HasKey(Expression<Func<TRelatedEntity, object>> keyExpression);

-        public virtual CollectionNavigationBuilder<TRelatedEntity, TNewRelatedEntity> HasMany<TNewRelatedEntity>(Expression<Func<TRelatedEntity, IEnumerable<TNewRelatedEntity>>> navigationExpression = null) where TNewRelatedEntity : class;

-        public virtual ReferenceNavigationBuilder<TRelatedEntity, TNewRelatedEntity> HasOne<TNewRelatedEntity>(Expression<Func<TRelatedEntity, TNewRelatedEntity>> navigationExpression = null) where TNewRelatedEntity : class;

-        public virtual ReferenceNavigationBuilder<TRelatedEntity, TNewRelatedEntity> HasOne<TNewRelatedEntity>(string navigationName) where TNewRelatedEntity : class;

-        public virtual ReferenceOwnershipBuilder<TEntity, TRelatedEntity> HasPrincipalKey(Expression<Func<TEntity, object>> keyExpression);

-        public virtual new ReferenceOwnershipBuilder<TEntity, TRelatedEntity> HasPrincipalKey(params string[] keyPropertyNames);

-        public virtual ReferenceOwnershipBuilder<TEntity, TRelatedEntity> Ignore(Expression<Func<TRelatedEntity, object>> propertyExpression);

-        public virtual new ReferenceOwnershipBuilder<TEntity, TRelatedEntity> Ignore(string propertyName);

-        public virtual new ReferenceOwnershipBuilder<TEntity, TRelatedEntity> OnDelete(DeleteBehavior deleteBehavior);

-        public virtual CollectionOwnershipBuilder<TRelatedEntity, TDependentEntity> OwnsMany<TDependentEntity>(Expression<Func<TRelatedEntity, IEnumerable<TDependentEntity>>> navigationExpression) where TDependentEntity : class;

-        public virtual ReferenceOwnershipBuilder<TEntity, TRelatedEntity> OwnsMany<TDependentEntity>(Expression<Func<TRelatedEntity, IEnumerable<TDependentEntity>>> navigationExpression, Action<CollectionOwnershipBuilder<TRelatedEntity, TDependentEntity>> buildAction) where TDependentEntity : class;

-        public virtual CollectionOwnershipBuilder<TRelatedEntity, TDependentEntity> OwnsMany<TDependentEntity>(string navigationName) where TDependentEntity : class;

-        public virtual ReferenceOwnershipBuilder<TEntity, TRelatedEntity> OwnsMany<TDependentEntity>(string navigationName, Action<CollectionOwnershipBuilder<TRelatedEntity, TDependentEntity>> buildAction) where TDependentEntity : class;

-        public virtual ReferenceOwnershipBuilder<TRelatedEntity, TNewRelatedEntity> OwnsOne<TNewRelatedEntity>(Expression<Func<TRelatedEntity, TNewRelatedEntity>> navigationExpression) where TNewRelatedEntity : class;

-        public virtual ReferenceOwnershipBuilder<TEntity, TRelatedEntity> OwnsOne<TNewRelatedEntity>(Expression<Func<TRelatedEntity, TNewRelatedEntity>> navigationExpression, Action<ReferenceOwnershipBuilder<TRelatedEntity, TNewRelatedEntity>> buildAction) where TNewRelatedEntity : class;

-        public virtual ReferenceOwnershipBuilder<TRelatedEntity, TNewRelatedEntity> OwnsOne<TNewRelatedEntity>(string navigationName) where TNewRelatedEntity : class;

-        public virtual ReferenceOwnershipBuilder<TEntity, TRelatedEntity> OwnsOne<TNewRelatedEntity>(string navigationName, Action<ReferenceOwnershipBuilder<TRelatedEntity, TNewRelatedEntity>> buildAction) where TNewRelatedEntity : class;

-        public virtual PropertyBuilder<TProperty> Property<TProperty>(Expression<Func<TRelatedEntity, TProperty>> propertyExpression);

-        public virtual new ReferenceOwnershipBuilder<TEntity, TRelatedEntity> UsePropertyAccessMode(PropertyAccessMode propertyAccessMode);

-    }
-    public class ReferenceReferenceBuilder : ReferenceReferenceBuilderBase {
 {
-        public ReferenceReferenceBuilder(EntityType declaringEntityType, EntityType relatedEntityType, InternalRelationshipBuilder builder);

-        protected ReferenceReferenceBuilder(InternalRelationshipBuilder builder, ReferenceReferenceBuilder oldBuilder, bool inverted = false, bool foreignKeySet = false, bool principalKeySet = false, bool requiredSet = false);

-        public virtual ReferenceReferenceBuilder HasAnnotation(string annotation, object value);

-        public virtual ReferenceReferenceBuilder HasForeignKey(string dependentEntityTypeName, params string[] foreignKeyPropertyNames);

-        public virtual ReferenceReferenceBuilder HasForeignKey(Type dependentEntityType, params string[] foreignKeyPropertyNames);

-        protected virtual InternalRelationshipBuilder HasForeignKeyBuilder(EntityType dependentEntityType, string dependentEntityTypeName, IReadOnlyList<PropertyInfo> foreignKeyProperties);

-        protected virtual InternalRelationshipBuilder HasForeignKeyBuilder(EntityType dependentEntityType, string dependentEntityTypeName, IReadOnlyList<string> foreignKeyPropertyNames);

-        public virtual ReferenceReferenceBuilder HasPrincipalKey(string principalEntityTypeName, params string[] keyPropertyNames);

-        public virtual ReferenceReferenceBuilder HasPrincipalKey(Type principalEntityType, params string[] keyPropertyNames);

-        protected virtual InternalRelationshipBuilder HasPrincipalKeyBuilder(EntityType principalEntityType, string principalEntityTypeName, IReadOnlyList<PropertyInfo> foreignKeyProperties);

-        protected virtual InternalRelationshipBuilder HasPrincipalKeyBuilder(EntityType principalEntityType, string principalEntityTypeName, IReadOnlyList<string> foreignKeyPropertyNames);

-        public virtual ReferenceReferenceBuilder IsRequired(bool required = true);

-        public virtual ReferenceReferenceBuilder OnDelete(DeleteBehavior deleteBehavior);

-        protected virtual EntityType ResolveEntityType(string entityTypeName);

-        protected virtual EntityType ResolveEntityType(Type entityType);

-    }
-    public class ReferenceReferenceBuilder<TEntity, TRelatedEntity> : ReferenceReferenceBuilder where TEntity : class where TRelatedEntity : class {
 {
-        public ReferenceReferenceBuilder(EntityType declaringEntityType, EntityType relatedEntityType, InternalRelationshipBuilder builder);

-        protected ReferenceReferenceBuilder(InternalRelationshipBuilder builder, ReferenceReferenceBuilder oldBuilder, bool inverted = false, bool foreignKeySet = false, bool principalKeySet = false, bool requiredSet = false);

-        public virtual new ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasAnnotation(string annotation, object value);

-        public virtual new ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasForeignKey(string dependentEntityTypeName, params string[] foreignKeyPropertyNames);

-        public virtual new ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasForeignKey(Type dependentEntityType, params string[] foreignKeyPropertyNames);

-        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasForeignKey<TDependentEntity>(Expression<Func<TDependentEntity, object>> foreignKeyExpression) where TDependentEntity : class;

-        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasForeignKey<TDependentEntity>(params string[] foreignKeyPropertyNames) where TDependentEntity : class;

-        public virtual new ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasPrincipalKey(string principalEntityTypeName, params string[] keyPropertyNames);

-        public virtual new ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasPrincipalKey(Type principalEntityType, params string[] keyPropertyNames);

-        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasPrincipalKey<TPrincipalEntity>(Expression<Func<TPrincipalEntity, object>> keyExpression) where TPrincipalEntity : class;

-        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasPrincipalKey<TPrincipalEntity>(params string[] keyPropertyNames) where TPrincipalEntity : class;

-        public virtual new ReferenceReferenceBuilder<TEntity, TRelatedEntity> IsRequired(bool required = true);

-        public virtual new ReferenceReferenceBuilder<TEntity, TRelatedEntity> OnDelete(DeleteBehavior deleteBehavior);

-    }
-    public class ReferenceReferenceBuilderBase : IInfrastructure<IMutableModel>, IInfrastructure<InternalRelationshipBuilder> {
 {
-        public ReferenceReferenceBuilderBase(EntityType declaringEntityType, EntityType relatedEntityType, InternalRelationshipBuilder builder);

-        protected ReferenceReferenceBuilderBase(InternalRelationshipBuilder builder, ReferenceReferenceBuilderBase oldBuilder, bool inverted = false, bool foreignKeySet = false, bool principalKeySet = false, bool requiredSet = false);

-        protected virtual InternalRelationshipBuilder Builder { get; set; }

-        protected virtual EntityType DeclaringEntityType { get; }

-        public virtual IMutableForeignKey Metadata { get; }

-        IMutableModel Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.IMutableModel>.Instance { get; }

-        InternalRelationshipBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.Internal.InternalRelationshipBuilder>.Instance { get; }

-        protected virtual EntityType RelatedEntityType { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-}
```

