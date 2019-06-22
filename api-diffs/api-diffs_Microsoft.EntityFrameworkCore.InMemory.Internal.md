# Microsoft.EntityFrameworkCore.InMemory.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.InMemory.Internal {
 {
-    public static class InMemoryStrings {
 {
-        public static readonly EventDefinition LogTransactionsNotSupported;

-        public static readonly EventDefinition<int> LogSavedChanges;

-        public static string UpdateConcurrencyException { get; }

-        public static string UpdateConcurrencyTokenException(object entityType, object properties);

-        public static string UpdateConcurrencyTokenExceptionSensitive(object entityType, object keyValue, object conflictingValues, object databaseValues);

-    }
-}
```

