using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ClassLibraryTagHelpers
{
    [HtmlTargetElement(Attributes = "bold")]
    public class BoldTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Attributes.RemoveAll("bold");
            output.PreContent.AppendHtml("<b>");
            output.PostContent.AppendHtml("</b>");
        }
    }
}