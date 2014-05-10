using System;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class GeneralExtensions
    {
        public static HtmlString Tag(this IHtmlHelper htmlHelper, TagBuilder tagBuilder)
        {
            return htmlHelper.Raw(tagBuilder.ToString());
        }
    }
}