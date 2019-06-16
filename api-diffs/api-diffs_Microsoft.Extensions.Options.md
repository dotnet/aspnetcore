# Microsoft.Extensions.Options

``` diff
 namespace Microsoft.Extensions.Options {
     public class ConfigurationChangeTokenSource<TOptions> : IOptionsChangeTokenSource<TOptions> {
         public ConfigurationChangeTokenSource(IConfiguration config);
         public ConfigurationChangeTokenSource(string name, IConfiguration config);
         public string Name { get; }
         public IChangeToken GetChangeToken();
     }
     public class ConfigureFromConfigurationOptions<TOptions> : ConfigureOptions<TOptions> where TOptions : class {
         public ConfigureFromConfigurationOptions(IConfiguration config);
     }
     public class ConfigureNamedOptions<TOptions> : IConfigureNamedOptions<TOptions>, IConfigureOptions<TOptions> where TOptions : class {
         public ConfigureNamedOptions(string name, Action<TOptions> action);
         public Action<TOptions> Action { get; }
         public string Name { get; }
         public virtual void Configure(string name, TOptions options);
         public void Configure(TOptions options);
     }
     public class ConfigureNamedOptions<TOptions, TDep> : IConfigureNamedOptions<TOptions>, IConfigureOptions<TOptions> where TOptions : class where TDep : class {
         public ConfigureNamedOptions(string name, TDep dependency, Action<TOptions, TDep> action);
         public Action<TOptions, TDep> Action { get; }
         public TDep Dependency { get; }
         public string Name { get; }
         public virtual void Configure(string name, TOptions options);
         public void Configure(TOptions options);
     }
     public class ConfigureNamedOptions<TOptions, TDep1, TDep2> : IConfigureNamedOptions<TOptions>, IConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class {
         public ConfigureNamedOptions(string name, TDep1 dependency, TDep2 dependency2, Action<TOptions, TDep1, TDep2> action);
         public Action<TOptions, TDep1, TDep2> Action { get; }
         public TDep1 Dependency1 { get; }
         public TDep2 Dependency2 { get; }
         public string Name { get; }
         public virtual void Configure(string name, TOptions options);
         public void Configure(TOptions options);
     }
     public class ConfigureNamedOptions<TOptions, TDep1, TDep2, TDep3> : IConfigureNamedOptions<TOptions>, IConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class where TDep3 : class {
         public ConfigureNamedOptions(string name, TDep1 dependency, TDep2 dependency2, TDep3 dependency3, Action<TOptions, TDep1, TDep2, TDep3> action);
         public Action<TOptions, TDep1, TDep2, TDep3> Action { get; }
         public TDep1 Dependency1 { get; }
         public TDep2 Dependency2 { get; }
         public TDep3 Dependency3 { get; }
         public string Name { get; }
         public virtual void Configure(string name, TOptions options);
         public void Configure(TOptions options);
     }
     public class ConfigureNamedOptions<TOptions, TDep1, TDep2, TDep3, TDep4> : IConfigureNamedOptions<TOptions>, IConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class {
         public ConfigureNamedOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, Action<TOptions, TDep1, TDep2, TDep3, TDep4> action);
         public Action<TOptions, TDep1, TDep2, TDep3, TDep4> Action { get; }
         public TDep1 Dependency1 { get; }
         public TDep2 Dependency2 { get; }
         public TDep3 Dependency3 { get; }
         public TDep4 Dependency4 { get; }
         public string Name { get; }
         public virtual void Configure(string name, TOptions options);
         public void Configure(TOptions options);
     }
     public class ConfigureNamedOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> : IConfigureNamedOptions<TOptions>, IConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class where TDep5 : class {
         public ConfigureNamedOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, TDep5 dependency5, Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> action);
         public Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> Action { get; }
         public TDep1 Dependency1 { get; }
         public TDep2 Dependency2 { get; }
         public TDep3 Dependency3 { get; }
         public TDep4 Dependency4 { get; }
         public TDep5 Dependency5 { get; }
         public string Name { get; }
         public virtual void Configure(string name, TOptions options);
         public void Configure(TOptions options);
     }
     public class ConfigureOptions<TOptions> : IConfigureOptions<TOptions> where TOptions : class {
         public ConfigureOptions(Action<TOptions> action);
         public Action<TOptions> Action { get; }
         public virtual void Configure(TOptions options);
     }
     public class DataAnnotationValidateOptions<TOptions> : IValidateOptions<TOptions> where TOptions : class {
         public DataAnnotationValidateOptions(string name);
         public string Name { get; }
         public ValidateOptionsResult Validate(string name, TOptions options);
     }
     public interface IConfigureNamedOptions<in TOptions> : IConfigureOptions<TOptions> where TOptions : class {
         void Configure(string name, TOptions options);
     }
     public interface IConfigureOptions<in TOptions> where TOptions : class {
         void Configure(TOptions options);
     }
     public interface IOptions<out TOptions> where TOptions : class, new() {
         TOptions Value { get; }
     }
     public interface IOptionsChangeTokenSource<out TOptions> {
         string Name { get; }
         IChangeToken GetChangeToken();
     }
     public interface IOptionsFactory<TOptions> where TOptions : class, new() {
         TOptions Create(string name);
     }
     public interface IOptionsMonitor<out TOptions> {
         TOptions CurrentValue { get; }
         TOptions Get(string name);
         IDisposable OnChange(Action<TOptions, string> listener);
     }
     public interface IOptionsMonitorCache<TOptions> where TOptions : class {
         void Clear();
         TOptions GetOrAdd(string name, Func<TOptions> createOptions);
         bool TryAdd(string name, TOptions options);
         bool TryRemove(string name);
     }
     public interface IOptionsSnapshot<out TOptions> : IOptions<TOptions> where TOptions : class, new() {
         TOptions Get(string name);
     }
     public interface IPostConfigureOptions<in TOptions> where TOptions : class {
         void PostConfigure(string name, TOptions options);
     }
     public interface IValidateOptions<TOptions> where TOptions : class {
         ValidateOptionsResult Validate(string name, TOptions options);
     }
     public class NamedConfigureFromConfigurationOptions<TOptions> : ConfigureNamedOptions<TOptions> where TOptions : class {
         public NamedConfigureFromConfigurationOptions(string name, IConfiguration config);
         public NamedConfigureFromConfigurationOptions(string name, IConfiguration config, Action<BinderOptions> configureBinder);
     }
     public static class Options {
         public static readonly string DefaultName;
         public static IOptions<TOptions> Create<TOptions>(TOptions options) where TOptions : class, new();
     }
     public class OptionsBuilder<TOptions> where TOptions : class {
         public OptionsBuilder(IServiceCollection services, string name);
         public string Name { get; }
         public IServiceCollection Services { get; }
         public virtual OptionsBuilder<TOptions> Configure(Action<TOptions> configureOptions);
         public virtual OptionsBuilder<TOptions> Configure<TDep1, TDep2, TDep3, TDep4, TDep5>(Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> configureOptions) where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class where TDep5 : class;
         public virtual OptionsBuilder<TOptions> Configure<TDep1, TDep2, TDep3, TDep4>(Action<TOptions, TDep1, TDep2, TDep3, TDep4> configureOptions) where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class;
         public virtual OptionsBuilder<TOptions> Configure<TDep1, TDep2, TDep3>(Action<TOptions, TDep1, TDep2, TDep3> configureOptions) where TDep1 : class where TDep2 : class where TDep3 : class;
         public virtual OptionsBuilder<TOptions> Configure<TDep1, TDep2>(Action<TOptions, TDep1, TDep2> configureOptions) where TDep1 : class where TDep2 : class;
         public virtual OptionsBuilder<TOptions> Configure<TDep>(Action<TOptions, TDep> configureOptions) where TDep : class;
         public virtual OptionsBuilder<TOptions> PostConfigure(Action<TOptions> configureOptions);
         public virtual OptionsBuilder<TOptions> PostConfigure<TDep1, TDep2, TDep3, TDep4, TDep5>(Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> configureOptions) where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class where TDep5 : class;
         public virtual OptionsBuilder<TOptions> PostConfigure<TDep1, TDep2, TDep3, TDep4>(Action<TOptions, TDep1, TDep2, TDep3, TDep4> configureOptions) where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class;
         public virtual OptionsBuilder<TOptions> PostConfigure<TDep1, TDep2, TDep3>(Action<TOptions, TDep1, TDep2, TDep3> configureOptions) where TDep1 : class where TDep2 : class where TDep3 : class;
         public virtual OptionsBuilder<TOptions> PostConfigure<TDep1, TDep2>(Action<TOptions, TDep1, TDep2> configureOptions) where TDep1 : class where TDep2 : class;
         public virtual OptionsBuilder<TOptions> PostConfigure<TDep>(Action<TOptions, TDep> configureOptions) where TDep : class;
         public virtual OptionsBuilder<TOptions> Validate(Func<TOptions, bool> validation);
         public virtual OptionsBuilder<TOptions> Validate(Func<TOptions, bool> validation, string failureMessage);
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
     public class OptionsCache<TOptions> : IOptionsMonitorCache<TOptions> where TOptions : class {
         public OptionsCache();
         public void Clear();
         public virtual TOptions GetOrAdd(string name, Func<TOptions> createOptions);
         public virtual bool TryAdd(string name, TOptions options);
         public virtual bool TryRemove(string name);
     }
     public class OptionsFactory<TOptions> : IOptionsFactory<TOptions> where TOptions : class, new() {
         public OptionsFactory(IEnumerable<IConfigureOptions<TOptions>> setups, IEnumerable<IPostConfigureOptions<TOptions>> postConfigures);
         public OptionsFactory(IEnumerable<IConfigureOptions<TOptions>> setups, IEnumerable<IPostConfigureOptions<TOptions>> postConfigures, IEnumerable<IValidateOptions<TOptions>> validations);
         public TOptions Create(string name);
     }
     public class OptionsManager<TOptions> : IOptions<TOptions>, IOptionsSnapshot<TOptions> where TOptions : class, new() {
         public OptionsManager(IOptionsFactory<TOptions> factory);
         public TOptions Value { get; }
         public virtual TOptions Get(string name);
     }
-    public class OptionsMonitor<TOptions> : IOptionsMonitor<TOptions> where TOptions : class, new() {
+    public class OptionsMonitor<TOptions> : IDisposable, IOptionsMonitor<TOptions> where TOptions : class, new() {
         public OptionsMonitor(IOptionsFactory<TOptions> factory, IEnumerable<IOptionsChangeTokenSource<TOptions>> sources, IOptionsMonitorCache<TOptions> cache);
         public TOptions CurrentValue { get; }
+        public void Dispose();
         public virtual TOptions Get(string name);
         public IDisposable OnChange(Action<TOptions, string> listener);
     }
     public static class OptionsMonitorExtensions {
         public static IDisposable OnChange<TOptions>(this IOptionsMonitor<TOptions> monitor, Action<TOptions> listener);
     }
     public class OptionsValidationException : Exception {
         public OptionsValidationException(string optionsName, Type optionsType, IEnumerable<string> failureMessages);
         public IEnumerable<string> Failures { get; }
+        public override string Message { get; }
         public string OptionsName { get; }
         public Type OptionsType { get; }
     }
     public class OptionsWrapper<TOptions> : IOptions<TOptions> where TOptions : class, new() {
         public OptionsWrapper(TOptions options);
         public TOptions Value { get; }
-        public void Add(string name, TOptions options);

-        public TOptions Get(string name);

-        public bool Remove(string name);

     }
     public class PostConfigureOptions<TOptions> : IPostConfigureOptions<TOptions> where TOptions : class {
         public PostConfigureOptions(string name, Action<TOptions> action);
         public Action<TOptions> Action { get; }
         public string Name { get; }
         public virtual void PostConfigure(string name, TOptions options);
     }
     public class PostConfigureOptions<TOptions, TDep> : IPostConfigureOptions<TOptions> where TOptions : class where TDep : class {
         public PostConfigureOptions(string name, TDep dependency, Action<TOptions, TDep> action);
         public Action<TOptions, TDep> Action { get; }
         public TDep Dependency { get; }
         public string Name { get; }
         public virtual void PostConfigure(string name, TOptions options);
         public void PostConfigure(TOptions options);
     }
     public class PostConfigureOptions<TOptions, TDep1, TDep2> : IPostConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class {
         public PostConfigureOptions(string name, TDep1 dependency, TDep2 dependency2, Action<TOptions, TDep1, TDep2> action);
         public Action<TOptions, TDep1, TDep2> Action { get; }
         public TDep1 Dependency1 { get; }
         public TDep2 Dependency2 { get; }
         public string Name { get; }
         public virtual void PostConfigure(string name, TOptions options);
         public void PostConfigure(TOptions options);
     }
     public class PostConfigureOptions<TOptions, TDep1, TDep2, TDep3> : IPostConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class where TDep3 : class {
         public PostConfigureOptions(string name, TDep1 dependency, TDep2 dependency2, TDep3 dependency3, Action<TOptions, TDep1, TDep2, TDep3> action);
         public Action<TOptions, TDep1, TDep2, TDep3> Action { get; }
         public TDep1 Dependency1 { get; }
         public TDep2 Dependency2 { get; }
         public TDep3 Dependency3 { get; }
         public string Name { get; }
         public virtual void PostConfigure(string name, TOptions options);
         public void PostConfigure(TOptions options);
     }
     public class PostConfigureOptions<TOptions, TDep1, TDep2, TDep3, TDep4> : IPostConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class {
         public PostConfigureOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, Action<TOptions, TDep1, TDep2, TDep3, TDep4> action);
         public Action<TOptions, TDep1, TDep2, TDep3, TDep4> Action { get; }
         public TDep1 Dependency1 { get; }
         public TDep2 Dependency2 { get; }
         public TDep3 Dependency3 { get; }
         public TDep4 Dependency4 { get; }
         public string Name { get; }
         public virtual void PostConfigure(string name, TOptions options);
         public void PostConfigure(TOptions options);
     }
     public class PostConfigureOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> : IPostConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class where TDep5 : class {
         public PostConfigureOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, TDep5 dependency5, Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> action);
         public Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> Action { get; }
         public TDep1 Dependency1 { get; }
         public TDep2 Dependency2 { get; }
         public TDep3 Dependency3 { get; }
         public TDep4 Dependency4 { get; }
         public TDep5 Dependency5 { get; }
         public string Name { get; }
         public virtual void PostConfigure(string name, TOptions options);
         public void PostConfigure(TOptions options);
     }
     public class ValidateOptions<TOptions> : IValidateOptions<TOptions> where TOptions : class {
         public ValidateOptions(string name, Func<TOptions, bool> validation, string failureMessage);
         public string FailureMessage { get; }
         public string Name { get; }
         public Func<TOptions, bool> Validation { get; }
         public ValidateOptionsResult Validate(string name, TOptions options);
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
         public static readonly ValidateOptionsResult Skip;
         public static readonly ValidateOptionsResult Success;
         public ValidateOptionsResult();
         public bool Failed { get; protected set; }
         public string FailureMessage { get; protected set; }
+        public IEnumerable<string> Failures { get; protected set; }
         public bool Skipped { get; protected set; }
         public bool Succeeded { get; protected set; }
+        public static ValidateOptionsResult Fail(IEnumerable<string> failures);
         public static ValidateOptionsResult Fail(string failureMessage);
     }
 }
```

