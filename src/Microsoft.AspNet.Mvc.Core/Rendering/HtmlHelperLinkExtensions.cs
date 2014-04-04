
namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperLinkExtensions
    {
        public static HtmlString ActionLink<TModel>(
            [NotNull] this IHtmlHelper<TModel> helper, 
            [NotNull] string linkText, 
            string actionName)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName: null,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: null,
                htmlAttributes: null);
        }

        public static HtmlString ActionLink<TModel>(
            [NotNull] this IHtmlHelper<TModel> helper, 
            [NotNull] string linkText, 
            string actionName, 
            object routeValues)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName: null,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: routeValues,
                htmlAttributes: null);
        }

        public static HtmlString ActionLink<TModel>(
            [NotNull] this IHtmlHelper<TModel> helper, 
            [NotNull] string linkText, 
            string actionName, 
            object routeValues, 
            object htmlAttributes)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName: null,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: routeValues,
                htmlAttributes: htmlAttributes);
        }

        public static HtmlString ActionLink<TModel>(
            [NotNull] this IHtmlHelper<TModel> helper, 
            [NotNull] string linkText, 
            string actionName, 
            string controllerName)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: null,
                htmlAttributes: null);
        }

        public static HtmlString ActionLink<TModel>(
            [NotNull] this IHtmlHelper<TModel> helper, 
            [NotNull] string linkText, 
            string actionName, 
            string controllerName, 
            object routeValues, 
            object htmlAttributes)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: routeValues,
                htmlAttributes: htmlAttributes);
        }
    }
}
