# Microsoft.AspNetCore.Razor.Language.Extensions

``` diff
-namespace Microsoft.AspNetCore.Razor.Language.Extensions {
 {
-    public sealed class DefaultTagHelperBodyIntermediateNode : ExtensionIntermediateNode {
 {
-        public DefaultTagHelperBodyIntermediateNode();

-        public DefaultTagHelperBodyIntermediateNode(TagHelperBodyIntermediateNode bodyNode);

-        public override IntermediateNodeCollection Children { get; }

-        public TagMode TagMode { get; set; }

-        public string TagName { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void WriteNode(CodeTarget target, CodeRenderingContext context);

-    }
-    public sealed class DefaultTagHelperCreateIntermediateNode : ExtensionIntermediateNode {
 {
-        public DefaultTagHelperCreateIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string FieldName { get; set; }

-        public TagHelperDescriptor TagHelper { get; set; }

-        public string TypeName { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-        public override void WriteNode(CodeTarget target, CodeRenderingContext context);

-    }
-    public sealed class DefaultTagHelperExecuteIntermediateNode : ExtensionIntermediateNode {
 {
-        public DefaultTagHelperExecuteIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void WriteNode(CodeTarget target, CodeRenderingContext context);

-    }
-    public sealed class DefaultTagHelperHtmlAttributeIntermediateNode : ExtensionIntermediateNode {
 {
-        public DefaultTagHelperHtmlAttributeIntermediateNode();

-        public DefaultTagHelperHtmlAttributeIntermediateNode(TagHelperHtmlAttributeIntermediateNode htmlAttributeNode);

-        public string AttributeName { get; set; }

-        public AttributeStructure AttributeStructure { get; set; }

-        public override IntermediateNodeCollection Children { get; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-        public override void WriteNode(CodeTarget target, CodeRenderingContext context);

-    }
-    public sealed class DefaultTagHelperPropertyIntermediateNode : ExtensionIntermediateNode {
 {
-        public DefaultTagHelperPropertyIntermediateNode();

-        public DefaultTagHelperPropertyIntermediateNode(TagHelperPropertyIntermediateNode propertyNode);

-        public string AttributeName { get; set; }

-        public AttributeStructure AttributeStructure { get; set; }

-        public BoundAttributeDescriptor BoundAttribute { get; set; }

-        public override IntermediateNodeCollection Children { get; }

-        public string FieldName { get; set; }

-        public bool IsIndexerNameMatch { get; set; }

-        public string PropertyName { get; set; }

-        public TagHelperDescriptor TagHelper { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-        public override void WriteNode(CodeTarget target, CodeRenderingContext context);

-    }
-    public sealed class DefaultTagHelperRuntimeIntermediateNode : ExtensionIntermediateNode {
 {
-        public DefaultTagHelperRuntimeIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void WriteNode(CodeTarget target, CodeRenderingContext context);

-    }
-    public static class FunctionsDirective {
 {
-        public static readonly DirectiveDescriptor Directive;

-        public static void Register(IRazorEngineBuilder builder);

-        public static void Register(RazorProjectEngineBuilder builder);

-    }
-    public sealed class FunctionsDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass, IRazorEngineFeature, IRazorFeature {
 {
-        public FunctionsDirectivePass();

-        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-    public interface IDefaultTagHelperTargetExtension : ICodeTargetExtension {
 {
-        void WriteTagHelperBody(CodeRenderingContext context, DefaultTagHelperBodyIntermediateNode node);

-        void WriteTagHelperCreate(CodeRenderingContext context, DefaultTagHelperCreateIntermediateNode node);

-        void WriteTagHelperExecute(CodeRenderingContext context, DefaultTagHelperExecuteIntermediateNode node);

-        void WriteTagHelperHtmlAttribute(CodeRenderingContext context, DefaultTagHelperHtmlAttributeIntermediateNode node);

-        void WriteTagHelperProperty(CodeRenderingContext context, DefaultTagHelperPropertyIntermediateNode node);

-        void WriteTagHelperRuntime(CodeRenderingContext context, DefaultTagHelperRuntimeIntermediateNode node);

-    }
-    public static class InheritsDirective {
 {
-        public static readonly DirectiveDescriptor Directive;

-        public static void Register(IRazorEngineBuilder builder);

-        public static void Register(RazorProjectEngineBuilder builder);

-    }
-    public sealed class InheritsDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass, IRazorEngineFeature, IRazorFeature {
 {
-        public InheritsDirectivePass();

-        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-    public interface ISectionTargetExtension : ICodeTargetExtension {
 {
-        void WriteSection(CodeRenderingContext context, SectionIntermediateNode node);

-    }
-    public interface ITemplateTargetExtension : ICodeTargetExtension {
 {
-        void WriteTemplate(CodeRenderingContext context, TemplateIntermediateNode node);

-    }
-    public class RazorCompiledItemMetadataAttributeIntermediateNode : ExtensionIntermediateNode {
 {
-        public RazorCompiledItemMetadataAttributeIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string Key { get; set; }

-        public string Value { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-        public override void WriteNode(CodeTarget target, CodeRenderingContext context);

-    }
-    public static class SectionDirective {
 {
-        public static readonly DirectiveDescriptor Directive;

-        public static void Register(IRazorEngineBuilder builder);

-        public static void Register(RazorProjectEngineBuilder builder);

-    }
-    public sealed class SectionDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass, IRazorEngineFeature, IRazorFeature {
 {
-        public SectionDirectivePass();

-        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-    public sealed class SectionIntermediateNode : ExtensionIntermediateNode {
 {
-        public SectionIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string SectionName { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-        public override void WriteNode(CodeTarget target, CodeRenderingContext context);

-    }
-    public sealed class SectionTargetExtension : ICodeTargetExtension, ISectionTargetExtension {
 {
-        public static readonly string DefaultSectionMethodName;

-        public SectionTargetExtension();

-        public string SectionMethodName { get; set; }

-        public void WriteSection(CodeRenderingContext context, SectionIntermediateNode node);

-    }
-    public sealed class TemplateIntermediateNode : ExtensionIntermediateNode {
 {
-        public TemplateIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void WriteNode(CodeTarget target, CodeRenderingContext context);

-    }
-    public sealed class TemplateTargetExtension : ICodeTargetExtension, ITemplateTargetExtension {
 {
-        public static readonly string DefaultTemplateTypeName;

-        public TemplateTargetExtension();

-        public string TemplateTypeName { get; set; }

-        public void WriteTemplate(CodeRenderingContext context, TemplateIntermediateNode node);

-    }
-}
```

