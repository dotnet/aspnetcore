# Microsoft.AspNetCore.Razor.Runtime.TagHelpers

``` diff
 namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers {
     public class TagHelperExecutionContext {
         public TagHelperExecutionContext(string tagName, TagMode tagMode, IDictionary<object, object> items, string uniqueId, Func<Task> executeChildContentAsync, Action<HtmlEncoder> startTagHelperWritingScope, Func<TagHelperContent> endTagHelperWritingScope);
         public bool ChildContentRetrieved { get; }
         public TagHelperContext Context { get; }
         public IDictionary<object, object> Items { get; private set; }
         public TagHelperOutput Output { get; internal set; }
         public IList<ITagHelper> TagHelpers { get; }
         public void Add(ITagHelper tagHelper);
         public void AddHtmlAttribute(TagHelperAttribute attribute);
         public void AddHtmlAttribute(string name, object value, HtmlAttributeValueStyle valueStyle);
         public void AddTagHelperAttribute(TagHelperAttribute attribute);
         public void AddTagHelperAttribute(string name, object value, HtmlAttributeValueStyle valueStyle);
         public void Reinitialize(string tagName, TagMode tagMode, IDictionary<object, object> items, string uniqueId, Func<Task> executeChildContentAsync);
         public Task SetOutputContentAsync();
     }
     public class TagHelperRunner {
         public TagHelperRunner();
         public Task RunAsync(TagHelperExecutionContext executionContext);
     }
     public class TagHelperScopeManager {
         public TagHelperScopeManager(Action<HtmlEncoder> startTagHelperWritingScope, Func<TagHelperContent> endTagHelperWritingScope);
         public TagHelperExecutionContext Begin(string tagName, TagMode tagMode, string uniqueId, Func<Task> executeChildContentAsync);
         public TagHelperExecutionContext End();
     }
 }
```

