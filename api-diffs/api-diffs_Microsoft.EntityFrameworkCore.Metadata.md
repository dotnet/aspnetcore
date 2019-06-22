# Microsoft.EntityFrameworkCore.Metadata

``` diff
-namespace Microsoft.EntityFrameworkCore.Metadata {
 {
-    public interface IDbFunction {
 {
-        string FunctionName { get; }

-        MethodInfo MethodInfo { get; }

-        string Schema { get; }

-        Func<IReadOnlyCollection<Expression>, Expression> Translation { get; }

-    }
-    public interface IEntityType : IAnnotatable, ITypeBase {
 {
-        IEntityType BaseType { get; }

-        IEntityType DefiningEntityType { get; }

-        string DefiningNavigationName { get; }

-        LambdaExpression DefiningQuery { get; }

-        bool IsQueryType { get; }

-        LambdaExpression QueryFilter { get; }

-        IForeignKey FindForeignKey(IReadOnlyList<IProperty> properties, IKey principalKey, IEntityType principalEntityType);

-        IIndex FindIndex(IReadOnlyList<IProperty> properties);

-        IKey FindKey(IReadOnlyList<IProperty> properties);

-        IKey FindPrimaryKey();

-        IProperty FindProperty(string name);

-        IServiceProperty FindServiceProperty(string name);

-        IEnumerable<IForeignKey> GetForeignKeys();

-        IEnumerable<IIndex> GetIndexes();

-        IEnumerable<IKey> GetKeys();

-        IEnumerable<IProperty> GetProperties();

-        IEnumerable<IServiceProperty> GetServiceProperties();

-    }
-    public interface IForeignKey : IAnnotatable {
 {
-        IEntityType DeclaringEntityType { get; }

-        DeleteBehavior DeleteBehavior { get; }

-        INavigation DependentToPrincipal { get; }

-        bool IsOwnership { get; }

-        bool IsRequired { get; }

-        bool IsUnique { get; }

-        IEntityType PrincipalEntityType { get; }

-        IKey PrincipalKey { get; }

-        INavigation PrincipalToDependent { get; }

-        IReadOnlyList<IProperty> Properties { get; }

-    }
-    public interface IIndex : IAnnotatable {
 {
-        IEntityType DeclaringEntityType { get; }

-        bool IsUnique { get; }

-        IReadOnlyList<IProperty> Properties { get; }

-    }
-    public interface IKey : IAnnotatable {
 {
-        IEntityType DeclaringEntityType { get; }

-        IReadOnlyList<IProperty> Properties { get; }

-    }
-    public interface IModel : IAnnotatable {
 {
-        IEntityType FindEntityType(string name);

-        IEntityType FindEntityType(string name, string definingNavigationName, IEntityType definingEntityType);

-        IEnumerable<IEntityType> GetEntityTypes();

-    }
-    public interface IMutableAnnotatable : IAnnotatable {
 {
-        new object this[string name] { get; set; }

-        Annotation AddAnnotation(string name, object value);

-        new Annotation FindAnnotation(string name);

-        new IEnumerable<Annotation> GetAnnotations();

-        Annotation RemoveAnnotation(string name);

-        void SetAnnotation(string name, object value);

-    }
-    public interface IMutableDbFunction : IDbFunction {
 {
-        new string FunctionName { get; set; }

-        new string Schema { get; set; }

-        new Func<IReadOnlyCollection<Expression>, Expression> Translation { get; set; }

-    }
-    public interface IMutableEntityType : IAnnotatable, IEntityType, IMutableAnnotatable, IMutableTypeBase, ITypeBase {
 {
-        new IMutableEntityType BaseType { get; set; }

-        new LambdaExpression DefiningQuery { get; set; }

-        new bool IsQueryType { get; set; }

-        new IMutableModel Model { get; }

-        new LambdaExpression QueryFilter { get; set; }

-        IMutableForeignKey AddForeignKey(IReadOnlyList<IMutableProperty> properties, IMutableKey principalKey, IMutableEntityType principalEntityType);

-        IMutableIndex AddIndex(IReadOnlyList<IMutableProperty> properties);

-        IMutableKey AddKey(IReadOnlyList<IMutableProperty> properties);

-        IMutableProperty AddProperty(string name, Type propertyType);

-        IMutableServiceProperty AddServiceProperty(MemberInfo memberInfo);

-        new IMutableForeignKey FindForeignKey(IReadOnlyList<IProperty> properties, IKey principalKey, IEntityType principalEntityType);

-        new IMutableIndex FindIndex(IReadOnlyList<IProperty> properties);

-        new IMutableKey FindKey(IReadOnlyList<IProperty> properties);

-        new IMutableKey FindPrimaryKey();

-        new IMutableProperty FindProperty(string name);

-        new IMutableServiceProperty FindServiceProperty(string name);

-        new IEnumerable<IMutableForeignKey> GetForeignKeys();

-        new IEnumerable<IMutableIndex> GetIndexes();

-        new IEnumerable<IMutableKey> GetKeys();

-        new IEnumerable<IMutableProperty> GetProperties();

-        new IEnumerable<IMutableServiceProperty> GetServiceProperties();

-        IMutableForeignKey RemoveForeignKey(IReadOnlyList<IProperty> properties, IKey principalKey, IEntityType principalEntityType);

-        IMutableIndex RemoveIndex(IReadOnlyList<IProperty> properties);

-        IMutableKey RemoveKey(IReadOnlyList<IProperty> properties);

-        IMutableProperty RemoveProperty(string name);

-        IMutableServiceProperty RemoveServiceProperty(string name);

-        IMutableKey SetPrimaryKey(IReadOnlyList<IMutableProperty> properties);

-    }
-    public interface IMutableForeignKey : IAnnotatable, IForeignKey, IMutableAnnotatable {
 {
-        new IMutableEntityType DeclaringEntityType { get; }

-        new DeleteBehavior DeleteBehavior { get; set; }

-        new IMutableNavigation DependentToPrincipal { get; }

-        new bool IsOwnership { get; set; }

-        new bool IsRequired { get; set; }

-        new bool IsUnique { get; set; }

-        new IMutableEntityType PrincipalEntityType { get; }

-        new IMutableKey PrincipalKey { get; }

-        new IMutableNavigation PrincipalToDependent { get; }

-        new IReadOnlyList<IMutableProperty> Properties { get; }

-        IMutableNavigation HasDependentToPrincipal(PropertyInfo property);

-        IMutableNavigation HasDependentToPrincipal(string name);

-        IMutableNavigation HasPrincipalToDependent(PropertyInfo property);

-        IMutableNavigation HasPrincipalToDependent(string name);

-    }
-    public interface IMutableIndex : IAnnotatable, IIndex, IMutableAnnotatable {
 {
-        new IMutableEntityType DeclaringEntityType { get; }

-        new bool IsUnique { get; set; }

-        new IReadOnlyList<IMutableProperty> Properties { get; }

-    }
-    public interface IMutableKey : IAnnotatable, IKey, IMutableAnnotatable {
 {
-        new IMutableEntityType DeclaringEntityType { get; }

-        new IReadOnlyList<IMutableProperty> Properties { get; }

-    }
-    public interface IMutableModel : IAnnotatable, IModel, IMutableAnnotatable {
 {
-        IMutableEntityType AddEntityType(string name);

-        IMutableEntityType AddEntityType(string name, string definingNavigationName, IMutableEntityType definingEntityType);

-        IMutableEntityType AddEntityType(Type clrType);

-        IMutableEntityType AddEntityType(Type clrType, string definingNavigationName, IMutableEntityType definingEntityType);

-        IMutableEntityType AddQueryType(Type type);

-        new IMutableEntityType FindEntityType(string name);

-        IMutableEntityType FindEntityType(string name, string definingNavigationName, IMutableEntityType definingEntityType);

-        new IEnumerable<IMutableEntityType> GetEntityTypes();

-        IMutableEntityType RemoveEntityType(string name);

-        IMutableEntityType RemoveEntityType(string name, string definingNavigationName, IMutableEntityType definingEntityType);

-    }
-    public interface IMutableNavigation : IAnnotatable, IMutableAnnotatable, IMutablePropertyBase, INavigation, IPropertyBase {
 {
-        new IMutableEntityType DeclaringEntityType { get; }

-        new IMutableForeignKey ForeignKey { get; }

-        new bool IsEagerLoaded { get; set; }

-    }
-    public interface IMutableProperty : IAnnotatable, IMutableAnnotatable, IMutablePropertyBase, IProperty, IPropertyBase {
 {
-        new PropertySaveBehavior AfterSaveBehavior { get; set; }

-        new PropertySaveBehavior BeforeSaveBehavior { get; set; }

-        new IMutableEntityType DeclaringEntityType { get; }

-        new bool IsConcurrencyToken { get; set; }

-        new bool IsNullable { get; set; }

-        new bool IsReadOnlyAfterSave { get; set; }

-        new bool IsReadOnlyBeforeSave { get; set; }

-        new bool IsStoreGeneratedAlways { get; set; }

-        new ValueGenerated ValueGenerated { get; set; }

-    }
-    public interface IMutablePropertyBase : IAnnotatable, IMutableAnnotatable, IPropertyBase {
 {
-        new IMutableTypeBase DeclaringType { get; }

-    }
-    public interface IMutableSequence : ISequence {
 {
-        new Type ClrType { get; set; }

-        new int IncrementBy { get; set; }

-        new bool IsCyclic { get; set; }

-        new Nullable<long> MaxValue { get; set; }

-        new Nullable<long> MinValue { get; set; }

-        new long StartValue { get; set; }

-    }
-    public interface IMutableServiceProperty : IAnnotatable, IMutableAnnotatable, IMutablePropertyBase, IPropertyBase, IServiceProperty {
 {
-        new IMutableEntityType DeclaringEntityType { get; }

-    }
-    public interface IMutableTypeBase : IAnnotatable, IMutableAnnotatable, ITypeBase {
 {
-        new IMutableModel Model { get; }

-    }
-    public interface INavigation : IAnnotatable, IPropertyBase {
 {
-        IEntityType DeclaringEntityType { get; }

-        IForeignKey ForeignKey { get; }

-        bool IsEagerLoaded { get; }

-    }
-    public interface IProperty : IAnnotatable, IPropertyBase {
 {
-        PropertySaveBehavior AfterSaveBehavior { get; }

-        PropertySaveBehavior BeforeSaveBehavior { get; }

-        new Type ClrType { get; }

-        IEntityType DeclaringEntityType { get; }

-        bool IsConcurrencyToken { get; }

-        bool IsNullable { get; }

-        bool IsReadOnlyAfterSave { get; }

-        bool IsReadOnlyBeforeSave { get; }

-        new bool IsShadowProperty { get; }

-        bool IsStoreGeneratedAlways { get; }

-        ValueGenerated ValueGenerated { get; }

-    }
-    public interface IPropertyBase : IAnnotatable {
 {
-        Type ClrType { get; }

-        ITypeBase DeclaringType { get; }

-        FieldInfo FieldInfo { get; }

-        bool IsShadowProperty { get; }

-        string Name { get; }

-        PropertyInfo PropertyInfo { get; }

-    }
-    public interface IRelationalEntityTypeAnnotations {
 {
-        IProperty DiscriminatorProperty { get; }

-        object DiscriminatorValue { get; }

-        string Schema { get; }

-        string TableName { get; }

-    }
-    public interface IRelationalForeignKeyAnnotations {
 {
-        string Name { get; }

-    }
-    public interface IRelationalIndexAnnotations {
 {
-        string Filter { get; }

-        string Name { get; }

-    }
-    public interface IRelationalKeyAnnotations {
 {
-        string Name { get; }

-    }
-    public interface IRelationalModelAnnotations {
 {
-        IReadOnlyList<IDbFunction> DbFunctions { get; }

-        string DefaultSchema { get; }

-        IReadOnlyList<ISequence> Sequences { get; }

-        IDbFunction FindDbFunction(MethodInfo method);

-        ISequence FindSequence(string name, string schema = null);

-    }
-    public interface IRelationalPropertyAnnotations {
 {
-        string ColumnName { get; }

-        string ColumnType { get; }

-        string ComputedColumnSql { get; }

-        object DefaultValue { get; }

-        string DefaultValueSql { get; }

-        bool IsFixedLength { get; }

-    }
-    public interface ISequence {
 {
-        Type ClrType { get; }

-        int IncrementBy { get; }

-        bool IsCyclic { get; }

-        Nullable<long> MaxValue { get; }

-        Nullable<long> MinValue { get; }

-        IModel Model { get; }

-        string Name { get; }

-        string Schema { get; }

-        long StartValue { get; }

-    }
-    public interface IServiceProperty : IAnnotatable, IPropertyBase {
 {
-        IEntityType DeclaringEntityType { get; }

-    }
-    public interface ISqlServerEntityTypeAnnotations : IRelationalEntityTypeAnnotations {
 {
-        bool IsMemoryOptimized { get; }

-    }
-    public interface ISqlServerIndexAnnotations : IRelationalIndexAnnotations {
 {
-        IReadOnlyList<string> IncludeProperties { get; }

-        Nullable<bool> IsClustered { get; }

-    }
-    public interface ISqlServerKeyAnnotations : IRelationalKeyAnnotations {
 {
-        Nullable<bool> IsClustered { get; }

-    }
-    public interface ISqlServerModelAnnotations : IRelationalModelAnnotations {
 {
-        string HiLoSequenceName { get; }

-        string HiLoSequenceSchema { get; }

-        Nullable<SqlServerValueGenerationStrategy> ValueGenerationStrategy { get; }

-    }
-    public interface ISqlServerPropertyAnnotations : IRelationalPropertyAnnotations {
 {
-        string HiLoSequenceName { get; }

-        string HiLoSequenceSchema { get; }

-        Nullable<SqlServerValueGenerationStrategy> ValueGenerationStrategy { get; }

-        ISequence FindHiLoSequence();

-    }
-    public interface ITypeBase : IAnnotatable {
 {
-        Type ClrType { get; }

-        IModel Model { get; }

-        string Name { get; }

-    }
-    public enum PropertySaveBehavior {
 {
-        Ignore = 1,

-        Save = 0,

-        Throw = 2,

-    }
-    public static class RelationalAnnotationNames {
 {
-        public const string ColumnName = "Relational:ColumnName";

-        public const string ColumnType = "Relational:ColumnType";

-        public const string ComputedColumnSql = "Relational:ComputedColumnSql";

-        public const string DbFunction = "Relational:DbFunction";

-        public const string DefaultSchema = "Relational:DefaultSchema";

-        public const string DefaultValue = "Relational:DefaultValue";

-        public const string DefaultValueSql = "Relational:DefaultValueSql";

-        public const string DiscriminatorProperty = "Relational:DiscriminatorProperty";

-        public const string DiscriminatorValue = "Relational:DiscriminatorValue";

-        public const string Filter = "Relational:Filter";

-        public const string IsFixedLength = "Relational:IsFixedLength";

-        public const string MaxIdentifierLength = "Relational:MaxIdentifierLength";

-        public const string Name = "Relational:Name";

-        public const string Prefix = "Relational:";

-        public const string Schema = "Relational:Schema";

-        public const string SequencePrefix = "Relational:Sequence:";

-        public const string TableName = "Relational:TableName";

-        public const string TypeMapping = "Relational:TypeMapping";

-    }
-    public class RelationalAnnotations {
 {
-        public RelationalAnnotations(IAnnotatable metadata);

-        public virtual IAnnotatable Metadata { get; }

-        public virtual bool CanSetAnnotation(string relationalAnnotationName, object value);

-        public virtual bool RemoveAnnotation(string annotationName);

-        public virtual bool SetAnnotation(string annotationName, object value);

-    }
-    public class RelationalEntityTypeAnnotations : IRelationalEntityTypeAnnotations {
 {
-        public RelationalEntityTypeAnnotations(IEntityType entityType);

-        protected RelationalEntityTypeAnnotations(RelationalAnnotations annotations);

-        protected virtual RelationalAnnotations Annotations { get; }

-        public virtual IProperty DiscriminatorProperty { get; set; }

-        public virtual object DiscriminatorValue { get; set; }

-        protected virtual IEntityType EntityType { get; }

-        public virtual string Schema { get; set; }

-        public virtual string TableName { get; set; }

-        protected virtual RelationalEntityTypeAnnotations GetAnnotations(IEntityType entityType);

-        protected virtual RelationalModelAnnotations GetAnnotations(IModel model);

-        protected virtual Nullable<ConfigurationSource> GetDiscriminatorPropertyConfigurationSource();

-        protected virtual Nullable<ConfigurationSource> GetDiscriminatorValueConfigurationSource();

-        protected virtual IProperty GetNonRootDiscriminatorProperty();

-        protected virtual bool RemoveDiscriminatorValue();

-        protected virtual bool SetDiscriminatorProperty(IProperty value);

-        protected virtual bool SetDiscriminatorProperty(IProperty value, Type oldDiscriminatorType);

-        protected virtual bool SetDiscriminatorValue(object value);

-        protected virtual bool SetSchema(string value);

-        protected virtual bool SetTableName(string value);

-    }
-    public class RelationalForeignKeyAnnotations : IRelationalForeignKeyAnnotations {
 {
-        public RelationalForeignKeyAnnotations(IForeignKey foreignKey);

-        protected RelationalForeignKeyAnnotations(RelationalAnnotations annotations);

-        protected virtual RelationalAnnotations Annotations { get; }

-        protected virtual IForeignKey ForeignKey { get; }

-        public virtual string Name { get; set; }

-        protected virtual bool SetName(string value);

-    }
-    public class RelationalIndexAnnotations : IRelationalIndexAnnotations {
 {
-        public RelationalIndexAnnotations(IIndex index);

-        protected RelationalIndexAnnotations(RelationalAnnotations annotations);

-        protected virtual RelationalAnnotations Annotations { get; }

-        public virtual string Filter { get; set; }

-        protected virtual IIndex Index { get; }

-        public virtual string Name { get; set; }

-        protected virtual bool SetFilter(string value);

-        protected virtual bool SetName(string value);

-    }
-    public class RelationalKeyAnnotations : IRelationalKeyAnnotations {
 {
-        public RelationalKeyAnnotations(IKey key);

-        protected RelationalKeyAnnotations(RelationalAnnotations annotations);

-        protected virtual RelationalAnnotations Annotations { get; }

-        protected virtual IKey Key { get; }

-        public virtual string Name { get; set; }

-        protected virtual bool SetName(string value);

-    }
-    public class RelationalModelAnnotations : IRelationalModelAnnotations {
 {
-        public RelationalModelAnnotations(IModel model);

-        protected RelationalModelAnnotations(RelationalAnnotations annotations);

-        protected virtual RelationalAnnotations Annotations { get; }

-        public virtual IReadOnlyList<IDbFunction> DbFunctions { get; }

-        public virtual string DefaultSchema { get; set; }

-        public virtual int MaxIdentifierLength { get; set; }

-        IReadOnlyList<ISequence> Microsoft.EntityFrameworkCore.Metadata.IRelationalModelAnnotations.Sequences { get; }

-        protected virtual IModel Model { get; }

-        public virtual IReadOnlyList<IMutableSequence> Sequences { get; }

-        public virtual IDbFunction FindDbFunction(MethodInfo methodInfo);

-        public virtual IMutableSequence FindSequence(string name, string schema = null);

-        public virtual DbFunction GetOrAddDbFunction(MethodInfo methodInfo);

-        public virtual IMutableSequence GetOrAddSequence(string name, string schema = null);

-        ISequence Microsoft.EntityFrameworkCore.Metadata.IRelationalModelAnnotations.FindSequence(string name, string schema);

-        protected virtual bool SetDefaultSchema(string value);

-        protected virtual bool SetMaxIdentifierLength(Nullable<int> value);

-    }
-    public class RelationalPropertyAnnotations : IRelationalPropertyAnnotations {
 {
-        public RelationalPropertyAnnotations(IProperty property);

-        protected RelationalPropertyAnnotations(RelationalAnnotations annotations);

-        protected virtual RelationalAnnotations Annotations { get; }

-        public virtual string ColumnName { get; set; }

-        public virtual string ColumnType { get; set; }

-        public virtual string ComputedColumnSql { get; set; }

-        public virtual object DefaultValue { get; set; }

-        public virtual string DefaultValueSql { get; set; }

-        public virtual bool IsFixedLength { get; set; }

-        protected virtual IProperty Property { get; }

-        protected virtual bool ShouldThrowOnConflict { get; }

-        protected virtual bool ShouldThrowOnInvalidConfiguration { get; }

-        protected virtual bool CanSetComputedColumnSql(string value);

-        protected virtual bool CanSetDefaultValue(object value);

-        protected virtual bool CanSetDefaultValueSql(string value);

-        protected virtual void ClearAllServerGeneratedValues();

-        protected virtual RelationalEntityTypeAnnotations GetAnnotations(IEntityType entityType);

-        protected virtual RelationalPropertyAnnotations GetAnnotations(IProperty property);

-        protected virtual string GetComputedColumnSql(bool fallback);

-        protected virtual object GetDefaultValue(bool fallback);

-        protected virtual string GetDefaultValueSql(bool fallback);

-        protected virtual bool SetColumnName(string value);

-        protected virtual bool SetColumnType(string value);

-        protected virtual bool SetComputedColumnSql(string value);

-        protected virtual bool SetDefaultValue(object value);

-        protected virtual bool SetDefaultValueSql(string value);

-        protected virtual bool SetFixedLength(bool fixedLength);

-    }
-    public static class RelationalPropertyExtensions {
 {
-        public static bool IsColumnNullable(this IProperty property);

-    }
-    public class SequenceBuilder {
 {
-        public SequenceBuilder(IMutableSequence sequence);

-        public virtual IMutableSequence Metadata { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public virtual SequenceBuilder HasMax(long maximum);

-        public virtual SequenceBuilder HasMin(long minimum);

-        public virtual SequenceBuilder IncrementsBy(int increment);

-        public virtual SequenceBuilder IsCyclic(bool cyclic = true);

-        public virtual SequenceBuilder StartsAt(long startValue);

-        public override string ToString();

-    }
-    public class SimpleModelFactory {
 {
-        public SimpleModelFactory();

-        public virtual IMutableModel Create();

-    }
-    public class SqlServerEntityTypeAnnotations : RelationalEntityTypeAnnotations, IRelationalEntityTypeAnnotations, ISqlServerEntityTypeAnnotations {
 {
-        public SqlServerEntityTypeAnnotations(IEntityType entityType);

-        public SqlServerEntityTypeAnnotations(RelationalAnnotations annotations);

-        public virtual bool IsMemoryOptimized { get; set; }

-        protected virtual bool SetIsMemoryOptimized(bool value);

-    }
-    public class SqlServerIndexAnnotations : RelationalIndexAnnotations, IRelationalIndexAnnotations, ISqlServerIndexAnnotations {
 {
-        public SqlServerIndexAnnotations(IIndex index);

-        protected SqlServerIndexAnnotations(RelationalAnnotations annotations);

-        public virtual IReadOnlyList<string> IncludeProperties { get; set; }

-        public virtual Nullable<bool> IsClustered { get; set; }

-        protected virtual bool SetInclude(IReadOnlyList<string> properties);

-        protected virtual bool SetIsClustered(Nullable<bool> value);

-    }
-    public class SqlServerKeyAnnotations : RelationalKeyAnnotations, IRelationalKeyAnnotations, ISqlServerKeyAnnotations {
 {
-        public SqlServerKeyAnnotations(IKey key);

-        protected SqlServerKeyAnnotations(RelationalAnnotations annotations);

-        public virtual Nullable<bool> IsClustered { get; set; }

-        protected virtual bool SetIsClustered(Nullable<bool> value);

-    }
-    public class SqlServerModelAnnotations : RelationalModelAnnotations, IRelationalModelAnnotations, ISqlServerModelAnnotations {
 {
-        public const string DefaultHiLoSequenceName = "EntityFrameworkHiLoSequence";

-        public SqlServerModelAnnotations(IModel model);

-        protected SqlServerModelAnnotations(RelationalAnnotations annotations);

-        public virtual string HiLoSequenceName { get; set; }

-        public virtual string HiLoSequenceSchema { get; set; }

-        public virtual Nullable<SqlServerValueGenerationStrategy> ValueGenerationStrategy { get; set; }

-        protected virtual bool SetHiLoSequenceName(string value);

-        protected virtual bool SetHiLoSequenceSchema(string value);

-        protected virtual bool SetValueGenerationStrategy(Nullable<SqlServerValueGenerationStrategy> value);

-    }
-    public class SqlServerPropertyAnnotations : RelationalPropertyAnnotations, IRelationalPropertyAnnotations, ISqlServerPropertyAnnotations {
 {
-        public SqlServerPropertyAnnotations(IProperty property);

-        protected SqlServerPropertyAnnotations(RelationalAnnotations annotations);

-        public virtual string HiLoSequenceName { get; set; }

-        public virtual string HiLoSequenceSchema { get; set; }

-        public virtual Nullable<SqlServerValueGenerationStrategy> ValueGenerationStrategy { get; set; }

-        protected override bool CanSetComputedColumnSql(string value);

-        protected override bool CanSetDefaultValue(object value);

-        protected override bool CanSetDefaultValueSql(string value);

-        protected virtual bool CanSetValueGenerationStrategy(Nullable<SqlServerValueGenerationStrategy> value);

-        protected override void ClearAllServerGeneratedValues();

-        public virtual ISequence FindHiLoSequence();

-        protected override string GetComputedColumnSql(bool fallback);

-        protected override object GetDefaultValue(bool fallback);

-        protected override string GetDefaultValueSql(bool fallback);

-        public virtual Nullable<SqlServerValueGenerationStrategy> GetSqlServerValueGenerationStrategy(bool fallbackToModel);

-        protected virtual bool SetHiLoSequenceName(string value);

-        protected virtual bool SetHiLoSequenceSchema(string value);

-        protected virtual bool SetValueGenerationStrategy(Nullable<SqlServerValueGenerationStrategy> value);

-    }
-    public enum SqlServerValueGenerationStrategy {
 {
-        IdentityColumn = 1,

-        SequenceHiLo = 0,

-    }
-    public enum ValueGenerated {
 {
-        Never = 0,

-        OnAdd = 1,

-        OnAddOrUpdate = 3,

-        OnUpdate = 2,

-    }
-}
```

