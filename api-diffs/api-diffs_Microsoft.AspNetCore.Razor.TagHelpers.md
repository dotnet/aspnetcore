# Microsoft.AspNetCore.Razor.TagHelpers

``` diff
 namespace Microsoft.AspNetCore.Razor.TagHelpers {
     public class DefaultTagHelperContent : TagHelperContent {
         public DefaultTagHelperContent();
         public override bool IsEmptyOrWhiteSpace { get; }
         public override bool IsModified { get; }
         public override TagHelperContent Append(string unencoded);
         public override TagHelperContent AppendHtml(IHtmlContent htmlContent);
         public override TagHelperContent AppendHtml(string encoded);
         public override TagHelperContent Clear();
         public override void CopyTo(IHtmlContentBuilder destination);
         public override string GetContent();
         public override string GetContent(HtmlEncoder encoder);
         public override void MoveTo(IHtmlContentBuilder destination);
         public override void Reinitialize();
         public override void WriteTo(TextWriter writer, HtmlEncoder encoder);
     }
     public sealed class HtmlAttributeNameAttribute : Attribute {
         public HtmlAttributeNameAttribute();
         public HtmlAttributeNameAttribute(string name);
         public string DictionaryAttributePrefix { get; set; }
         public bool DictionaryAttributePrefixSet { get; private set; }
         public string Name { get; }
     }
     public sealed class HtmlAttributeNotBoundAttribute : Attribute {
         public HtmlAttributeNotBoundAttribute();
     }
     public enum HtmlAttributeValueStyle {
         DoubleQuotes = 0,
         Minimized = 3,
         NoQuotes = 2,
         SingleQuotes = 1,
     }
     public sealed class HtmlTargetElementAttribute : Attribute {
         public const string ElementCatchAllTarget = "*";
         public HtmlTargetElementAttribute();
         public HtmlTargetElementAttribute(string tag);
         public string Attributes { get; set; }
         public string ParentTag { get; set; }
         public string Tag { get; }
         public TagStructure TagStructure { get; set; }
     }
     public interface ITagHelper : ITagHelperComponent
     public interface ITagHelperComponent {
         int Order { get; }
         void Init(TagHelperContext context);
         Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
     }
-    public class NullHtmlEncoder : HtmlEncoder {
+    public sealed class NullHtmlEncoder : HtmlEncoder {
-        protected NullHtmlEncoder();

-        public static new NullHtmlEncoder Default { get; }
+        public static NullHtmlEncoder Default { get; }
         public override int MaxOutputCharactersPerInputCharacter { get; }
         public override void Encode(TextWriter output, char[] value, int startIndex, int characterCount);
         public override void Encode(TextWriter output, string value, int startIndex, int characterCount);
         public override string Encode(string value);
         public unsafe override int FindFirstCharacterToEncode(char* text, int textLength);
         public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten);
         public override bool WillEncode(int unicodeScalar);
     }
     public sealed class OutputElementHintAttribute : Attribute {
         public OutputElementHintAttribute(string outputElement);
         public string OutputElement { get; }
     }
     public abstract class ReadOnlyTagHelperAttributeList : ReadOnlyCollection<TagHelperAttribute> {
         protected ReadOnlyTagHelperAttributeList();
         public ReadOnlyTagHelperAttributeList(IList<TagHelperAttribute> attributes);
         public TagHelperAttribute this[string name] { get; }
         public bool ContainsName(string name);
         public int IndexOfName(string name);
         protected static bool NameEquals(string name, TagHelperAttribute attribute);
         public bool TryGetAttribute(string name, out TagHelperAttribute attribute);
         public bool TryGetAttributes(string name, out IReadOnlyList<TagHelperAttribute> attributes);
     }
-    public class RestrictChildrenAttribute : Attribute {
+    public sealed class RestrictChildrenAttribute : Attribute {
         public RestrictChildrenAttribute(string childTag, params string[] childTags);
         public IEnumerable<string> ChildTags { get; }
     }
     public abstract class TagHelper : ITagHelper, ITagHelperComponent {
         protected TagHelper();
         public virtual int Order { get; }
         public virtual void Init(TagHelperContext context);
         public virtual void Process(TagHelperContext context, TagHelperOutput output);
         public virtual Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
     }
     public class TagHelperAttribute : IHtmlContent, IHtmlContentContainer {
         public TagHelperAttribute(string name);
         public TagHelperAttribute(string name, object value);
         public TagHelperAttribute(string name, object value, HtmlAttributeValueStyle valueStyle);
         public string Name { get; }
         public object Value { get; }
         public HtmlAttributeValueStyle ValueStyle { get; }
         public void CopyTo(IHtmlContentBuilder destination);
         public bool Equals(TagHelperAttribute other);
         public override bool Equals(object obj);
         public override int GetHashCode();
         public void MoveTo(IHtmlContentBuilder destination);
         public void WriteTo(TextWriter writer, HtmlEncoder encoder);
     }
     public class TagHelperAttributeList : ReadOnlyTagHelperAttributeList, ICollection<TagHelperAttribute>, IEnumerable, IEnumerable<TagHelperAttribute>, IList<TagHelperAttribute> {
         public TagHelperAttributeList();
         public TagHelperAttributeList(IEnumerable<TagHelperAttribute> attributes);
         public TagHelperAttributeList(List<TagHelperAttribute> attributes);
         bool System.Collections.Generic.ICollection<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute>.IsReadOnly { get; }
         public TagHelperAttribute this[int index] { get; set; }
         public void Add(TagHelperAttribute attribute);
         public void Add(string name, object value);
         public void Clear();
         public void Insert(int index, TagHelperAttribute attribute);
         public bool Remove(TagHelperAttribute attribute);
         public bool RemoveAll(string name);
         public void RemoveAt(int index);
         public void SetAttribute(TagHelperAttribute attribute);
         public void SetAttribute(string name, object value);
     }
     public abstract class TagHelperComponent : ITagHelperComponent {
         protected TagHelperComponent();
         public virtual int Order { get; }
         public virtual void Init(TagHelperContext context);
         public virtual void Process(TagHelperContext context, TagHelperOutput output);
         public virtual Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
     }
     public abstract class TagHelperContent : IHtmlContent, IHtmlContentBuilder, IHtmlContentContainer {
         protected TagHelperContent();
         public abstract bool IsEmptyOrWhiteSpace { get; }
         public abstract bool IsModified { get; }
         public abstract TagHelperContent Append(string unencoded);
         public TagHelperContent AppendFormat(IFormatProvider provider, string format, params object[] args);
         public TagHelperContent AppendFormat(string format, params object[] args);
         public abstract TagHelperContent AppendHtml(IHtmlContent htmlContent);
         public abstract TagHelperContent AppendHtml(string encoded);
         public abstract TagHelperContent Clear();
         public abstract void CopyTo(IHtmlContentBuilder destination);
         public abstract string GetContent();
         public abstract string GetContent(HtmlEncoder encoder);
         IHtmlContentBuilder Microsoft.AspNetCore.Html.IHtmlContentBuilder.Append(string unencoded);
         IHtmlContentBuilder Microsoft.AspNetCore.Html.IHtmlContentBuilder.AppendHtml(IHtmlContent content);
         IHtmlContentBuilder Microsoft.AspNetCore.Html.IHtmlContentBuilder.AppendHtml(string encoded);
         IHtmlContentBuilder Microsoft.AspNetCore.Html.IHtmlContentBuilder.Clear();
         public abstract void MoveTo(IHtmlContentBuilder destination);
         public abstract void Reinitialize();
         public TagHelperContent SetContent(string unencoded);
         public TagHelperContent SetHtmlContent(IHtmlContent htmlContent);
         public TagHelperContent SetHtmlContent(string encoded);
         public abstract void WriteTo(TextWriter writer, HtmlEncoder encoder);
     }
     public class TagHelperContext {
         public TagHelperContext(TagHelperAttributeList allAttributes, IDictionary<object, object> items, string uniqueId);
         public TagHelperContext(string tagName, TagHelperAttributeList allAttributes, IDictionary<object, object> items, string uniqueId);
         public ReadOnlyTagHelperAttributeList AllAttributes { get; }
         public IDictionary<object, object> Items { get; private set; }
         public string TagName { get; private set; }
         public string UniqueId { get; private set; }
         public void Reinitialize(IDictionary<object, object> items, string uniqueId);
         public void Reinitialize(string tagName, IDictionary<object, object> items, string uniqueId);
     }
     public class TagHelperOutput : IHtmlContent, IHtmlContentContainer {
         public TagHelperOutput(string tagName, TagHelperAttributeList attributes, Func<bool, HtmlEncoder, Task<TagHelperContent>> getChildContentAsync);
         public TagHelperAttributeList Attributes { get; }
         public TagHelperContent Content { get; set; }
         public bool IsContentModified { get; }
         public TagHelperContent PostContent { get; }
         public TagHelperContent PostElement { get; }
         public TagHelperContent PreContent { get; }
         public TagHelperContent PreElement { get; }
         public TagMode TagMode { get; set; }
         public string TagName { get; set; }
         public Task<TagHelperContent> GetChildContentAsync();
         public Task<TagHelperContent> GetChildContentAsync(bool useCachedResult);
         public Task<TagHelperContent> GetChildContentAsync(bool useCachedResult, HtmlEncoder encoder);
         public Task<TagHelperContent> GetChildContentAsync(HtmlEncoder encoder);
         void Microsoft.AspNetCore.Html.IHtmlContentContainer.CopyTo(IHtmlContentBuilder destination);
         void Microsoft.AspNetCore.Html.IHtmlContentContainer.MoveTo(IHtmlContentBuilder destination);
         public void Reinitialize(string tagName, TagMode tagMode);
         public void SuppressOutput();
         public void WriteTo(TextWriter writer, HtmlEncoder encoder);
     }
     public enum TagMode {
         SelfClosing = 1,
         StartTagAndEndTag = 0,
         StartTagOnly = 2,
     }
     public enum TagStructure {
         NormalOrSelfClosing = 1,
         Unspecified = 0,
         WithoutEndTag = 2,
     }
 }
```

