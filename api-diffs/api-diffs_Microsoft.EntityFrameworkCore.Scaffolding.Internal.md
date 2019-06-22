# Microsoft.EntityFrameworkCore.Scaffolding.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Scaffolding.Internal {
 {
-    public class CandidateNamingService : ICandidateNamingService {
 {
-        public CandidateNamingService();

-        public virtual string GenerateCandidateIdentifier(DatabaseColumn originalColumn);

-        public virtual string GenerateCandidateIdentifier(DatabaseTable originalTable);

-        public virtual string GetDependentEndCandidateNavigationPropertyName(IForeignKey foreignKey);

-        public virtual string GetPrincipalEndCandidateNavigationPropertyName(IForeignKey foreignKey, string dependentEndNavigationPropertyName);

-    }
-    public class CSharpDbContextGenerator : ICSharpDbContextGenerator {
 {
-        public CSharpDbContextGenerator(IEnumerable<IScaffoldingProviderCodeGenerator> legacyProviderCodeGenerators, IEnumerable<IProviderConfigurationCodeGenerator> providerCodeGenerators, IAnnotationCodeGenerator annotationCodeGenerator, ICSharpHelper cSharpHelper);

-        protected virtual void GenerateClass(IModel model, string contextName, string connectionString, bool useDataAnnotations, bool suppressConnectionStringWarning);

-        protected virtual void GenerateOnConfiguring(string connectionString, bool suppressConnectionStringWarning);

-        protected virtual void GenerateOnModelCreating(IModel model, bool useDataAnnotations);

-        public virtual string WriteCode(IModel model, string @namespace, string contextName, string connectionString, bool useDataAnnotations, bool suppressConnectionStringWarning);

-    }
-    public class CSharpEntityTypeGenerator : ICSharpEntityTypeGenerator {
 {
-        public CSharpEntityTypeGenerator(ICSharpHelper cSharpHelper);

-        protected virtual void GenerateClass(IEntityType entityType);

-        protected virtual void GenerateConstructor(IEntityType entityType);

-        protected virtual void GenerateEntityTypeDataAnnotations(IEntityType entityType);

-        protected virtual void GenerateNavigationProperties(IEntityType entityType);

-        protected virtual void GenerateProperties(IEntityType entityType);

-        protected virtual void GeneratePropertyDataAnnotations(IProperty property);

-        public virtual string WriteCode(IEntityType entityType, string @namespace, bool useDataAnnotations);

-    }
-    public class CSharpModelGenerator : ModelCodeGenerator {
 {
-        public CSharpModelGenerator(ModelCodeGeneratorDependencies dependencies, ICSharpDbContextGenerator cSharpDbContextGenerator, ICSharpEntityTypeGenerator cSharpEntityTypeGenerator);

-        public virtual ICSharpDbContextGenerator CSharpDbContextGenerator { get; }

-        public virtual ICSharpEntityTypeGenerator CSharpEntityTypeGenerator { get; }

-        public override string Language { get; }

-        public override ScaffoldedModel GenerateModel(IModel model, string @namespace, string contextDir, string contextName, string connectionString, ModelCodeGenerationOptions options);

-    }
-    public class CSharpNamer<T> {
 {
-        protected readonly Dictionary<T, string> NameCache;

-        public CSharpNamer(Func<T, string> nameGetter, ICSharpUtilities cSharpUtilities, Func<string, string> singularizePluralizer);

-        public virtual string GetName(T item);

-    }
-    public class CSharpUniqueNamer<T> : CSharpNamer<T> {
 {
-        public CSharpUniqueNamer(Func<T, string> nameGetter, ICSharpUtilities cSharpUtilities, Func<string, string> singularizePluralizer);

-        public CSharpUniqueNamer(Func<T, string> nameGetter, IEnumerable<string> usedNames, ICSharpUtilities cSharpUtilities, Func<string, string> singularizePluralizer);

-        public override string GetName(T item);

-    }
-    public class CSharpUtilities : ICSharpUtilities {
 {
-        public CSharpUtilities();

-        public virtual string GenerateCSharpIdentifier(string identifier, ICollection<string> existingIdentifiers, Func<string, string> singularizePluralizer);

-        public virtual string GenerateCSharpIdentifier(string identifier, ICollection<string> existingIdentifiers, Func<string, string> singularizePluralizer, Func<string, ICollection<string>, string> uniquifier);

-        public virtual bool IsCSharpKeyword(string identifier);

-        public virtual bool IsValidIdentifier(string name);

-        public virtual string Uniquifier(string proposedIdentifier, ICollection<string> existingIdentifiers);

-    }
-    public interface ICandidateNamingService {
 {
-        string GenerateCandidateIdentifier(DatabaseColumn originalColumn);

-        string GenerateCandidateIdentifier(DatabaseTable originalTable);

-        string GetDependentEndCandidateNavigationPropertyName(IForeignKey foreignKey);

-        string GetPrincipalEndCandidateNavigationPropertyName(IForeignKey foreignKey, string dependentEndNavigationPropertyName);

-    }
-    public interface ICSharpDbContextGenerator {
 {
-        string WriteCode(IModel model, string @namespace, string contextName, string connectionString, bool useDataAnnotations, bool suppressConnectionStringWarning);

-    }
-    public interface ICSharpEntityTypeGenerator {
 {
-        string WriteCode(IEntityType entityType, string @namespace, bool useDataAnnotations);

-    }
-    public interface ICSharpUtilities {
 {
-        string GenerateCSharpIdentifier(string identifier, ICollection<string> existingIdentifiers, Func<string, string> singularizePluralizer);

-        string GenerateCSharpIdentifier(string identifier, ICollection<string> existingIdentifiers, Func<string, string> singularizePluralizer, Func<string, ICollection<string>, string> uniquifier);

-        bool IsCSharpKeyword(string identifier);

-        bool IsValidIdentifier(string name);

-    }
-    public interface IScaffoldingModelFactory {
 {
-        IModel Create(DatabaseModel databaseModel, bool useDatabaseNames);

-    }
-    public interface IScaffoldingTypeMapper {
 {
-        TypeScaffoldingInfo FindMapping(string storeType, bool keyOrIndex, bool rowVersion);

-    }
-    public class ModelCodeGeneratorSelector : LanguageBasedSelector<IModelCodeGenerator>, IModelCodeGeneratorSelector {
 {
-        public ModelCodeGeneratorSelector(IEnumerable<IModelCodeGenerator> services);

-    }
-    public class RelationalScaffoldingModelFactory : IScaffoldingModelFactory {
 {
-        public RelationalScaffoldingModelFactory(IOperationReporter reporter, ICandidateNamingService candidateNamingService, IPluralizer pluralizer, ICSharpUtilities cSharpUtilities, IScaffoldingTypeMapper scaffoldingTypeMapper);

-        protected virtual void AddNavigationProperties(IMutableForeignKey foreignKey);

-        public virtual IModel Create(DatabaseModel databaseModel, bool useDatabaseNames);

-        protected virtual List<string> ExistingIdentifiers(IEntityType entityType);

-        protected virtual string GetDbSetName(DatabaseTable table);

-        protected virtual string GetEntityTypeName(DatabaseTable table);

-        protected virtual string GetPropertyName(DatabaseColumn column);

-        protected virtual TypeScaffoldingInfo GetTypeScaffoldingInfo(DatabaseColumn column);

-        protected virtual PropertyBuilder VisitColumn(EntityTypeBuilder builder, DatabaseColumn column);

-        protected virtual EntityTypeBuilder VisitColumns(EntityTypeBuilder builder, ICollection<DatabaseColumn> columns);

-        protected virtual ModelBuilder VisitDatabaseModel(ModelBuilder modelBuilder, DatabaseModel databaseModel);

-        protected virtual IMutableForeignKey VisitForeignKey(ModelBuilder modelBuilder, DatabaseForeignKey foreignKey);

-        protected virtual ModelBuilder VisitForeignKeys(ModelBuilder modelBuilder, IList<DatabaseForeignKey> foreignKeys);

-        protected virtual IndexBuilder VisitIndex(EntityTypeBuilder builder, DatabaseIndex index);

-        protected virtual EntityTypeBuilder VisitIndexes(EntityTypeBuilder builder, ICollection<DatabaseIndex> indexes);

-        protected virtual KeyBuilder VisitPrimaryKey(EntityTypeBuilder builder, DatabaseTable table);

-        protected virtual SequenceBuilder VisitSequence(ModelBuilder modelBuilder, DatabaseSequence sequence);

-        protected virtual ModelBuilder VisitSequences(ModelBuilder modelBuilder, ICollection<DatabaseSequence> sequences);

-        protected virtual EntityTypeBuilder VisitTable(ModelBuilder modelBuilder, DatabaseTable table);

-        protected virtual ModelBuilder VisitTables(ModelBuilder modelBuilder, ICollection<DatabaseTable> tables);

-        protected virtual IndexBuilder VisitUniqueConstraint(EntityTypeBuilder builder, DatabaseUniqueConstraint uniqueConstraint);

-        protected virtual EntityTypeBuilder VisitUniqueConstraints(EntityTypeBuilder builder, ICollection<DatabaseUniqueConstraint> uniqueConstraints);

-    }
-    public class ReverseEngineerScaffolder : IReverseEngineerScaffolder {
 {
-        public ReverseEngineerScaffolder(IDatabaseModelFactory databaseModelFactory, IScaffoldingModelFactory scaffoldingModelFactory, IModelCodeGeneratorSelector modelCodeGeneratorSelector, ICSharpUtilities cSharpUtilities, ICSharpHelper cSharpHelper, INamedConnectionStringResolver connectionStringResolver);

-        public virtual SavedModelFiles Save(ScaffoldedModel scaffoldedModel, string outputDir, bool overwriteFiles);

-        public virtual ScaffoldedModel ScaffoldModel(string connectionString, IEnumerable<string> tables, IEnumerable<string> schemas, string @namespace, string language, string contextDir, string contextName, ModelReverseEngineerOptions modelOptions, ModelCodeGenerationOptions codeOptions);

-    }
-    public class ScaffoldingTypeMapper : IScaffoldingTypeMapper {
 {
-        public ScaffoldingTypeMapper(IRelationalTypeMappingSource typeMappingSource);

-        public virtual TypeScaffoldingInfo FindMapping(string storeType, bool keyOrIndex, bool rowVersion);

-    }
-    public class TableSelectionSet {
 {
-        public static readonly TableSelectionSet All;

-        public TableSelectionSet();

-        public TableSelectionSet(IEnumerable<string> tables);

-        public TableSelectionSet(IEnumerable<string> tables, IEnumerable<string> schemas);

-        public virtual IReadOnlyList<TableSelectionSet.Selection> Schemas { get; }

-        public virtual IReadOnlyList<TableSelectionSet.Selection> Tables { get; }

-        public class Selection {
 {
-            public Selection(string selectionText);

-            public virtual bool IsMatched { get; set; }

-            public virtual string Text { get; }

-        }
-    }
-    public class TypeScaffoldingInfo {
 {
-        public TypeScaffoldingInfo(Type clrType, bool inferred, Nullable<bool> scaffoldUnicode, Nullable<int> scaffoldMaxLength, Nullable<bool> scaffoldFixedLength);

-        public virtual Type ClrType { get; }

-        public virtual bool IsInferred { get; }

-        public virtual Nullable<bool> ScaffoldFixedLength { get; }

-        public virtual Nullable<int> ScaffoldMaxLength { get; }

-        public virtual Nullable<bool> ScaffoldUnicode { get; }

-    }
-}
```

