# Microsoft.EntityFrameworkCore.SqlServer.ValueGeneration.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.ValueGeneration.Internal {
 {
-    public interface ISqlServerSequenceValueGeneratorFactory {
 {
-        ValueGenerator Create(IProperty property, SqlServerSequenceValueGeneratorState generatorState, ISqlServerConnection connection);

-    }
-    public interface ISqlServerValueGeneratorCache : IValueGeneratorCache {
 {
-        SqlServerSequenceValueGeneratorState GetOrAddSequenceState(IProperty property);

-    }
-    public class SqlServerSequenceHiLoValueGenerator<TValue> : HiLoValueGenerator<TValue> {
 {
-        public SqlServerSequenceHiLoValueGenerator(IRawSqlCommandBuilder rawSqlCommandBuilder, ISqlServerUpdateSqlGenerator sqlGenerator, SqlServerSequenceValueGeneratorState generatorState, ISqlServerConnection connection);

-        public override bool GeneratesTemporaryValues { get; }

-        protected override long GetNewLowValue();

-        protected override Task<long> GetNewLowValueAsync(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class SqlServerSequenceValueGeneratorFactory : ISqlServerSequenceValueGeneratorFactory {
 {
-        public SqlServerSequenceValueGeneratorFactory(IRawSqlCommandBuilder rawSqlCommandBuilder, ISqlServerUpdateSqlGenerator sqlGenerator);

-        public virtual ValueGenerator Create(IProperty property, SqlServerSequenceValueGeneratorState generatorState, ISqlServerConnection connection);

-    }
-    public class SqlServerSequenceValueGeneratorState : HiLoValueGeneratorState {
 {
-        public SqlServerSequenceValueGeneratorState(ISequence sequence);

-        public virtual ISequence Sequence { get; }

-    }
-    public class SqlServerValueGeneratorCache : ValueGeneratorCache, ISqlServerValueGeneratorCache, IValueGeneratorCache {
 {
-        public SqlServerValueGeneratorCache(ValueGeneratorCacheDependencies dependencies);

-        public virtual SqlServerSequenceValueGeneratorState GetOrAddSequenceState(IProperty property);

-    }
-    public class SqlServerValueGeneratorSelector : RelationalValueGeneratorSelector {
 {
-        public SqlServerValueGeneratorSelector(ValueGeneratorSelectorDependencies dependencies, ISqlServerSequenceValueGeneratorFactory sequenceFactory, ISqlServerConnection connection);

-        public virtual new ISqlServerValueGeneratorCache Cache { get; }

-        public override ValueGenerator Create(IProperty property, IEntityType entityType);

-        public override ValueGenerator Select(IProperty property, IEntityType entityType);

-    }
-}
```

