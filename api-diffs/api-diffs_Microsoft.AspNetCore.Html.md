# Microsoft.AspNetCore.Html

``` diff
 namespace Microsoft.AspNetCore.Html {
     public class HtmlContentBuilder : IHtmlContent, IHtmlContentBuilder, IHtmlContentContainer {
         public HtmlContentBuilder();
         public HtmlContentBuilder(IList<object> entries);
         public HtmlContentBuilder(int capacity);
         public int Count { get; }
         public IHtmlContentBuilder Append(string unencoded);
         public IHtmlContentBuilder AppendHtml(IHtmlContent htmlContent);
         public IHtmlContentBuilder AppendHtml(string encoded);
         public IHtmlContentBuilder Clear();
         public void CopyTo(IHtmlContentBuilder destination);
         public void MoveTo(IHtmlContentBuilder destination);
         public void WriteTo(TextWriter writer, HtmlEncoder encoder);
     }
     public static class HtmlContentBuilderExtensions {
         public static IHtmlContentBuilder AppendFormat(this IHtmlContentBuilder builder, IFormatProvider formatProvider, string format, params object[] args);
         public static IHtmlContentBuilder AppendFormat(this IHtmlContentBuilder builder, string format, params object[] args);
         public static IHtmlContentBuilder AppendHtmlLine(this IHtmlContentBuilder builder, string encoded);
         public static IHtmlContentBuilder AppendLine(this IHtmlContentBuilder builder);
         public static IHtmlContentBuilder AppendLine(this IHtmlContentBuilder builder, IHtmlContent content);
         public static IHtmlContentBuilder AppendLine(this IHtmlContentBuilder builder, string unencoded);
         public static IHtmlContentBuilder SetContent(this IHtmlContentBuilder builder, string unencoded);
         public static IHtmlContentBuilder SetHtmlContent(this IHtmlContentBuilder builder, IHtmlContent content);
         public static IHtmlContentBuilder SetHtmlContent(this IHtmlContentBuilder builder, string encoded);
     }
     public class HtmlFormattableString : IHtmlContent {
         public HtmlFormattableString(IFormatProvider formatProvider, string format, params object[] args);
         public HtmlFormattableString(string format, params object[] args);
         public void WriteTo(TextWriter writer, HtmlEncoder encoder);
     }
     public class HtmlString : IHtmlContent {
         public static readonly HtmlString Empty;
         public static readonly HtmlString NewLine;
         public HtmlString(string value);
         public string Value { get; }
         public override string ToString();
         public void WriteTo(TextWriter writer, HtmlEncoder encoder);
     }
     public interface IHtmlContent {
         void WriteTo(TextWriter writer, HtmlEncoder encoder);
     }
     public interface IHtmlContentBuilder : IHtmlContent, IHtmlContentContainer {
         IHtmlContentBuilder Append(string unencoded);
         IHtmlContentBuilder AppendHtml(IHtmlContent content);
         IHtmlContentBuilder AppendHtml(string encoded);
         IHtmlContentBuilder Clear();
     }
     public interface IHtmlContentContainer : IHtmlContent {
         void CopyTo(IHtmlContentBuilder builder);
         void MoveTo(IHtmlContentBuilder builder);
     }
 }
```

