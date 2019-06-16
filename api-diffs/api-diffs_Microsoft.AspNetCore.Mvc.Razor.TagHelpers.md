# Microsoft.AspNetCore.Mvc.Razor.TagHelpers

``` diff
 namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers {
     public class BodyTagHelper : TagHelperComponentTagHelper {
         public BodyTagHelper(ITagHelperComponentManager manager, ILoggerFactory loggerFactory);
     }
     public class HeadTagHelper : TagHelperComponentTagHelper {
         public HeadTagHelper(ITagHelperComponentManager manager, ILoggerFactory loggerFactory);
     }
     public interface ITagHelperComponentManager {
         ICollection<ITagHelperComponent> Components { get; }
     }
     public interface ITagHelperComponentPropertyActivator {
         void Activate(ViewContext context, ITagHelperComponent tagHelperComponent);
     }
     public abstract class TagHelperComponentTagHelper : TagHelper {
         public TagHelperComponentTagHelper(ITagHelperComponentManager manager, ILoggerFactory loggerFactory);
         public ITagHelperComponentPropertyActivator PropertyActivator { get; set; }
         public ViewContext ViewContext { get; set; }
         public override void Init(TagHelperContext context);
         public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
     }
     public class TagHelperFeature {
         public TagHelperFeature();
         public IList<TypeInfo> TagHelpers { get; }
     }
     public class TagHelperFeatureProvider : IApplicationFeatureProvider, IApplicationFeatureProvider<TagHelperFeature> {
         public TagHelperFeatureProvider();
         protected virtual bool IncludePart(ApplicationPart part);
         protected virtual bool IncludeType(TypeInfo type);
         public void PopulateFeature(IEnumerable<ApplicationPart> parts, TagHelperFeature feature);
     }
     public class UrlResolutionTagHelper : TagHelper {
         public UrlResolutionTagHelper(IUrlHelperFactory urlHelperFactory, HtmlEncoder htmlEncoder);
         protected HtmlEncoder HtmlEncoder { get; }
         public override int Order { get; }
         protected IUrlHelperFactory UrlHelperFactory { get; }
         public ViewContext ViewContext { get; set; }
         public override void Process(TagHelperContext context, TagHelperOutput output);
         protected void ProcessUrlAttribute(string attributeName, TagHelperOutput output);
         protected bool TryResolveUrl(string url, out IHtmlContent resolvedUrl);
         protected bool TryResolveUrl(string url, out string resolvedUrl);
     }
 }
```

