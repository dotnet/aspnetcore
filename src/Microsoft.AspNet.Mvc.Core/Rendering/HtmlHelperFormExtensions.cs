
namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperFormExtensions
    {
        public static MvcForm BeginForm<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper)
        {
            // Generates <form action="{current url}" method="post">.
            return htmlHelper.BeginForm(actionName: null, controllerName: null, routeValues: null,
                                        method: FormMethod.Post, htmlAttributes: null);
        }

        public static MvcForm BeginForm<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, FormMethod method)
        {
            return htmlHelper.BeginForm(actionName: null, controllerName: null, routeValues: null,
                                        method: method, htmlAttributes: null);
        }

        public static MvcForm BeginForm<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, FormMethod method,
                                                object htmlAttributes)
        {
            return htmlHelper.BeginForm(actionName: null, controllerName: null, routeValues: null,
                                        method: method, htmlAttributes: htmlAttributes);
        }

        public static MvcForm BeginForm<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, object routeValues)
        {
            return htmlHelper.BeginForm(actionName: null, controllerName: null, routeValues: routeValues,
                                        method: FormMethod.Post, htmlAttributes: null);
        }

        public static MvcForm BeginForm<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string actionName,
                                                string controllerName)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues: null,
                                        method: FormMethod.Post, htmlAttributes: null);
        }

        public static MvcForm BeginForm<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string actionName,
                                                string controllerName, object routeValues)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues,
                                        FormMethod.Post, htmlAttributes: null);
        }

        public static MvcForm BeginForm<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string actionName,
                                                string controllerName, FormMethod method)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues: null,
                                        method: method, htmlAttributes: null);
        }

        public static MvcForm BeginForm<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string actionName,
                                                string controllerName, object routeValues, FormMethod method)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues,
                                        method, htmlAttributes: null);
        }

        public static MvcForm BeginForm<TModel>([NotNull] this IHtmlHelper<TModel> htmlHelper, string actionName,
                                                string controllerName, FormMethod method, object htmlAttributes)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues: null,
                                        method: method, htmlAttributes: htmlAttributes);
        }
    }
}
