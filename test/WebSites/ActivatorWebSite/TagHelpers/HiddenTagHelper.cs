using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace ActivatorWebSite.TagHelpers
{
    [HtmlElementName("span")]
    [ContentBehavior(ContentBehavior.Modify)]
    public class HiddenTagHelper : TagHelper
    {
        public string Name { get; set; }

        [Activate]
        public IHtmlHelper HtmlHelper { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Content = HtmlHelper.Hidden(Name, output.Content).ToString();
        }
    }
}