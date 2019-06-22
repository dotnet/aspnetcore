# Microsoft.EntityFrameworkCore.InMemory.ValueGeneration.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.InMemory.ValueGeneration.Internal {
 {
-    public class InMemoryIntegerValueGenerator<TValue> : ValueGenerator<TValue> {
 {
-        public InMemoryIntegerValueGenerator();

-        public override bool GeneratesTemporaryValues { get; }

-        public override TValue Next(EntityEntry entry);

-    }
-    public class InMemoryIntegerValueGeneratorFactory : ValueGeneratorFactory {
 {
-        public InMemoryIntegerValueGeneratorFactory();

-        public override ValueGenerator Create(IProperty property);

-    }
-    public class InMemoryValueGeneratorSelector : ValueGeneratorSelector {
 {
-        public InMemoryValueGeneratorSelector(ValueGeneratorSelectorDependencies dependencies);

-        public override ValueGenerator Create(IProperty property, IEntityType entityType);

-    }
-}
```

