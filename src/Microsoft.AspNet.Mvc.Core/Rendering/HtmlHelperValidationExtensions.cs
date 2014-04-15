using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class HtmlHelperValidationExtensions
    {
        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors: false);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper, bool excludePropertyErrors)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors, message: null);
        }

        public static HtmlString ValidationSummary([NotNull] this IHtmlHelper htmlHelper, string message)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors: false, message: message,
                htmlAttributes: (object)null);
        }

        public static HtmlString ValidationSummary(
            [NotNull] this IHtmlHelper htmlHelper,
            bool excludePropertyErrors,
            string message)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors, message, htmlAttributes: (object)null);
        }

        public static HtmlString ValidationSummary(
            [NotNull] this IHtmlHelper htmlHelper,
            string message,
            object htmlAttributes)
        {
            return ValidationSummary(htmlHelper, excludePropertyErrors: false, message: message,
                htmlAttributes: HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString ValidationSummary(
            [NotNull] this IHtmlHelper htmlHelper,
            bool excludePropertyErrors,
            string message,
            object htmlAttributes)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors, message,
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString ValidationSummary(
            [NotNull] this IHtmlHelper htmlHelper,
            string message,
            IDictionary<string, object> htmlAttributes)
        {
            return htmlHelper.ValidationSummary(excludePropertyErrors: false, message: message,
                htmlAttributes: htmlAttributes);
        }

        private IHtmlString BuildValidationMessage(string name, string message, IDictionary<string, object> htmlAttributes)
        {
            var modelState = ModelState[name];
            IEnumerable<string> errors = null;
            if (modelState != null)
            {
                errors = modelState.Errors;
            }
            bool hasError = errors != null && errors.Any();
            if (!hasError && !UnobtrusiveJavaScriptEnabled)
            {
                // If unobtrusive validation is enabled, we need to generate an empty span with the "val-for" attribute"
                return null;
            }
            else
            {
                string error = null;
                if (hasError)
                {
                    error = message ?? errors.First();
                }

                TagBuilder tagBuilder = new TagBuilder("span") { InnerHtml = Encode(error) };
                tagBuilder.MergeAttributes(htmlAttributes);
                if (UnobtrusiveJavaScriptEnabled)
                {
                    bool replaceValidationMessageContents = String.IsNullOrEmpty(message);
                    tagBuilder.MergeAttribute("data-valmsg-for", name);
                    tagBuilder.MergeAttribute("data-valmsg-replace", replaceValidationMessageContents.ToString().ToLowerInvariant());
                }
                tagBuilder.AddCssClass(hasError ? ValidationMessageCssClassName : ValidationMessageValidCssClassName);
                return tagBuilder.ToHtmlString(TagRenderMode.Normal);
            }
        }
    }
}
