# Microsoft.AspNetCore.Razor.Hosting

``` diff
 namespace Microsoft.AspNetCore.Razor.Hosting {
     public interface IRazorSourceChecksumMetadata {
         string Checksum { get; }
         string ChecksumAlgorithm { get; }
         string Identifier { get; }
     }
     public abstract class RazorCompiledItem {
         protected RazorCompiledItem();
         public abstract string Identifier { get; }
         public abstract string Kind { get; }
         public abstract IReadOnlyList<object> Metadata { get; }
         public abstract Type Type { get; }
     }
     public sealed class RazorCompiledItemAttribute : Attribute {
         public RazorCompiledItemAttribute(Type type, string kind, string identifier);
         public string Identifier { get; }
         public string Kind { get; }
         public Type Type { get; }
     }
     public static class RazorCompiledItemExtensions {
         public static IReadOnlyList<IRazorSourceChecksumMetadata> GetChecksumMetadata(this RazorCompiledItem item);
     }
     public class RazorCompiledItemLoader {
         public RazorCompiledItemLoader();
         protected virtual RazorCompiledItem CreateItem(RazorCompiledItemAttribute attribute);
         protected IEnumerable<RazorCompiledItemAttribute> LoadAttributes(Assembly assembly);
         public virtual IReadOnlyList<RazorCompiledItem> LoadItems(Assembly assembly);
     }
     public sealed class RazorCompiledItemMetadataAttribute : Attribute {
         public RazorCompiledItemMetadataAttribute(string key, string value);
         public string Key { get; }
         public string Value { get; }
     }
     public sealed class RazorConfigurationNameAttribute : Attribute {
         public RazorConfigurationNameAttribute(string configurationName);
         public string ConfigurationName { get; }
     }
     public sealed class RazorExtensionAssemblyNameAttribute : Attribute {
         public RazorExtensionAssemblyNameAttribute(string extensionName, string assemblyName);
         public string AssemblyName { get; }
         public string ExtensionName { get; }
     }
     public sealed class RazorLanguageVersionAttribute : Attribute {
         public RazorLanguageVersionAttribute(string languageVersion);
         public string LanguageVersion { get; }
     }
     public sealed class RazorSourceChecksumAttribute : Attribute, IRazorSourceChecksumMetadata {
         public RazorSourceChecksumAttribute(string checksumAlgorithm, string checksum, string identifier);
         public string Checksum { get; }
         public string ChecksumAlgorithm { get; }
         public string Identifier { get; }
     }
 }
```

