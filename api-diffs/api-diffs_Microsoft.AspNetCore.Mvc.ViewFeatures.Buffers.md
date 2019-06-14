# Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers

``` diff
+namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers {
+    public interface IViewBufferScope {
+        TextWriter CreateWriter(TextWriter writer);
+        ViewBufferValue[] GetPage(int pageSize);
+        void ReturnSegment(ViewBufferValue[] segment);
+    }
+    public readonly struct ViewBufferValue {
+        public ViewBufferValue(IHtmlContent content);
+        public ViewBufferValue(string value);
+        public object Value { get; }
+    }
+}
```

