using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Default implementation of non-generic portions of <see cref="IHtmlHelper{T}">.
    /// </summary>
    public class HtmlHelper : ICanHasViewContext
    {
        public static readonly string ValidationInputCssClassName = "input-validation-error";
        public static readonly string ValidationInputValidCssClassName = "input-validation-valid";
        public static readonly string ValidationMessageCssClassName = "field-validation-error";
        public static readonly string ValidationMessageValidCssClassName = "field-validation-valid";
        public static readonly string ValidationSummaryCssClassName = "validation-summary-errors";
        public static readonly string ValidationSummaryValidCssClassName = "validation-summary-valid";

        private const string HiddenListItem = @"<li style=""display:none""></li>";

        private ViewContext _viewContext;
        private IViewEngine _viewEngine;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlHelper"/> class.
        /// </summary>
        public HtmlHelper(IViewEngine viewEngine)
        {
            _viewEngine = viewEngine;

            // Underscores are fine characters in id's.
            IdAttributeDotReplacement = "_";
        }

        public string IdAttributeDotReplacement { get; set; }

        public HttpContext HttpContext { get; private set; }

        public ViewContext ViewContext
        {
            get
            {
                if (_viewContext == null)
                {
                    throw new InvalidOperationException(Resources.HtmlHelper_NotContextualized);
                }

                return _viewContext;
            }
            private set
            {
                _viewContext = value;
            }
        }

        public dynamic ViewBag
        {
            get
            {
                return ViewContext.ViewBag;
            }
        }

        public ViewDataDictionary ViewData
        {
            get
            {
                return ViewContext.ViewData;
            }
        }

        /// <summary>
        /// Creates a dictionary of HTML attributes from the input object,
        /// translating underscores to dashes.
        /// <example>
        /// new { data_name="value" } will translate to the entry { "data-name" , "value" }
        /// in the resulting dictionary.
        /// </example>
        /// </summary>
        /// <param name="htmlAttributes">Anonymous object describing HTML attributes.</param>
        /// <returns>A dictionary that represents HTML attributes.</returns>
        public static IDictionary<string, object> AnonymousObjectToHtmlAttributes(object htmlAttributes)
        {
            Dictionary<string, object> result;
            var valuesAsDictionary = htmlAttributes as IDictionary<string, object>;
            if (valuesAsDictionary != null)
            {
                result = new Dictionary<string, object>(valuesAsDictionary, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                if (htmlAttributes != null)
                {
                    foreach (var prop in htmlAttributes.GetType().GetRuntimeProperties())
                    {
                        var value = prop.GetValue(htmlAttributes);
                        result.Add(prop.Name, value);
                    }
                }
            }

            return result;
        }

        public virtual void Contextualize([NotNull] ViewContext viewContext)
        {
            ViewContext = viewContext;
        }

        public string Encode(string value)
        {
            return (!string.IsNullOrEmpty(value)) ? WebUtility.HtmlEncode(value) : string.Empty;
        }

        public string Encode(object value)
        {
            return value != null ? WebUtility.HtmlEncode(value.ToString()) : string.Empty;
        }

        public string GenerateIdFromName([NotNull] string name)
        {
            return TagBuilder.CreateSanitizedId(name, IdAttributeDotReplacement);
        }

        /// <inheritdoc />
        public virtual HtmlString Name(string name)
        {
            var fullName = ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            return new HtmlString(Encode(fullName));
        }

        public async Task<HtmlString> PartialAsync([NotNull] string partialViewName, object model,
                                                   ViewDataDictionary viewData)
        {
            using (var writer = new StringWriter(CultureInfo.CurrentCulture))
            {
                await RenderPartialCoreAsync(partialViewName, model, viewData, writer);

                return new HtmlString(writer.ToString());
            }
        }

        public Task RenderPartialAsync([NotNull] string partialViewName, object model, ViewDataDictionary viewData)
        {
            return RenderPartialCoreAsync(partialViewName, model, viewData, ViewContext.Writer);
        }

        protected virtual async Task RenderPartialCoreAsync([NotNull] string partialViewName,
                                                            object model,
                                                            ViewDataDictionary viewData,
                                                            TextWriter writer)
        {
            // Determine which ViewData we should use to construct a new ViewData
            var baseViewData = viewData ?? ViewData;

            var newViewData = new ViewDataDictionary(baseViewData, model);

            var newViewContext = new ViewContext(ViewContext)
            {
                ViewData = newViewData,
                Writer = writer
            };

            var viewEngineResult = await _viewEngine.FindPartialView(newViewContext.ViewEngineContext, partialViewName);

            await viewEngineResult.View.RenderAsync(newViewContext);
        }

        public virtual HtmlString ValidationSummary(bool excludePropertyErrors, string message, IDictionary<string, object> htmlAttributes)
        {
            var formContext = ViewContext.ClientValidationEnabled ? ViewContext.FormContext : null;

            if (ViewData.ModelState.IsValid == true)
            {
                if (formContext == null ||
                    ViewContext.UnobtrusiveJavaScriptEnabled &&
                    excludePropertyErrors)
                {
                    // No client side validation/updates
                    return HtmlString.Empty;
                }
            }

            string messageSpan;
            if (!string.IsNullOrEmpty(message))
            {
                var spanTag = new TagBuilder("span");
                spanTag.SetInnerText(message);
                messageSpan = spanTag.ToString(TagRenderMode.Normal) + Environment.NewLine;
            }
            else
            {
                messageSpan = null;
            }

            var htmlSummary = new StringBuilder();
            var modelStates = ValidationHelpers.GetModelStateList(ViewData, excludePropertyErrors);

            foreach (var modelState in modelStates)
            {
                foreach (var modelError in modelState.Errors)
                {
                    string errorText = ValidationHelpers.GetUserErrorMessageOrDefault(modelError, modelState: null);

                    if (!string.IsNullOrEmpty(errorText))
                    {
                        var listItem = new TagBuilder("li");
                        listItem.SetInnerText(errorText);
                        htmlSummary.AppendLine(listItem.ToString(TagRenderMode.Normal));
                    }
                }
            }

            if (htmlSummary.Length == 0)
            {
                htmlSummary.AppendLine(HiddenListItem);
            }

            var unorderedList = new TagBuilder("ul")
            {
                InnerHtml = htmlSummary.ToString()
            };

            var divBuilder = new TagBuilder("div");
            divBuilder.MergeAttributes(htmlAttributes);

            if (ViewData.ModelState.IsValid == true)
            {
                divBuilder.AddCssClass(HtmlHelper.ValidationSummaryValidCssClassName);
            }
            else
            {
                divBuilder.AddCssClass(HtmlHelper.ValidationSummaryCssClassName);
            }

            divBuilder.InnerHtml = messageSpan + unorderedList.ToString(TagRenderMode.Normal);

            if (formContext != null)
            {
                if (ViewContext.UnobtrusiveJavaScriptEnabled)
                {
                    if (!excludePropertyErrors)
                    {
                        // Only put errors in the validation summary if they're supposed to be included there
                        divBuilder.MergeAttribute("data-valmsg-summary", "true");
                    }
                }
                else
                {
                    // client validation summaries need an ID
                    divBuilder.GenerateId("validationSummary", IdAttributeDotReplacement);
                    formContext.ValidationSummaryId = divBuilder.Attributes["id"];
                    formContext.ReplaceValidationSummary = !excludePropertyErrors;
                }
            }

            return divBuilder.ToHtmlString(TagRenderMode.Normal);
        }

        /// <summary>
        /// Returns the HTTP method that handles form input (GET or POST) as a string.
        /// </summary>
        /// <param name="method">The HTTP method that handles the form.</param>
        /// <returns>The form method string, either "get" or "post".</returns>
        public static string GetFormMethodString(FormMethod method)
        {
            switch (method)
            {
                case FormMethod.Get:
                    return "get";
                case FormMethod.Post:
                    return "post";
                default:
                    return "post";
            }
        }

        public static string GetInputTypeString(InputType inputType)
        {
            switch (inputType)
            {
                case InputType.CheckBox:
                    return "checkbox";
                case InputType.Hidden:
                    return "hidden";
                case InputType.Password:
                    return "password";
                case InputType.Radio:
                    return "radio";
                case InputType.Text:
                    return "text";
                default:
                    return "text";
            }
        }

        public HtmlString Raw(string value)
        {
            return new HtmlString(value);
        }

        public HtmlString Raw(object value)
        {
            return new HtmlString(value == null ? null : value.ToString());
        }
    }
}
