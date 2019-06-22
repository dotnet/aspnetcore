# Microsoft.EntityFrameworkCore.ValueGeneration

``` diff
-namespace Microsoft.EntityFrameworkCore.ValueGeneration {
 {
-    public class GuidValueGenerator : ValueGenerator<Guid> {
 {
-        public GuidValueGenerator();

-        public override bool GeneratesTemporaryValues { get; }

-        public override Guid Next(EntityEntry entry);

-    }
-    public abstract class HiLoValueGenerator<TValue> : ValueGenerator<TValue> {
 {
-        protected HiLoValueGenerator(HiLoValueGeneratorState generatorState);

-        protected abstract long GetNewLowValue();

-        protected virtual Task<long> GetNewLowValueAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override TValue Next(EntityEntry entry);

-        public override Task<TValue> NextAsync(EntityEntry entry, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class HiLoValueGeneratorState {
 {
-        public HiLoValueGeneratorState(int blockSize);

-        public virtual TValue Next<TValue>(Func<long> getNewLowValue);

-        public virtual Task<TValue> NextAsync<TValue>(Func<CancellationToken, Task<long>> getNewLowValue, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public interface IValueGeneratorCache {
 {
-        ValueGenerator GetOrAdd(IProperty property, IEntityType entityType, Func<IProperty, IEntityType, ValueGenerator> factory);

-    }
-    public interface IValueGeneratorSelector {
 {
-        ValueGenerator Select(IProperty property, IEntityType entityType);

-    }
-    public class RelationalValueGeneratorSelector : ValueGeneratorSelector {
 {
-        public RelationalValueGeneratorSelector(ValueGeneratorSelectorDependencies dependencies);

-        public override ValueGenerator Create(IProperty property, IEntityType entityType);

-    }
-    public class SequentialGuidValueGenerator : ValueGenerator<Guid> {
 {
-        public SequentialGuidValueGenerator();

-        public override bool GeneratesTemporaryValues { get; }

-        public override Guid Next(EntityEntry entry);

-    }
-    public class TemporaryGuidValueGenerator : GuidValueGenerator {
 {
-        public TemporaryGuidValueGenerator();

-        public override bool GeneratesTemporaryValues { get; }

-    }
-    public abstract class ValueGenerator {
 {
-        protected ValueGenerator();

-        public abstract bool GeneratesTemporaryValues { get; }

-        public virtual object Next(EntityEntry entry);

-        public virtual Task<object> NextAsync(EntityEntry entry, CancellationToken cancellationToken = default(CancellationToken));

-        protected abstract object NextValue(EntityEntry entry);

-        protected virtual Task<object> NextValueAsync(EntityEntry entry, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public abstract class ValueGenerator<TValue> : ValueGenerator {
 {
-        protected ValueGenerator();

-        public abstract new TValue Next(EntityEntry entry);

-        public virtual new Task<TValue> NextAsync(EntityEntry entry, CancellationToken cancellationToken = default(CancellationToken));

-        protected override object NextValue(EntityEntry entry);

-        protected override Task<object> NextValueAsync(EntityEntry entry, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class ValueGeneratorCache : IValueGeneratorCache {
 {
-        public ValueGeneratorCache(ValueGeneratorCacheDependencies dependencies);

-        public virtual ValueGenerator GetOrAdd(IProperty property, IEntityType entityType, Func<IProperty, IEntityType, ValueGenerator> factory);

-    }
-    public sealed class ValueGeneratorCacheDependencies {
 {
-        public ValueGeneratorCacheDependencies();

-    }
-    public abstract class ValueGeneratorFactory {
 {
-        protected ValueGeneratorFactory();

-        public abstract ValueGenerator Create(IProperty property);

-    }
-    public class ValueGeneratorSelector : IValueGeneratorSelector {
 {
-        public ValueGeneratorSelector(ValueGeneratorSelectorDependencies dependencies);

-        public virtual IValueGeneratorCache Cache { get; }

-        protected virtual ValueGeneratorSelectorDependencies Dependencies { get; }

-        public virtual ValueGenerator Create(IProperty property, IEntityType entityType);

-        public virtual ValueGenerator Select(IProperty property, IEntityType entityType);

-    }
-    public sealed class ValueGeneratorSelectorDependencies {
 {
-        public ValueGeneratorSelectorDependencies(IValueGeneratorCache cache);

-        public IValueGeneratorCache Cache { get; }

-        public ValueGeneratorSelectorDependencies With(IValueGeneratorCache cache);

-    }
-}
```

