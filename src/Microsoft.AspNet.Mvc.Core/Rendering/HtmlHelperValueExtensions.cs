using System;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperValueExtensions
    {
        public static HtmlString Value([NotNull] this IHtmlHelper htmlHelper, string name)
        {
            return htmlHelper.Value(name, format: null);
        }

        public static HtmlString ValueFor<TModel, TProperty>(
            [NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            return htmlHelper.ValueFor(expression, format: null);
        }

        public static HtmlString ValueForModel([NotNull] this IHtmlHelper htmlHelper)
        {
            return ValueForModel(htmlHelper, format: null);
        }

        public static HtmlString ValueForModel([NotNull] this IHtmlHelper htmlHelper, string format)
        {
            return htmlHelper.Value(string.Empty, format);
        }
    }
}
