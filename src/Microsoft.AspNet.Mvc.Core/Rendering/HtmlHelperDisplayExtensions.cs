using System;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperDisplayExtensions
    {
        public static HtmlString Display<TModel>([NotNull] this IHtmlHelper<TModel> html,
                                                 string expression)
        {
            return html.Display(expression, templateName: null, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString Display<TModel>([NotNull] this IHtmlHelper<TModel> html,
                                                 string expression,
                                                 object additionalViewData)
        {
            return html.Display(expression, templateName: null, htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        public static HtmlString Display<TModel>([NotNull] this IHtmlHelper<TModel> html,
                                                 string expression,
                                                 string templateName)
        {
            return html.Display(expression, templateName, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString Display<TModel>([NotNull] this IHtmlHelper<TModel> html,
                                                 string expression,
                                                 string templateName,
                                                 object additionalViewData)
        {
            return html.Display(expression, templateName, htmlFieldName: null, additionalViewData: additionalViewData);
        }

        public static HtmlString Display<TModel>([NotNull] this IHtmlHelper<TModel> html,
                                                 string expression,
                                                 string templateName,
                                                 string htmlFieldName)
        {
            return html.Display(expression, templateName, htmlFieldName, additionalViewData: null);
        }

        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                            [NotNull] Expression<Func<TModel, TValue>> expression)
        {
            return html.DisplayFor<TValue>(expression, templateName: null, htmlFieldName: null,
                additionalViewData: null);
        }

        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                            [NotNull] Expression<Func<TModel, TValue>> expression,
                                                            object additionalViewData)
        {
            return html.DisplayFor<TValue>(expression, templateName: null, htmlFieldName: null,
                additionalViewData: additionalViewData);
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
            return html.DisplayFor<TValue>(expression, templateName, htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        public static HtmlString DisplayFor<TModel, TValue>([NotNull] this IHtmlHelper<TModel> html,
                                                            [NotNull] Expression<Func<TModel, TValue>> expression,
                                                            string templateName,
                                                            string htmlFieldName)
        {
            return html.DisplayFor<TValue>(expression, templateName: templateName, htmlFieldName: htmlFieldName,
                additionalViewData: null);
        }

        public static HtmlString DisplayForModel<TModel>([NotNull] this IHtmlHelper<TModel> html)
        {
            return html.DisplayForModel(templateName: null, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString DisplayForModel<TModel>([NotNull] this IHtmlHelper<TModel> html,
                                                         object additionalViewData)
        {
            return html.DisplayForModel(templateName: null, htmlFieldName: null,
                additionalViewData: additionalViewData);
        }

        public static HtmlString DisplayForModel<TModel>([NotNull] this IHtmlHelper<TModel> html,
                                                         string templateName)
        {
            return html.DisplayForModel(templateName, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString DisplayForModel<TModel>([NotNull] this IHtmlHelper<TModel> html,
                                                         string templateName,
                                                         object additionalViewData)
        {
            return html.DisplayForModel(templateName, htmlFieldName: null, additionalViewData: additionalViewData);
        }

        public static HtmlString DisplayForModel<TModel>([NotNull] this IHtmlHelper<TModel> html,
                                                         string templateName,
                                                         string htmlFieldName)
        {
            return html.DisplayForModel(templateName, htmlFieldName, additionalViewData: null);
        }
    }
}
