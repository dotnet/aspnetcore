# Microsoft.AspNetCore.Razor.Language.CodeGeneration

``` diff
-namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration {
 {
-    public abstract class CodeRenderingContext {
 {
-        protected CodeRenderingContext();

-        public abstract IEnumerable<IntermediateNode> Ancestors { get; }

-        public abstract CodeWriter CodeWriter { get; }

-        public abstract RazorDiagnosticCollection Diagnostics { get; }

-        public abstract string DocumentKind { get; }

-        public abstract ItemCollection Items { get; }

-        public abstract IntermediateNodeWriter NodeWriter { get; }

-        public abstract RazorCodeGenerationOptions Options { get; }

-        public abstract IntermediateNode Parent { get; }

-        public abstract RazorSourceDocument SourceDocument { get; }

-        public abstract void AddSourceMappingFor(IntermediateNode node);

-        public abstract void RenderChildren(IntermediateNode node);

-        public abstract void RenderChildren(IntermediateNode node, IntermediateNodeWriter writer);

-        public abstract void RenderNode(IntermediateNode node);

-        public abstract void RenderNode(IntermediateNode node, IntermediateNodeWriter writer);

-    }
-    public abstract class CodeTarget {
 {
-        protected CodeTarget();

-        public static CodeTarget CreateDefault(RazorCodeDocument codeDocument, RazorCodeGenerationOptions options);

-        public static CodeTarget CreateDefault(RazorCodeDocument codeDocument, RazorCodeGenerationOptions options, Action<CodeTargetBuilder> configure);

-        public static CodeTarget CreateEmpty(RazorCodeDocument codeDocument, RazorCodeGenerationOptions options, Action<CodeTargetBuilder> configure);

-        public abstract IntermediateNodeWriter CreateNodeWriter();

-        public abstract TExtension GetExtension<TExtension>() where TExtension : class, ICodeTargetExtension;

-        public abstract bool HasExtension<TExtension>() where TExtension : class, ICodeTargetExtension;

-    }
-    public abstract class CodeTargetBuilder {
 {
-        protected CodeTargetBuilder();

-        public abstract RazorCodeDocument CodeDocument { get; }

-        public abstract RazorCodeGenerationOptions Options { get; }

-        public abstract ICollection<ICodeTargetExtension> TargetExtensions { get; }

-        public abstract CodeTarget Build();

-    }
-    public sealed class CodeWriter {
 {
-        public CodeWriter();

-        public int CurrentIndent { get; set; }

-        public int Length { get; }

-        public SourceLocation Location { get; }

-        public string NewLine { get; set; }

-        public char this[int index] { get; }

-        public string GenerateCode();

-        public CodeWriter Write(string value);

-        public CodeWriter Write(string value, int startIndex, int count);

-        public CodeWriter WriteLine();

-        public CodeWriter WriteLine(string value);

-    }
-    public class DesignTimeNodeWriter : IntermediateNodeWriter {
 {
-        public DesignTimeNodeWriter();

-        public override void BeginWriterScope(CodeRenderingContext context, string writer);

-        public override void EndWriterScope(CodeRenderingContext context);

-        public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node);

-        public override void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node);

-        public override void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node);

-        public override void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node);

-        public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node);

-        public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node);

-        public override void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node);

-        public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node);

-    }
-    public abstract class DocumentWriter {
 {
-        protected DocumentWriter();

-        public DocumentWriter Create(CodeTarget codeTarget, RazorCodeGenerationOptions options);

-        public static DocumentWriter CreateDefault(CodeTarget codeTarget, RazorCodeGenerationOptions options);

-        public abstract RazorCSharpDocument WriteDocument(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);

-    }
-    public interface ICodeTargetExtension

-    public abstract class IntermediateNodeWriter {
 {
-        protected IntermediateNodeWriter();

-        public abstract void BeginWriterScope(CodeRenderingContext context, string writer);

-        public abstract void EndWriterScope(CodeRenderingContext context);

-        public abstract void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node);

-        public abstract void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node);

-        public abstract void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node);

-        public abstract void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node);

-        public abstract void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node);

-        public abstract void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node);

-        public abstract void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node);

-        public abstract void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node);

-    }
-    public class RuntimeNodeWriter : IntermediateNodeWriter {
 {
-        public RuntimeNodeWriter();

-        public virtual string BeginWriteAttributeMethod { get; set; }

-        public virtual string EndWriteAttributeMethod { get; set; }

-        public virtual string PopWriterMethod { get; set; }

-        public virtual string PushWriterMethod { get; set; }

-        public string TemplateTypeName { get; set; }

-        public virtual string WriteAttributeValueMethod { get; set; }

-        public virtual string WriteCSharpExpressionMethod { get; set; }

-        public virtual string WriteHtmlContentMethod { get; set; }

-        public override void BeginWriterScope(CodeRenderingContext context, string writer);

-        public override void EndWriterScope(CodeRenderingContext context);

-        public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node);

-        public override void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node);

-        public override void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node);

-        public override void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node);

-        public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node);

-        public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node);

-        public override void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node);

-        public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node);

-    }
-}
```

