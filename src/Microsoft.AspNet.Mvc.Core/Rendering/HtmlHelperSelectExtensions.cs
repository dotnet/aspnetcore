using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class SelectExtensions
    {
        public static HtmlString DropDownList<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string name)
        {
            return htmlHelper.DropDownList(name, selectList: null, optionLabel: null, htmlAttributes: null);
        }

        public static HtmlString DropDownList<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string name,
            string optionLabel)
        {
            return htmlHelper.DropDownList(name, selectList: null, optionLabel: optionLabel, htmlAttributes: null);
        }

        public static HtmlString DropDownList<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string name,
            IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.DropDownList(name, selectList, optionLabel: null, htmlAttributes: null);
        }

        public static HtmlString DropDownList<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string name,
            IEnumerable<SelectListItem> selectList, object htmlAttributes)
        {
            return htmlHelper.DropDownList(name, selectList, optionLabel: null, htmlAttributes: htmlAttributes);
        }

        public static HtmlString DropDownList<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string name,
            IEnumerable<SelectListItem> selectList, string optionLabel)
        {
            return htmlHelper.DropDownList(name, selectList, optionLabel, htmlAttributes: null);
        }

        public static HtmlString DropDownListFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList)
        {
            return htmlHelper.DropDownListFor(expression, selectList, optionLabel: null, htmlAttributes: null);
        }

        public static HtmlString DropDownListFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            return htmlHelper.DropDownListFor(expression, selectList, optionLabel: null,
                htmlAttributes: htmlAttributes);
        }

        public static HtmlString DropDownListFor<TModel, TProperty>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList,
            string optionLabel)
        {
            return htmlHelper.DropDownListFor(expression, selectList, optionLabel, htmlAttributes: null);
        }
    }
}
