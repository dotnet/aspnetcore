# Microsoft.AspNetCore.Mvc.ApplicationParts

``` diff
 namespace Microsoft.AspNetCore.Mvc.ApplicationParts {
     public abstract class ApplicationPart {
         protected ApplicationPart();
         public abstract string Name { get; }
     }
+    public sealed class ApplicationPartAttribute : Attribute {
+        public ApplicationPartAttribute(string assemblyName);
+        public string AssemblyName { get; }
+    }
     public abstract class ApplicationPartFactory {
         protected ApplicationPartFactory();
         public static ApplicationPartFactory GetApplicationPartFactory(Assembly assembly);
         public abstract IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly);
     }
     public class ApplicationPartManager {
         public ApplicationPartManager();
         public IList<ApplicationPart> ApplicationParts { get; }
         public IList<IApplicationFeatureProvider> FeatureProviders { get; }
         public void PopulateFeature<TFeature>(TFeature feature);
     }
-    public class AssemblyPart : ApplicationPart, IApplicationPartTypeProvider, ICompilationReferencesProvider {
+    public class AssemblyPart : ApplicationPart, IApplicationPartTypeProvider {
         public AssemblyPart(Assembly assembly);
         public Assembly Assembly { get; }
         public override string Name { get; }
         public IEnumerable<TypeInfo> Types { get; }
-        public IEnumerable<string> GetReferencePaths();

     }
     public class CompiledRazorAssemblyApplicationPartFactory : ApplicationPartFactory {
         public CompiledRazorAssemblyApplicationPartFactory();
         public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly);
         public static IEnumerable<ApplicationPart> GetDefaultApplicationParts(Assembly assembly);
     }
     public class CompiledRazorAssemblyPart : ApplicationPart, IRazorCompiledItemProvider {
         public CompiledRazorAssemblyPart(Assembly assembly);
         public Assembly Assembly { get; }
         IEnumerable<RazorCompiledItem> Microsoft.AspNetCore.Mvc.ApplicationParts.IRazorCompiledItemProvider.CompiledItems { get; }
         public override string Name { get; }
     }
     public class DefaultApplicationPartFactory : ApplicationPartFactory {
         public DefaultApplicationPartFactory();
         public static DefaultApplicationPartFactory Instance { get; }
         public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly);
         public static IEnumerable<ApplicationPart> GetDefaultApplicationParts(Assembly assembly);
     }
     public interface IApplicationFeatureProvider
     public interface IApplicationFeatureProvider<TFeature> : IApplicationFeatureProvider {
         void PopulateFeature(IEnumerable<ApplicationPart> parts, TFeature feature);
     }
     public interface IApplicationPartTypeProvider {
         IEnumerable<TypeInfo> Types { get; }
     }
     public interface ICompilationReferencesProvider {
         IEnumerable<string> GetReferencePaths();
     }
     public interface IRazorCompiledItemProvider {
         IEnumerable<RazorCompiledItem> CompiledItems { get; }
     }
     public class NullApplicationPartFactory : ApplicationPartFactory {
         public NullApplicationPartFactory();
         public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly);
     }
     public sealed class ProvideApplicationPartFactoryAttribute : Attribute {
         public ProvideApplicationPartFactoryAttribute(string factoryTypeName);
         public ProvideApplicationPartFactoryAttribute(Type factoryType);
         public Type GetFactoryType();
     }
     public sealed class RelatedAssemblyAttribute : Attribute {
         public RelatedAssemblyAttribute(string assemblyFileName);
         public string AssemblyFileName { get; }
         public static IReadOnlyList<Assembly> GetRelatedAssemblies(Assembly assembly, bool throwOnError);
     }
 }
```

