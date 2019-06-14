# Microsoft.AspNetCore.Razor.Language.Intermediate

``` diff
-namespace Microsoft.AspNetCore.Razor.Language.Intermediate {
 {
-    public sealed class ClassDeclarationIntermediateNode : MemberDeclarationIntermediateNode {
 {
-        public ClassDeclarationIntermediateNode();

-        public string BaseType { get; set; }

-        public override IntermediateNodeCollection Children { get; }

-        public string ClassName { get; set; }

-        public IList<string> Interfaces { get; set; }

-        public IList<string> Modifiers { get; }

-        public IList<TypeParameter> TypeParameters { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public static class CommonAnnotations {
 {
-        public static readonly object Imported;

-        public static readonly object PrimaryClass;

-        public static readonly object PrimaryMethod;

-        public static readonly object PrimaryNamespace;

-        public static class DefaultTagHelperExtension {
 {
-            public static readonly object TagHelperField;

-        }
-    }
-    public sealed class CSharpCodeAttributeValueIntermediateNode : IntermediateNode {
 {
-        public CSharpCodeAttributeValueIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string Prefix { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class CSharpCodeIntermediateNode : IntermediateNode {
 {
-        public CSharpCodeIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class CSharpExpressionAttributeValueIntermediateNode : IntermediateNode {
 {
-        public CSharpExpressionAttributeValueIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string Prefix { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class CSharpExpressionIntermediateNode : IntermediateNode {
 {
-        public CSharpExpressionIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class DirectiveIntermediateNode : IntermediateNode {
 {
-        public DirectiveIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public DirectiveDescriptor Directive { get; set; }

-        public string DirectiveName { get; set; }

-        public IEnumerable<DirectiveTokenIntermediateNode> Tokens { get; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class DirectiveTokenIntermediateNode : IntermediateNode {
 {
-        public DirectiveTokenIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string Content { get; set; }

-        public DirectiveTokenDescriptor DirectiveToken { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class DocumentIntermediateNode : IntermediateNode {
 {
-        public DocumentIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string DocumentKind { get; set; }

-        public RazorCodeGenerationOptions Options { get; set; }

-        public CodeTarget Target { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public static class DocumentIntermediateNodeExtensions {
 {
-        public static IReadOnlyList<IntermediateNodeReference> FindDirectiveReferences(this DocumentIntermediateNode node, DirectiveDescriptor directive);

-        public static ClassDeclarationIntermediateNode FindPrimaryClass(this DocumentIntermediateNode node);

-        public static MethodDeclarationIntermediateNode FindPrimaryMethod(this DocumentIntermediateNode node);

-        public static NamespaceDeclarationIntermediateNode FindPrimaryNamespace(this DocumentIntermediateNode node);

-    }
-    public abstract class ExtensionIntermediateNode : IntermediateNode {
 {
-        protected ExtensionIntermediateNode();

-        protected static void AcceptExtensionNode<TNode>(TNode node, IntermediateNodeVisitor visitor) where TNode : ExtensionIntermediateNode;

-        protected void ReportMissingCodeTargetExtension<TDependency>(CodeRenderingContext context);

-        public abstract void WriteNode(CodeTarget target, CodeRenderingContext context);

-    }
-    public sealed class FieldDeclarationIntermediateNode : MemberDeclarationIntermediateNode {
 {
-        public FieldDeclarationIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string FieldName { get; set; }

-        public string FieldType { get; set; }

-        public IList<string> Modifiers { get; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class HtmlAttributeIntermediateNode : IntermediateNode {
 {
-        public HtmlAttributeIntermediateNode();

-        public string AttributeName { get; set; }

-        public override IntermediateNodeCollection Children { get; }

-        public string Prefix { get; set; }

-        public string Suffix { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class HtmlAttributeValueIntermediateNode : IntermediateNode {
 {
-        public HtmlAttributeValueIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string Prefix { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class HtmlContentIntermediateNode : IntermediateNode {
 {
-        public HtmlContentIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public interface IExtensionIntermediateNodeVisitor<TNode> where TNode : ExtensionIntermediateNode {
 {
-        void VisitExtension(TNode node);

-    }
-    public abstract class IntermediateNode {
 {
-        protected IntermediateNode();

-        public ItemCollection Annotations { get; }

-        public abstract IntermediateNodeCollection Children { get; }

-        public RazorDiagnosticCollection Diagnostics { get; }

-        public bool HasDiagnostics { get; }

-        public Nullable<SourceSpan> Source { get; set; }

-        public abstract void Accept(IntermediateNodeVisitor visitor);

-        public virtual void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class IntermediateNodeCollection : ICollection<IntermediateNode>, IEnumerable, IEnumerable<IntermediateNode>, IList<IntermediateNode> {
 {
-        public static readonly IntermediateNodeCollection ReadOnly;

-        public IntermediateNodeCollection();

-        public int Count { get; }

-        public bool IsReadOnly { get; }

-        public IntermediateNode this[int index] { get; set; }

-        public void Add(IntermediateNode item);

-        public void Clear();

-        public bool Contains(IntermediateNode item);

-        public void CopyTo(IntermediateNode[] array, int arrayIndex);

-        public IntermediateNodeCollection.Enumerator GetEnumerator();

-        public int IndexOf(IntermediateNode item);

-        public void Insert(int index, IntermediateNode item);

-        public bool Remove(IntermediateNode item);

-        public void RemoveAt(int index);

-        IEnumerator<IntermediateNode> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNode>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public struct Enumerator : IDisposable, IEnumerator, IEnumerator<IntermediateNode> {
 {
-            public Enumerator(IntermediateNodeCollection collection);

-            public IntermediateNode Current { get; }

-            object System.Collections.IEnumerator.Current { get; }

-            public void Dispose();

-            public bool MoveNext();

-            public void Reset();

-        }
-    }
-    public static class IntermediateNodeExtensions {
 {
-        public static IReadOnlyList<TNode> FindDescendantNodes<TNode>(this IntermediateNode node) where TNode : IntermediateNode;

-        public static IReadOnlyList<RazorDiagnostic> GetAllDiagnostics(this IntermediateNode node);

-        public static bool IsImported(this IntermediateNode node);

-    }
-    public abstract class IntermediateNodeFormatter {
 {
-        protected IntermediateNodeFormatter();

-        public abstract void WriteChildren(IntermediateNodeCollection children);

-        public abstract void WriteContent(string content);

-        public abstract void WriteProperty(string key, string value);

-    }
-    public struct IntermediateNodeReference {
 {
-        public IntermediateNodeReference(IntermediateNode parent, IntermediateNode node);

-        public IntermediateNode Node { get; }

-        public IntermediateNode Parent { get; }

-        public void Deconstruct(out IntermediateNode parent, out IntermediateNode node);

-        public IntermediateNodeReference InsertAfter(IntermediateNode node);

-        public void InsertAfter(IEnumerable<IntermediateNode> nodes);

-        public IntermediateNodeReference InsertBefore(IntermediateNode node);

-        public void InsertBefore(IEnumerable<IntermediateNode> nodes);

-        public void Remove();

-        public IntermediateNodeReference Replace(IntermediateNode node);

-    }
-    public abstract class IntermediateNodeVisitor {
 {
-        protected IntermediateNodeVisitor();

-        public virtual void Visit(IntermediateNode node);

-        public virtual void VisitClassDeclaration(ClassDeclarationIntermediateNode node);

-        public virtual void VisitCSharpCode(CSharpCodeIntermediateNode node);

-        public virtual void VisitCSharpCodeAttributeValue(CSharpCodeAttributeValueIntermediateNode node);

-        public virtual void VisitCSharpExpression(CSharpExpressionIntermediateNode node);

-        public virtual void VisitCSharpExpressionAttributeValue(CSharpExpressionAttributeValueIntermediateNode node);

-        public virtual void VisitDefault(IntermediateNode node);

-        public virtual void VisitDirective(DirectiveIntermediateNode node);

-        public virtual void VisitDirectiveToken(DirectiveTokenIntermediateNode node);

-        public virtual void VisitDocument(DocumentIntermediateNode node);

-        public virtual void VisitExtension(ExtensionIntermediateNode node);

-        public virtual void VisitFieldDeclaration(FieldDeclarationIntermediateNode node);

-        public virtual void VisitHtml(HtmlContentIntermediateNode node);

-        public virtual void VisitHtmlAttribute(HtmlAttributeIntermediateNode node);

-        public virtual void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node);

-        public virtual void VisitMalformedDirective(MalformedDirectiveIntermediateNode node);

-        public virtual void VisitMethodDeclaration(MethodDeclarationIntermediateNode node);

-        public virtual void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node);

-        public virtual void VisitPropertyDeclaration(PropertyDeclarationIntermediateNode node);

-        public virtual void VisitTagHelper(TagHelperIntermediateNode node);

-        public virtual void VisitTagHelperBody(TagHelperBodyIntermediateNode node);

-        public virtual void VisitTagHelperHtmlAttribute(TagHelperHtmlAttributeIntermediateNode node);

-        public virtual void VisitTagHelperProperty(TagHelperPropertyIntermediateNode node);

-        public virtual void VisitToken(IntermediateToken node);

-        public virtual void VisitUsingDirective(UsingDirectiveIntermediateNode node);

-    }
-    public abstract class IntermediateNodeWalker : IntermediateNodeVisitor {
 {
-        protected IntermediateNodeWalker();

-        protected IReadOnlyList<IntermediateNode> Ancestors { get; }

-        protected IntermediateNode Parent { get; }

-        public override void VisitDefault(IntermediateNode node);

-    }
-    public sealed class IntermediateToken : IntermediateNode {
 {
-        public IntermediateToken();

-        public override IntermediateNodeCollection Children { get; }

-        public string Content { get; set; }

-        public bool IsCSharp { get; }

-        public bool IsHtml { get; }

-        public TokenKind Kind { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class MalformedDirectiveIntermediateNode : IntermediateNode {
 {
-        public MalformedDirectiveIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public DirectiveDescriptor Directive { get; set; }

-        public string DirectiveName { get; set; }

-        public IEnumerable<DirectiveTokenIntermediateNode> Tokens { get; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public abstract class MemberDeclarationIntermediateNode : IntermediateNode {
 {
-        protected MemberDeclarationIntermediateNode();

-    }
-    public sealed class MethodDeclarationIntermediateNode : MemberDeclarationIntermediateNode {
 {
-        public MethodDeclarationIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string MethodName { get; set; }

-        public IList<string> Modifiers { get; }

-        public IList<MethodParameter> Parameters { get; }

-        public string ReturnType { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class MethodParameter {
 {
-        public MethodParameter();

-        public IList<string> Modifiers { get; }

-        public string ParameterName { get; set; }

-        public string TypeName { get; set; }

-    }
-    public sealed class NamespaceDeclarationIntermediateNode : IntermediateNode {
 {
-        public NamespaceDeclarationIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string Content { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class PropertyDeclarationIntermediateNode : MemberDeclarationIntermediateNode {
 {
-        public PropertyDeclarationIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public IList<string> Modifiers { get; }

-        public string PropertyName { get; set; }

-        public string PropertyType { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-    }
-    public sealed class TagHelperBodyIntermediateNode : IntermediateNode {
 {
-        public TagHelperBodyIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-    }
-    public sealed class TagHelperHtmlAttributeIntermediateNode : IntermediateNode {
 {
-        public TagHelperHtmlAttributeIntermediateNode();

-        public string AttributeName { get; set; }

-        public AttributeStructure AttributeStructure { get; set; }

-        public override IntermediateNodeCollection Children { get; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class TagHelperIntermediateNode : IntermediateNode {
 {
-        public TagHelperIntermediateNode();

-        public TagHelperBodyIntermediateNode Body { get; }

-        public override IntermediateNodeCollection Children { get; }

-        public IEnumerable<TagHelperHtmlAttributeIntermediateNode> HtmlAttributes { get; }

-        public IEnumerable<TagHelperPropertyIntermediateNode> Properties { get; }

-        public IList<TagHelperDescriptor> TagHelpers { get; }

-        public TagMode TagMode { get; set; }

-        public string TagName { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public sealed class TagHelperPropertyIntermediateNode : IntermediateNode {
 {
-        public TagHelperPropertyIntermediateNode();

-        public string AttributeName { get; set; }

-        public AttributeStructure AttributeStructure { get; set; }

-        public BoundAttributeDescriptor BoundAttribute { get; set; }

-        public override IntermediateNodeCollection Children { get; }

-        public bool IsIndexerNameMatch { get; set; }

-        public TagHelperDescriptor TagHelper { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-    public enum TokenKind {
 {
-        CSharp = 1,

-        Html = 2,

-        Unknown = 0,

-    }
-    public sealed class TypeParameter {
 {
-        public TypeParameter();

-        public string ParameterName { get; set; }

-    }
-    public sealed class UsingDirectiveIntermediateNode : IntermediateNode {
 {
-        public UsingDirectiveIntermediateNode();

-        public override IntermediateNodeCollection Children { get; }

-        public string Content { get; set; }

-        public override void Accept(IntermediateNodeVisitor visitor);

-        public override void FormatNode(IntermediateNodeFormatter formatter);

-    }
-}
```

