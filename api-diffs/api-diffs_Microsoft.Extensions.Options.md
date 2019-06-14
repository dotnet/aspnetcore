# Microsoft.Extensions.Options

``` diff
 namespace Microsoft.Extensions.Options {
     public class OptionsBuilder<TOptions> where TOptions : class {
+        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2, TDep3, TDep4, TDep5>(Func<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, bool> validation);
+        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2, TDep3, TDep4, TDep5>(Func<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, bool> validation, string failureMessage);
+        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2, TDep3, TDep4>(Func<TOptions, TDep1, TDep2, TDep3, TDep4, bool> validation);
+        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2, TDep3, TDep4>(Func<TOptions, TDep1, TDep2, TDep3, TDep4, bool> validation, string failureMessage);
+        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2, TDep3>(Func<TOptions, TDep1, TDep2, TDep3, bool> validation);
+        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2, TDep3>(Func<TOptions, TDep1, TDep2, TDep3, bool> validation, string failureMessage);
+        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2>(Func<TOptions, TDep1, TDep2, bool> validation);
+        public virtual OptionsBuilder<TOptions> Validate<TDep1, TDep2>(Func<TOptions, TDep1, TDep2, bool> validation, string failureMessage);
+        public virtual OptionsBuilder<TOptions> Validate<TDep>(Func<TOptions, TDep, bool> validation);
+        public virtual OptionsBuilder<TOptions> Validate<TDep>(Func<TOptions, TDep, bool> validation, string failureMessage);
     }
-    public class OptionsMonitor<TOptions> : IOptionsMonitor<TOptions> where TOptions : class, new() {
+    public class OptionsMonitor<TOptions> : IDisposable, IOptionsMonitor<TOptions> where TOptions : class, new() {
+        public void Dispose();
     }
     public class OptionsValidationException : Exception {
+        public override string Message { get; }
     }
     public class OptionsWrapper<TOptions> : IOptions<TOptions> where TOptions : class, new() {
-        public void Add(string name, TOptions options);

-        public TOptions Get(string name);

-        public bool Remove(string name);

     }
+    public class ValidateOptions<TOptions, TDep> : IValidateOptions<TOptions> where TOptions : class {
+        public ValidateOptions(string name, TDep dependency, Func<TOptions, TDep, bool> validation, string failureMessage);
+        public TDep Dependency { get; }
+        public string FailureMessage { get; }
+        public string Name { get; }
+        public Func<TOptions, TDep, bool> Validation { get; }
+        public ValidateOptionsResult Validate(string name, TOptions options);
+    }
+    public class ValidateOptions<TOptions, TDep1, TDep2> : IValidateOptions<TOptions> where TOptions : class {
+        public ValidateOptions(string name, TDep1 dependency1, TDep2 dependency2, Func<TOptions, TDep1, TDep2, bool> validation, string failureMessage);
+        public TDep1 Dependency1 { get; }
+        public TDep2 Dependency2 { get; }
+        public string FailureMessage { get; }
+        public string Name { get; }
+        public Func<TOptions, TDep1, TDep2, bool> Validation { get; }
+        public ValidateOptionsResult Validate(string name, TOptions options);
+    }
+    public class ValidateOptions<TOptions, TDep1, TDep2, TDep3> : IValidateOptions<TOptions> where TOptions : class {
+        public ValidateOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, Func<TOptions, TDep1, TDep2, TDep3, bool> validation, string failureMessage);
+        public TDep1 Dependency1 { get; }
+        public TDep2 Dependency2 { get; }
+        public TDep3 Dependency3 { get; }
+        public string FailureMessage { get; }
+        public string Name { get; }
+        public Func<TOptions, TDep1, TDep2, TDep3, bool> Validation { get; }
+        public ValidateOptionsResult Validate(string name, TOptions options);
+    }
+    public class ValidateOptions<TOptions, TDep1, TDep2, TDep3, TDep4> : IValidateOptions<TOptions> where TOptions : class {
+        public ValidateOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, Func<TOptions, TDep1, TDep2, TDep3, TDep4, bool> validation, string failureMessage);
+        public TDep1 Dependency1 { get; }
+        public TDep2 Dependency2 { get; }
+        public TDep3 Dependency3 { get; }
+        public TDep4 Dependency4 { get; }
+        public string FailureMessage { get; }
+        public string Name { get; }
+        public Func<TOptions, TDep1, TDep2, TDep3, TDep4, bool> Validation { get; }
+        public ValidateOptionsResult Validate(string name, TOptions options);
+    }
+    public class ValidateOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> : IValidateOptions<TOptions> where TOptions : class {
+        public ValidateOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, TDep5 dependency5, Func<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, bool> validation, string failureMessage);
+        public TDep1 Dependency1 { get; }
+        public TDep2 Dependency2 { get; }
+        public TDep3 Dependency3 { get; }
+        public TDep4 Dependency4 { get; }
+        public TDep5 Dependency5 { get; }
+        public string FailureMessage { get; }
+        public string Name { get; }
+        public Func<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5, bool> Validation { get; }
+        public ValidateOptionsResult Validate(string name, TOptions options);
+    }
     public class ValidateOptionsResult {
+        public IEnumerable<string> Failures { get; protected set; }
+        public static ValidateOptionsResult Fail(IEnumerable<string> failures);
     }
 }
```

