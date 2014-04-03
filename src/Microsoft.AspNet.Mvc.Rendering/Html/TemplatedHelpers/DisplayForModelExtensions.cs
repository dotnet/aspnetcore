
namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperDisplayForModelExtensions
    {
        public static HtmlString DisplayForModel<TModel>([NotNull] this IHtmlHelper<TModel> html)
        {
            return html.DisplayForModel(templateName: null, htmlFieldName: null, additionalViewData: null);
        }

        public static HtmlString DisplayForModel<TModel>([NotNull] this IHtmlHelper<TModel> html, 
                                                         object additionalViewData)
        {
            return html.DisplayForModel(templateName: null, htmlFieldName: null, additionalViewData: additionalViewData);
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