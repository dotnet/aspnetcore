# Microsoft.AspNetCore.Mvc.Rendering

``` diff
 namespace Microsoft.AspNetCore.Mvc.Rendering {
+    public static class HtmlHelperComponentPrerenderingExtensions {
+        public static Task<IHtmlContent> RenderComponentAsync<TComponent>(this IHtmlHelper htmlHelper) where TComponent : IComponent;
+        public static Task<IHtmlContent> RenderComponentAsync<TComponent>(this IHtmlHelper htmlHelper, object parameters) where TComponent : IComponent;
+    }
+    public static class HtmlHelperRazorComponentExtensions {
+        public static Task<IHtmlContent> RenderStaticComponentAsync<TComponent>(this IHtmlHelper htmlHelper) where TComponent : IComponent;
+        public static Task<IHtmlContent> RenderStaticComponentAsync<TComponent>(this IHtmlHelper htmlHelper, object parameters) where TComponent : IComponent;
+    }
     public interface IHtmlHelper {
-        IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct, ValueType;
+        IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct;
     }
     public interface IJsonHelper {
-        IHtmlContent Serialize(object value, JsonSerializerSettings serializerSettings);

     }
 }
```

