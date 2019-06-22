# Microsoft.EntityFrameworkCore.Metadata.Conventions

``` diff
-namespace Microsoft.EntityFrameworkCore.Metadata.Conventions {
 {
-    public class ConventionSet {
 {
-        public ConventionSet();

-        public virtual IList<IBaseTypeChangedConvention> BaseEntityTypeChangedConventions { get; }

-        public virtual IList<IEntityTypeAddedConvention> EntityTypeAddedConventions { get; }

-        public virtual IList<IEntityTypeAnnotationChangedConvention> EntityTypeAnnotationChangedConventions { get; }

-        public virtual IList<IEntityTypeIgnoredConvention> EntityTypeIgnoredConventions { get; }

-        public virtual IList<IEntityTypeMemberIgnoredConvention> EntityTypeMemberIgnoredConventions { get; }

-        public virtual IList<IEntityTypeRemovedConvention> EntityTypeRemovedConventions { get; }

-        public virtual IList<IForeignKeyAddedConvention> ForeignKeyAddedConventions { get; }

-        public virtual IList<IForeignKeyOwnershipChangedConvention> ForeignKeyOwnershipChangedConventions { get; }

-        public virtual IList<IForeignKeyRemovedConvention> ForeignKeyRemovedConventions { get; }

-        public virtual IList<IForeignKeyRequirednessChangedConvention> ForeignKeyRequirednessChangedConventions { get; }

-        public virtual IList<IForeignKeyUniquenessChangedConvention> ForeignKeyUniquenessChangedConventions { get; }

-        public virtual IList<IIndexAddedConvention> IndexAddedConventions { get; }

-        public virtual IList<IIndexAnnotationChangedConvention> IndexAnnotationChangedConventions { get; }

-        public virtual IList<IIndexRemovedConvention> IndexRemovedConventions { get; }

-        public virtual IList<IIndexUniquenessChangedConvention> IndexUniquenessChangedConventions { get; }

-        public virtual IList<IKeyAddedConvention> KeyAddedConventions { get; }

-        public virtual IList<IKeyRemovedConvention> KeyRemovedConventions { get; }

-        public virtual IList<IModelAnnotationChangedConvention> ModelAnnotationChangedConventions { get; }

-        public virtual IList<IModelBuiltConvention> ModelBuiltConventions { get; }

-        public virtual IList<IModelInitializedConvention> ModelInitializedConventions { get; }

-        public virtual IList<INavigationAddedConvention> NavigationAddedConventions { get; }

-        public virtual IList<INavigationRemovedConvention> NavigationRemovedConventions { get; }

-        public virtual IList<IPrimaryKeyChangedConvention> PrimaryKeyChangedConventions { get; }

-        public virtual IList<IPrincipalEndChangedConvention> PrincipalEndChangedConventions { get; }

-        public virtual IList<IPropertyAddedConvention> PropertyAddedConventions { get; }

-        public virtual IList<IPropertyAnnotationChangedConvention> PropertyAnnotationChangedConventions { get; }

-        public virtual IList<IPropertyFieldChangedConvention> PropertyFieldChangedConventions { get; }

-        public virtual IList<IPropertyNullabilityChangedConvention> PropertyNullabilityChangedConventions { get; }

-        public static ConventionSet CreateConventionSet(DbContext context);

-    }
-    public class SqlServerConventionSetBuilder : RelationalConventionSetBuilder {
 {
-        public SqlServerConventionSetBuilder(RelationalConventionSetBuilderDependencies dependencies, ISqlGenerationHelper sqlGenerationHelper);

-        public override ConventionSet AddConventions(ConventionSet conventionSet);

-        public static ConventionSet Build();

-    }
-}
```

