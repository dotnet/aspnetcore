# Microsoft.EntityFrameworkCore.SqlServer.Metadata.Conventions.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Metadata.Conventions.Internal {
 {
-    public class SqlServerDbFunctionConvention : RelationalDbFunctionConvention {
 {
-        public SqlServerDbFunctionConvention();

-        protected override void ApplyCustomizations(InternalModelBuilder modelBuilder, string name, Annotation annotation);

-    }
-    public class SqlServerIndexConvention : IBaseTypeChangedConvention, IIndexAddedConvention, IIndexAnnotationChangedConvention, IIndexUniquenessChangedConvention, IPropertyAnnotationChangedConvention, IPropertyNullabilityChangedConvention {
 {
-        public SqlServerIndexConvention(ISqlGenerationHelper sqlGenerationHelper);

-        public virtual bool Apply(InternalEntityTypeBuilder entityTypeBuilder, EntityType oldBaseType);

-        public virtual Annotation Apply(InternalIndexBuilder indexBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-        public virtual bool Apply(InternalPropertyBuilder propertyBuilder);

-        public virtual Annotation Apply(InternalPropertyBuilder propertyBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-        InternalIndexBuilder Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal.IIndexAddedConvention.Apply(InternalIndexBuilder indexBuilder);

-        bool Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal.IIndexUniquenessChangedConvention.Apply(InternalIndexBuilder indexBuilder);

-    }
-    public class SqlServerMemoryOptimizedTablesConvention : IEntityTypeAnnotationChangedConvention, IIndexAddedConvention, IKeyAddedConvention {
 {
-        public SqlServerMemoryOptimizedTablesConvention();

-        public virtual Annotation Apply(InternalEntityTypeBuilder entityTypeBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-        public virtual InternalIndexBuilder Apply(InternalIndexBuilder indexBuilder);

-        public virtual InternalKeyBuilder Apply(InternalKeyBuilder keyBuilder);

-    }
-    public class SqlServerValueGenerationStrategyConvention : IModelBuiltConvention, IModelInitializedConvention {
 {
-        public SqlServerValueGenerationStrategyConvention();

-        InternalModelBuilder Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal.IModelBuiltConvention.Apply(InternalModelBuilder modelBuilder);

-        InternalModelBuilder Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal.IModelInitializedConvention.Apply(InternalModelBuilder modelBuilder);

-    }
-    public class SqlServerValueGeneratorConvention : RelationalValueGeneratorConvention {
 {
-        public SqlServerValueGeneratorConvention();

-        public override Annotation Apply(InternalPropertyBuilder propertyBuilder, string name, Annotation annotation, Annotation oldAnnotation);

-        public override Nullable<ValueGenerated> GetValueGenerated(Property property);

-    }
-}
```

