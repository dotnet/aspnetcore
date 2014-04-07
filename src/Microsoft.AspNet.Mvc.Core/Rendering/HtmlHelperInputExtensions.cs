using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperInputExtensions
    {
        public static HtmlString TextBox<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string name)
        {
            return TextBox(htmlHelper, name, value: null);
        }

        public static HtmlString TextBox<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string name,
            object value)
        {
            return TextBox(htmlHelper, name, value, format: null);
        }

        public static HtmlString TextBox<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string name,
            object value, string format)
        {
            return TextBox(htmlHelper, name, value, format, htmlAttributes: null);
        }

        public static HtmlString TextBox<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string name,
            object value, object htmlAttributes)
        {
            return TextBox(htmlHelper, name, value, format: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString TextBox<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string name,
            object value, string format, object htmlAttributes)
        {
            return htmlHelper.TextBox(name, value, format,
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString TextBox<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string name,
            object value, IDictionary<string, object> htmlAttributes)
        {
            return htmlHelper.TextBox(name, value, format: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString TextBoxFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            return TextBoxFor(htmlHelper, expression, format: null);
        }

        public static HtmlString TextBoxFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, string format)
        {
            return TextBoxFor(htmlHelper, expression, format, htmlAttributes: null);
        }

        public static HtmlString TextBoxFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
        {
            return TextBoxFor(htmlHelper, expression, format: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString TextBoxFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, string format, object htmlAttributes)
        {
            return htmlHelper.TextBoxFor(expression, format: format,
                htmlAttributes: HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString TextBoxFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes)
        {
            return htmlHelper.TextBoxFor(expression, format: null, htmlAttributes: htmlAttributes);
        }
    }
}
