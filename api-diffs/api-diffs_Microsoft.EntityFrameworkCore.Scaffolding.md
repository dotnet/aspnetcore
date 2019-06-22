# Microsoft.EntityFrameworkCore.Scaffolding

``` diff
-namespace Microsoft.EntityFrameworkCore.Scaffolding {
 {
-    public interface IDatabaseModelFactory {
 {
-        DatabaseModel Create(DbConnection connection, IEnumerable<string> tables, IEnumerable<string> schemas);

-        DatabaseModel Create(string connectionString, IEnumerable<string> tables, IEnumerable<string> schemas);

-    }
-    public interface IModelCodeGenerator : ILanguageBasedService {
 {
-        ScaffoldedModel GenerateModel(IModel model, string @namespace, string contextDir, string contextName, string connectionString, ModelCodeGenerationOptions options);

-    }
-    public interface IModelCodeGeneratorSelector {
 {
-        IModelCodeGenerator Select(string language);

-    }
-    public interface IProviderCodeGeneratorPlugin {
 {
-        MethodCallCodeFragment GenerateContextOptions();

-        MethodCallCodeFragment GenerateProviderOptions();

-    }
-    public interface IProviderConfigurationCodeGenerator {
 {
-        MethodCallCodeFragment GenerateContextOptions();

-        MethodCallCodeFragment GenerateProviderOptions();

-        MethodCallCodeFragment GenerateUseProvider(string connectionString);

-        MethodCallCodeFragment GenerateUseProvider(string connectionString, MethodCallCodeFragment providerOptions);

-    }
-    public interface IReverseEngineerScaffolder {
 {
-        SavedModelFiles Save(ScaffoldedModel scaffoldedModel, string outputDir, bool overwriteFiles);

-        ScaffoldedModel ScaffoldModel(string connectionString, IEnumerable<string> tables, IEnumerable<string> schemas, string @namespace, string language, string contextDir, string contextName, ModelReverseEngineerOptions modelOptions, ModelCodeGenerationOptions codeOptions);

-    }
-    public interface IScaffoldingProviderCodeGenerator {
 {
-        string GenerateUseProvider(string connectionString, string language);

-    }
-    public class ModelCodeGenerationOptions {
 {
-        public ModelCodeGenerationOptions();

-        public virtual bool SuppressConnectionStringWarning { get; set; }

-        public virtual bool UseDataAnnotations { get; set; }

-    }
-    public abstract class ModelCodeGenerator : ILanguageBasedService, IModelCodeGenerator {
 {
-        protected ModelCodeGenerator(ModelCodeGeneratorDependencies dependencies);

-        protected virtual ModelCodeGeneratorDependencies Dependencies { get; }

-        public abstract string Language { get; }

-        public abstract ScaffoldedModel GenerateModel(IModel model, string @namespace, string contextDir, string contextName, string connectionString, ModelCodeGenerationOptions options);

-    }
-    public sealed class ModelCodeGeneratorDependencies {
 {
-        public ModelCodeGeneratorDependencies();

-    }
-    public class ModelReverseEngineerOptions {
 {
-        public ModelReverseEngineerOptions();

-        public virtual bool UseDatabaseNames { get; set; }

-    }
-    public abstract class ProviderCodeGenerator : IProviderConfigurationCodeGenerator {
 {
-        protected ProviderCodeGenerator(ProviderCodeGeneratorDependencies dependencies);

-        protected virtual ProviderCodeGeneratorDependencies Dependencies { get; }

-        public virtual MethodCallCodeFragment GenerateContextOptions();

-        public virtual MethodCallCodeFragment GenerateProviderOptions();

-        public virtual MethodCallCodeFragment GenerateUseProvider(string connectionString);

-        public virtual MethodCallCodeFragment GenerateUseProvider(string connectionString, MethodCallCodeFragment providerOptions);

-    }
-    public sealed class ProviderCodeGeneratorDependencies {
 {
-        public ProviderCodeGeneratorDependencies(IEnumerable<IProviderCodeGeneratorPlugin> plugins);

-        public IEnumerable<IProviderCodeGeneratorPlugin> Plugins { get; }

-        public ProviderCodeGeneratorDependencies With(IEnumerable<IProviderCodeGeneratorPlugin> plugins);

-    }
-    public class ProviderCodeGeneratorPlugin : IProviderCodeGeneratorPlugin {
 {
-        public ProviderCodeGeneratorPlugin();

-        public virtual MethodCallCodeFragment GenerateContextOptions();

-        public virtual MethodCallCodeFragment GenerateProviderOptions();

-    }
-    public class SavedModelFiles {
 {
-        public SavedModelFiles(string contextFile, IEnumerable<string> additionalFiles);

-        public virtual IList<string> AdditionalFiles { get; }

-        public virtual string ContextFile { get; }

-    }
-    public class ScaffoldedFile {
 {
-        public ScaffoldedFile();

-        public virtual string Code { get; set; }

-        public virtual string Path { get; set; }

-    }
-    public class ScaffoldedModel {
 {
-        public ScaffoldedModel();

-        public virtual IList<ScaffoldedFile> AdditionalFiles { get; }

-        public virtual ScaffoldedFile ContextFile { get; set; }

-    }
-}
```

