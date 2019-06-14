# Microsoft.AspNetCore.Razor.TagHelpers

``` diff
 namespace Microsoft.AspNetCore.Razor.TagHelpers {
-    public class NullHtmlEncoder : HtmlEncoder {
+    public sealed class NullHtmlEncoder : HtmlEncoder {
-        protected NullHtmlEncoder();

-        public static new NullHtmlEncoder Default { get; }
+        public static NullHtmlEncoder Default { get; }
     }
-    public class RestrictChildrenAttribute : Attribute
+    public sealed class RestrictChildrenAttribute : Attribute
 }
```

