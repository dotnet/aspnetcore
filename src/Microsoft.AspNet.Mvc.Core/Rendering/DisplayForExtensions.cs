using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperDisplayForExtensions
    {
        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html, 
                                                            [NotNull] Expression<Func<TModel, TValue>> expression)
        {
            return html.DisplayFor<TValue>(expression, templateName: null, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                            [NotNull] Expression<Func<TModel, TValue>> expression, 
                                                            object additionalViewData)
        {
            return html.DisplayFor<TValue>(expression, templateName: null, htmlFieldName: null, additionalViewData: additionalViewData);
        }

        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                            [NotNull] Expression<Func<TModel, TValue>> expression, 
                                                            string templateName)
        {
            return html.DisplayFor<TValue>(expression, templateName, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                            [NotNull] Expression<Func<TModel, TValue>> expression, 
                                                            string templateName, 
                                                            object additionalViewData)
        {
            return html.DisplayFor<TValue>(expression, templateName, htmlFieldName: null, additionalViewData: additionalViewData);
        }

        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                            [NotNull] Expression<Func<TModel, TValue>> expression, 
                                                            string templateName, 
                                                            string htmlFieldName)
        {
            return html.DisplayFor<TValue>(expression, templateName: templateName, htmlFieldName: htmlFieldName, additionalViewData: null);
        }
    }
}
