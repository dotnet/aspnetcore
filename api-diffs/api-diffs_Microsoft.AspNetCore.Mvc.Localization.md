# Microsoft.AspNetCore.Mvc.Localization

``` diff
 namespace Microsoft.AspNetCore.Mvc.Localization {
     public class HtmlLocalizer : IHtmlLocalizer {
         public HtmlLocalizer(IStringLocalizer localizer);
         public virtual LocalizedHtmlString this[string name, params object[] arguments] { get; }
         public virtual LocalizedHtmlString this[string name] { get; }
         public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures);
         public virtual LocalizedString GetString(string name);
         public virtual LocalizedString GetString(string name, params object[] arguments);
         protected virtual LocalizedHtmlString ToHtmlString(LocalizedString result);
         protected virtual LocalizedHtmlString ToHtmlString(LocalizedString result, object[] arguments);
         public virtual IHtmlLocalizer WithCulture(CultureInfo culture);
     }
     public class HtmlLocalizer<TResource> : IHtmlLocalizer, IHtmlLocalizer<TResource> {
         public HtmlLocalizer(IHtmlLocalizerFactory factory);
         public virtual LocalizedHtmlString this[string name, params object[] arguments] { get; }
         public virtual LocalizedHtmlString this[string name] { get; }
         public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures);
         public virtual LocalizedString GetString(string name);
         public virtual LocalizedString GetString(string name, params object[] arguments);
         public virtual IHtmlLocalizer WithCulture(CultureInfo culture);
     }
     public static class HtmlLocalizerExtensions {
         public static IEnumerable<LocalizedString> GetAllStrings(this IHtmlLocalizer htmlLocalizer);
         public static LocalizedHtmlString GetHtml(this IHtmlLocalizer htmlLocalizer, string name);
         public static LocalizedHtmlString GetHtml(this IHtmlLocalizer htmlLocalizer, string name, params object[] arguments);
     }
     public class HtmlLocalizerFactory : IHtmlLocalizerFactory {
         public HtmlLocalizerFactory(IStringLocalizerFactory localizerFactory);
         public virtual IHtmlLocalizer Create(string baseName, string location);
         public virtual IHtmlLocalizer Create(Type resourceSource);
     }
     public interface IHtmlLocalizer {
         LocalizedHtmlString this[string name, params object[] arguments] { get; }
         LocalizedHtmlString this[string name] { get; }
         IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures);
         LocalizedString GetString(string name);
         LocalizedString GetString(string name, params object[] arguments);
         IHtmlLocalizer WithCulture(CultureInfo culture);
     }
     public interface IHtmlLocalizer<TResource> : IHtmlLocalizer
     public interface IHtmlLocalizerFactory {
         IHtmlLocalizer Create(string baseName, string location);
         IHtmlLocalizer Create(Type resourceSource);
     }
     public interface IViewLocalizer : IHtmlLocalizer
     public class LocalizedHtmlString : IHtmlContent {
         public LocalizedHtmlString(string name, string value);
         public LocalizedHtmlString(string name, string value, bool isResourceNotFound);
         public LocalizedHtmlString(string name, string value, bool isResourceNotFound, params object[] arguments);
         public bool IsResourceNotFound { get; }
         public string Name { get; }
         public string Value { get; }
         public void WriteTo(TextWriter writer, HtmlEncoder encoder);
     }
     public class ViewLocalizer : IHtmlLocalizer, IViewContextAware, IViewLocalizer {
-        public ViewLocalizer(IHtmlLocalizerFactory localizerFactory, IHostingEnvironment hostingEnvironment);

+        public ViewLocalizer(IHtmlLocalizerFactory localizerFactory, IWebHostEnvironment hostingEnvironment);
         public virtual LocalizedHtmlString this[string key, params object[] arguments] { get; }
         public virtual LocalizedHtmlString this[string key] { get; }
         public void Contextualize(ViewContext viewContext);
         public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures);
         public LocalizedString GetString(string name);
         public LocalizedString GetString(string name, params object[] values);
         public IHtmlLocalizer WithCulture(CultureInfo culture);
     }
 }
```

