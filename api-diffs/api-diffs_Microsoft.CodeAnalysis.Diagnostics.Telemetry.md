# Microsoft.CodeAnalysis.Diagnostics.Telemetry

``` diff
-namespace Microsoft.CodeAnalysis.Diagnostics.Telemetry {
 {
-    public sealed class AnalyzerTelemetryInfo {
 {
-        public AnalyzerTelemetryInfo();

-        public int CodeBlockActionsCount { get; set; }

-        public int CodeBlockEndActionsCount { get; set; }

-        public int CodeBlockStartActionsCount { get; set; }

-        public int CompilationActionsCount { get; set; }

-        public int CompilationEndActionsCount { get; set; }

-        public int CompilationStartActionsCount { get; set; }

-        public TimeSpan ExecutionTime { get; set; }

-        public int OperationActionsCount { get; set; }

-        public int OperationBlockActionsCount { get; set; }

-        public int OperationBlockEndActionsCount { get; set; }

-        public int OperationBlockStartActionsCount { get; set; }

-        public int SemanticModelActionsCount { get; set; }

-        public int SymbolActionsCount { get; set; }

-        public int SyntaxNodeActionsCount { get; set; }

-        public int SyntaxTreeActionsCount { get; set; }

-    }
-}
```

