# Microsoft.AspNetCore.DiagnosticsViewPage.Views

``` diff
-namespace Microsoft.AspNetCore.DiagnosticsViewPage.Views {
 {
-    public class AttributeValue {
 {
-        public AttributeValue(string prefix, object value, bool literal);

-        public bool Literal { get; }

-        public string Prefix { get; }

-        public object Value { get; }

-        public static AttributeValue FromTuple(Tuple<string, object, bool> value);

-        public static AttributeValue FromTuple(Tuple<string, string, bool> value);

-        public static implicit operator AttributeValue (Tuple<string, object, bool> value);

-    }
-    public abstract class BaseView {
 {
-        protected BaseView();

-        protected HttpContext Context { get; private set; }

-        protected HtmlEncoder HtmlEncoder { get; set; }

-        protected JavaScriptEncoder JavaScriptEncoder { get; set; }

-        protected StreamWriter Output { get; private set; }

-        protected HttpRequest Request { get; private set; }

-        protected HttpResponse Response { get; private set; }

-        protected UrlEncoder UrlEncoder { get; set; }

-        protected void BeginWriteAttribute(string name, string begining, int startPosition, string ending, int endPosition, int thingy);

-        protected void EndWriteAttribute();

-        public abstract Task ExecuteAsync();

-        public Task ExecuteAsync(HttpContext context);

-        protected string HtmlEncodeAndReplaceLineBreaks(string input);

-        protected void Write(HelperResult result);

-        protected void Write(object value);

-        protected void Write(string value);

-        protected void WriteAttributeTo(TextWriter writer, string name, string leader, string trailer, params AttributeValue[] values);

-        protected void WriteAttributeValue(string thingy, int startPostion, object value, int endValue, int dealyo, bool yesno);

-        protected void WriteLiteral(object value);

-        protected void WriteLiteral(string value);

-        protected void WriteLiteralTo(TextWriter writer, object value);

-        protected void WriteLiteralTo(TextWriter writer, string value);

-        protected void WriteTo(TextWriter writer, object value);

-        protected void WriteTo(TextWriter writer, string value);

-    }
-    public class HelperResult {
 {
-        public HelperResult(Action<TextWriter> action);

-        public Action<TextWriter> WriteAction { get; }

-        public void WriteTo(TextWriter writer);

-    }
-}
```

