# Microsoft.CodeAnalysis.Razor

``` diff
-namespace Microsoft.CodeAnalysis.Razor {
 {
-    public sealed class CompilationTagHelperFeature : RazorEngineFeatureBase, IRazorEngineFeature, IRazorFeature, ITagHelperFeature {
 {
-        public CompilationTagHelperFeature();

-        public IReadOnlyList<TagHelperDescriptor> GetDescriptors();

-        protected override void OnInitialized();

-    }
-    public sealed class DefaultMetadataReferenceFeature : RazorEngineFeatureBase, IMetadataReferenceFeature, IRazorEngineFeature, IRazorFeature {
 {
-        public DefaultMetadataReferenceFeature();

-        public IReadOnlyList<MetadataReference> References { get; set; }

-    }
-    public sealed class DefaultTagHelperDescriptorProvider : RazorEngineFeatureBase, IRazorEngineFeature, IRazorFeature, ITagHelperDescriptorProvider {
 {
-        public DefaultTagHelperDescriptorProvider();

-        public bool DesignTime { get; set; }

-        public int Order { get; set; }

-        public void Execute(TagHelperDescriptorProviderContext context);

-    }
-    public interface IMetadataReferenceFeature : IRazorEngineFeature, IRazorFeature {
 {
-        IReadOnlyList<MetadataReference> References { get; }

-    }
-    public static class RazorLanguage {
 {
-        public const string ContentType = "RazorCSharp";

-        public const string CoreContentType = "RazorCoreCSharp";

-        public const string Name = "Razor";

-    }
-    public static class TagHelperDescriptorProviderContextExtensions {
 {
-        public static Compilation GetCompilation(this TagHelperDescriptorProviderContext context);

-        public static void SetCompilation(this TagHelperDescriptorProviderContext context, Compilation compilation);

-    }
-}
```

