using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class ValidationExtensions
    {
        public static HtmlString ValidationSummary<T>(this IHtmlHelper<T> htmlHelper)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors: false);
        }

        public static HtmlString ValidationSummary<T>(this IHtmlHelper<T> htmlHelper, bool excludePropertyErrors)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors, message: null);
        }

        public static HtmlString ValidationSummary<T>(this IHtmlHelper<T> htmlHelper, string message)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors: false, message: message, htmlAttributes: (object)null);
        }

        public static HtmlString ValidationSummary<T>(this IHtmlHelper<T> htmlHelper, bool excludePropertyErrors, string message)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors, message, htmlAttributes: (object)null);
        }

        public static HtmlString ValidationSummary<T>(this IHtmlHelper<T> htmlHelper, string message, object htmlAttributes)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors: false, message: message, htmlAttributes: HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString ValidationSummary<T>(this IHtmlHelper<T> htmlHelper, bool excludePropertyErrors, string message, object htmlAttributes)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors, message, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString ValidationSummary<T>(this IHtmlHelper<T> htmlHelper, string message, IDictionary<string, object> htmlAttributes)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors: false, message: message, htmlAttributes: htmlAttributes);
        }
    }
}
