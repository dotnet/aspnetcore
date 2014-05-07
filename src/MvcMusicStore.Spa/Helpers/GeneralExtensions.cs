using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace System.Web.Mvc.Html
{
    public static class GeneralExtensions
    {
        public static IHtmlString Tag(this HtmlHelper htmlHelper, TagBuilder tagBuilder)
        {
            return htmlHelper.Raw(tagBuilder.ToString());
        }
    }
}