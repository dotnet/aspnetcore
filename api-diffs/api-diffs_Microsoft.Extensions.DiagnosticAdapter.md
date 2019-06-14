# Microsoft.Extensions.DiagnosticAdapter

``` diff
-namespace Microsoft.Extensions.DiagnosticAdapter {
 {
-    public class DiagnosticNameAttribute : Attribute {
 {
-        public DiagnosticNameAttribute(string name);

-        public string Name { get; }

-    }
-    public class DiagnosticSourceAdapter : IObserver<KeyValuePair<string, object>> {
 {
-        public DiagnosticSourceAdapter(object target);

-        public DiagnosticSourceAdapter(object target, Func<string, bool> isEnabled);

-        public DiagnosticSourceAdapter(object target, Func<string, bool> isEnabled, IDiagnosticSourceMethodAdapter methodAdapter);

-        public DiagnosticSourceAdapter(object target, Func<string, object, object, bool> isEnabled);

-        public DiagnosticSourceAdapter(object target, Func<string, object, object, bool> isEnabled, IDiagnosticSourceMethodAdapter methodAdapter);

-        public bool IsEnabled(string diagnosticName);

-        public bool IsEnabled(string diagnosticName, object arg1, object arg2 = null);

-        void System.IObserver<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.OnCompleted();

-        void System.IObserver<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.OnError(Exception error);

-        void System.IObserver<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.OnNext(KeyValuePair<string, object> value);

-        public void Write(string diagnosticName, object parameters);

-    }
-    public interface IDiagnosticSourceMethodAdapter {
 {
-        Func<object, object, bool> Adapt(MethodInfo method, Type inputType);

-    }
-    public class ProxyDiagnosticSourceMethodAdapter : IDiagnosticSourceMethodAdapter {
 {
-        public ProxyDiagnosticSourceMethodAdapter();

-        public Func<object, object, bool> Adapt(MethodInfo method, Type inputType);

-    }
-}
```

