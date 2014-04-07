
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
            return html.Display(expression, templateName: null, htmlFieldName: null, additionalViewData: additionalViewData);
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
    }
}
