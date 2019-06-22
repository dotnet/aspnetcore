# Microsoft.EntityFrameworkCore.SqlServer.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Internal {
 {
-    public static class SqlServerStrings {
 {
-        public static readonly EventDefinition<string, string, bool> LogFoundIndex;

-        public static readonly EventDefinition<string, string, string, string> LogFoundForeignKey;

-        public static readonly EventDefinition<string, string, string, string> LogPrincipalColumnNotFound;

-        public static readonly EventDefinition<string, string, string> LogPrincipalTableNotInSelectionSet;

-        public static readonly EventDefinition<string, string> LogByteIdentityColumn;

-        public static readonly EventDefinition<string, string> LogDefaultDecimalTypeColumn;

-        public static readonly EventDefinition<string, string> LogFoundPrimaryKey;

-        public static readonly EventDefinition<string, string> LogFoundTypeAlias;

-        public static readonly EventDefinition<string, string> LogFoundUniqueConstraint;

-        public static readonly EventDefinition<string, string> LogReflexiveConstraintIgnored;

-        public static readonly EventDefinition<string> LogFoundDefaultSchema;

-        public static readonly EventDefinition<string> LogFoundTable;

-        public static readonly EventDefinition<string> LogMissingSchema;

-        public static readonly EventDefinition<string> LogMissingTable;

-        public static readonly FallbackEventDefinition LogFoundColumn;

-        public static readonly FallbackEventDefinition LogFoundSequence;

-        public static string AlterIdentityColumn { get; }

-        public static string AlterMemoryOptimizedTable { get; }

-        public static string ContainsFunctionOnClient { get; }

-        public static string FreeTextFunctionOnClient { get; }

-        public static string IndexTableRequired { get; }

-        public static string InvalidColumnNameForFreeText { get; }

-        public static string NoInitialCatalog { get; }

-        public static string TransientExceptionDetected { get; }

-        public static string DuplicateColumnNameValueGenerationStrategyMismatch(object entityType1, object property1, object entityType2, object property2, object columnName, object table);

-        public static string DuplicateKeyMismatchedClustering(object key1, object entityType1, object key2, object entityType2, object table, object keyName);

-        public static string IdentityBadType(object property, object entityType, object propertyType);

-        public static string IncludePropertyDuplicated(object entityType, object property);

-        public static string IncludePropertyInIndex(object entityType, object property);

-        public static string IncludePropertyNotFound(object entityType, object property);

-        public static string IncompatibleTableMemoryOptimizedMismatch(object table, object entityType, object otherEntityType, object memoryOptimizedEntityType, object nonMemoryOptimizedEntityType);

-        public static string InvalidTableToIncludeInScaffolding(object table);

-        public static string MultipleIdentityColumns(object properties, object table);

-        public static string NonKeyValueGeneration(object property, object entityType);

-        public static string SequenceBadType(object property, object entityType, object propertyType);

-        public static string UnqualifiedDataType(object dataType);

-        public static string UnqualifiedDataTypeOnProperty(object dataType, object property);

-    }
-}
```

