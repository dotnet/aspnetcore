using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ApplicationWithTagHelpers.TagHelpers
{
    public class TestTagHelper : TagHelper
    {
        public TestTagHelper(IUrlHelperFactory urlHelperFactory)
        {
            UrlHelperFactory = urlHelperFactory;
        }

        [HtmlAttributeNotBound]
        public IUrlHelperFactory UrlHelperFactory { get; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public string Controller { get; set; }

        public string Action { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var urlHelper = UrlHelperFactory.GetUrlHelper(ViewContext);
            output.Attributes.SetAttribute("href", urlHelper.Action(new UrlActionContext
            {
                Controller = Controller,
                Action = Action
            }));

            output.PreContent.SetContent($"{nameof(TestTagHelper)} content.");
        }
    }
}
