# Microsoft.AspNetCore.Mvc.ApplicationParts

``` diff
 namespace Microsoft.AspNetCore.Mvc.ApplicationParts {
+    public sealed class ApplicationPartAttribute : Attribute {
+        public ApplicationPartAttribute(string assemblyName);
+        public string AssemblyName { get; }
+    }
-    public class AssemblyPart : ApplicationPart, IApplicationPartTypeProvider, ICompilationReferencesProvider {
+    public class AssemblyPart : ApplicationPart, IApplicationPartTypeProvider {
-        public IEnumerable<string> GetReferencePaths();

     }
 }
```

