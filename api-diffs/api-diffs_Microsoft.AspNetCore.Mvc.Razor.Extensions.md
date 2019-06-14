# Microsoft.AspNetCore.Mvc.Razor.Extensions

``` diff
-namespace Microsoft.AspNetCore.Mvc.Razor.Extensions {
 {
-    public class AssemblyAttributeInjectionPass : IntermediateNodePassBase, IRazorEngineFeature, IRazorFeature, IRazorOptimizationPass {
 {
-        public AssemblyAttributeInjectionPass();

-        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-    public interface IInjectTargetExtension : ICodeTargetExtension {
 {
-        void WriteInjectProperty(CodeRenderingContext context, InjectIntermediateNode node);

-    }
-    public static class InjectDirective {
 {
-        public static readonly DirectiveDescriptor Directive;

-        public static IRazorEngineBuilder Register(IRazorEngineBuilder builder);

-        public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder);

-    }
-    public class InjectIntermediateNode : ExtensionIntermediateNode {
 {
-        public InjectIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string MemberName { get; set; }

-        public string TypeName { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-        public override void WriteNode(CodeTarget target, CodeRenderingContext context);

-    }
-    public class InjectTargetExtension : ICodeTargetExtension, IInjectTargetExtension {
 {
-        public InjectTargetExtension();

-        public void WriteInjectProperty(CodeRenderingContext context, InjectIntermediateNode node);

-    }
-    public class InstrumentationPass : IntermediateNodePassBase, IRazorEngineFeature, IRazorFeature, IRazorOptimizationPass {
 {
-        public InstrumentationPass();

-        public override int Order { get; }

-        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-    public interface IViewComponentTagHelperTargetExtension : ICodeTargetExtension {
 {
-        void WriteViewComponentTagHelper(CodeRenderingContext context, ViewComponentTagHelperIntermediateNode node);

-    }
-    public static class ModelDirective {
 {
-        public static readonly DirectiveDescriptor Directive;

-        public static string GetModelType(DocumentIntermediateNode document);

-        public static IRazorEngineBuilder Register(IRazorEngineBuilder builder);

-        public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder);

-    }
-    public class ModelExpressionPass : IntermediateNodePassBase, IRazorEngineFeature, IRazorFeature, IRazorOptimizationPass {
 {
-        public ModelExpressionPass();

-        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-    public class MvcRazorTemplateEngine : RazorTemplateEngine {
 {
-        public MvcRazorTemplateEngine(RazorEngine engine, RazorProject project);

-        public override RazorCodeDocument CreateCodeDocument(RazorProjectItem projectItem);

-    }
-    public class MvcViewDocumentClassifierPass : DocumentClassifierPassBase {
 {
-        public static readonly string MvcViewDocumentKind;

-        public MvcViewDocumentClassifierPass();

-        protected override string DocumentKind { get; }

-        protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-        protected override void OnDocumentStructureCreated(RazorCodeDocument codeDocument, NamespaceDeclarationIntermediateNode @namespace, ClassDeclarationIntermediateNode @class, MethodDeclarationIntermediateNode method);

-    }
-    public static class NamespaceDirective {
 {
-        public static readonly DirectiveDescriptor Directive;

-        public static void Register(IRazorEngineBuilder builder);

-        public static void Register(RazorProjectEngineBuilder builder);

-    }
-    public class PageDirective {
 {
-        public static readonly DirectiveDescriptor Directive;

-        public IntermediateNode DirectiveNode { get; }

-        public string RouteTemplate { get; }

-        public static IRazorEngineBuilder Register(IRazorEngineBuilder builder);

-        public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder);

-        public static bool TryGetPageDirective(DocumentIntermediateNode documentNode, out PageDirective pageDirective);

-    }
-    public class PagesPropertyInjectionPass : IntermediateNodePassBase, IRazorEngineFeature, IRazorFeature, IRazorOptimizationPass {
 {
-        public PagesPropertyInjectionPass();

-        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-    public static class RazorExtensions {
 {
-        public static void Register(IRazorEngineBuilder builder);

-        public static void Register(RazorProjectEngineBuilder builder);

-    }
-    public class RazorPageDocumentClassifierPass : DocumentClassifierPassBase {
 {
-        public static readonly string RazorPageDocumentKind;

-        public static readonly string RouteTemplateKey;

-        public RazorPageDocumentClassifierPass();

-        protected override string DocumentKind { get; }

-        protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-        protected override void OnDocumentStructureCreated(RazorCodeDocument codeDocument, NamespaceDeclarationIntermediateNode @namespace, ClassDeclarationIntermediateNode @class, MethodDeclarationIntermediateNode method);

-    }
-    public static class TagHelperDescriptorExtensions {
 {
-        public static string GetViewComponentName(this TagHelperDescriptor tagHelper);

-        public static bool IsViewComponentKind(this TagHelperDescriptor tagHelper);

-    }
-    public static class ViewComponentTagHelperConventions {
 {
-        public static readonly string Kind;

-    }
-    public sealed class ViewComponentTagHelperDescriptorProvider : RazorEngineFeatureBase, IRazorEngineFeature, IRazorFeature, ITagHelperDescriptorProvider {
 {
-        public ViewComponentTagHelperDescriptorProvider();

-        public int Order { get; set; }

-        public void Execute(TagHelperDescriptorProviderContext context);

-    }
-    public sealed class ViewComponentTagHelperIntermediateNode : ExtensionIntermediateNode {
 {
-        public ViewComponentTagHelperIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string ClassName { get; set; }

-        public TagHelperDescriptor TagHelper { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-        public override void WriteNode(CodeTarget target, CodeRenderingContext context);

-    }
-    public static class ViewComponentTagHelperMetadata {
 {
-        public static readonly string Name;

-    }
-    public class ViewComponentTagHelperPass : IntermediateNodePassBase, IRazorEngineFeature, IRazorFeature, IRazorOptimizationPass {
 {
-        public ViewComponentTagHelperPass();

-        public override int Order { get; }

-        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-}
```

