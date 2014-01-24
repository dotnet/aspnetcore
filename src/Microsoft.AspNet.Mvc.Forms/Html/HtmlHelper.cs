// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text;

namespace Microsoft.AspNet.Mvc
{
    public class HtmlHelper
    {
        public static readonly string ValidationInputCssClassName = "input-validation-error";
        public static readonly string ValidationInputValidCssClassName = "input-validation-valid";
        public static readonly string ValidationMessageCssClassName = "field-validation-error";
        public static readonly string ValidationMessageValidCssClassName = "field-validation-valid";
        public static readonly string ValidationSummaryCssClassName = "validation-summary-errors";
        public static readonly string ValidationSummaryValidCssClassName = "validation-summary-valid";

        private static readonly object _html5InputsModeKey = new object();

        public HtmlHelper(RequestContext requestContext, ViewData viewData)
        {
            if (requestContext == null)
            {
                throw new ArgumentNullException("requestContext");
            }

            RequestContext = requestContext;
            ViewData = viewData;
            // ClientValidationRuleFactory = (name, metadata) => ModelValidatorProviders.Providers.GetValidators(metadata ?? ModelMetadata.FromStringExpression(name, ViewData), ViewContext).SelectMany(v => v.GetClientValidationRules());
        }

        //internal Func<string, ModelMetadata, IEnumerable<ModelClientValidationRule>> ClientValidationRuleFactory { get; set; }

        public RequestContext RequestContext { get; private set; }

        public ViewData ViewData
        {
            get;
            private set;
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
        public static Dictionary<string, object> AnonymousObjectToHtmlAttributes(object htmlAttributes)
        {
            Dictionary<string, object> result;
            IDictionary<string, object> valuesAsDictionary = htmlAttributes as IDictionary<string, object>;
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
                        object val = prop.GetValue(htmlAttributes);
                        result.Add(prop.Name, val);
                    }
                }
            }

            return result;
        }

        //[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "For consistency, all helpers are instance methods.")]
        //public HtmlString AntiForgeryToken()
        //{
        //    return new HtmlString(AntiForgery.GetHtml().ToString());
        //}

        ///// <summary>
        ///// Set this property to <see cref="Mvc.Html5DateRenderingMode.Rfc3339"/> to have templated helpers such as Html.EditorFor render date and time
        ///// values as Rfc3339 compliant strings.
        ///// </summary>
        ///// <remarks>
        ///// The scope of this setting is for the current view alone. Sub views and parent views
        ///// will default to <see cref="Mvc.Html5DateRenderingMode.CurrentCulture"/> unless explicitly set otherwise.
        ///// </remarks>
        //[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "The usage of the property is as an instance property of the helper.")]
        //public Html5DateRenderingMode Html5DateRenderingMode
        //{
        //    get
        //    {
        //        object value;
        //        if (ScopeStorage.CurrentScope.TryGetValue(_html5InputsModeKey, out value))
        //        {
        //            return (Html5DateRenderingMode)value;
        //        }
        //        return default(Html5DateRenderingMode);
        //    }
        //    set
        //    {
        //        ScopeStorage.CurrentScope[_html5InputsModeKey] = value;
        //    }
        //}

        //[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "For consistency, all helpers are instance methods.")]
        //public string AttributeEncode(string value)
        //{
        //    return (!String.IsNullOrEmpty(value)) ? HttpUtility.HtmlAttributeEncode(value) : String.Empty;
        //}

        //public string AttributeEncode(object value)
        //{
        //    return AttributeEncode(Convert.ToString(value, CultureInfo.InvariantCulture));
        //}

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "For consistency, all helpers are instance methods.")]
        public string Encode(string value)
        {
            return (!String.IsNullOrEmpty(value)) ? WebUtility.HtmlEncode(value) : String.Empty;
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "For consistency, all helpers are instance methods.")]
        public string Encode(object value)
        {
            return value != null ? WebUtility.HtmlEncode(value.ToString()) : String.Empty;
        }

        internal static IView FindPartialView(ViewContext viewContext, string partialViewName, IViewEngine viewEngine)
        {
            ViewEngineResult result = viewEngine.FindView(viewContext, partialViewName).Result;
            if (result.View != null)
            {
                return result.View;
            }

            StringBuilder locationsText = new StringBuilder();
            foreach (string location in result.SearchedLocations)
            {
                locationsText.AppendLine();
                locationsText.Append(location);
            }

            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                                                              "MvcResources.Common_PartialViewNotFound", partialViewName, locationsText));
        }

        public static string GenerateIdFromName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            return TagBuilder.CreateSanitizedId(name);
        }

        //public static string GenerateLink(RequestContext requestContext, RouteCollection routeCollection, string linkText, string routeName, string actionName, string controllerName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        //{
        //    return GenerateLink(requestContext, routeCollection, linkText, routeName, actionName, controllerName, null /* protocol */, null /* hostName */, null /* fragment */, routeValues, htmlAttributes);
        //}

        //public static string GenerateLink(RequestContext requestContext, RouteCollection routeCollection, string linkText, string routeName, string actionName, string controllerName, string protocol, string hostName, string fragment, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        //{
        //    return GenerateLinkInternal(requestContext, routeCollection, linkText, routeName, actionName, controllerName, protocol, hostName, fragment, routeValues, htmlAttributes, true /* includeImplicitMvcValues */);
        //}

        //private static string GenerateLinkInternal(RequestContext requestContext, RouteCollection routeCollection, string linkText, string routeName, string actionName, string controllerName, string protocol, string hostName, string fragment, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes, bool includeImplicitMvcValues)
        //{
        //    string url = UrlHelper.GenerateUrl(routeName, actionName, controllerName, protocol, hostName, fragment, routeValues, routeCollection, requestContext, includeImplicitMvcValues);
        //    TagBuilder tagBuilder = new TagBuilder("a")
        //    {
        //        InnerHtml = (!String.IsNullOrEmpty(linkText)) ? HttpUtility.HtmlEncode(linkText) : String.Empty
        //    };
        //    tagBuilder.MergeAttributes(htmlAttributes);
        //    tagBuilder.MergeAttribute("href", url);
        //    return tagBuilder.ToString(TagRenderMode.Normal);
        //}

        //public static string GenerateRouteLink(RequestContext requestContext, RouteCollection routeCollection, string linkText, string routeName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        //{
        //    return GenerateRouteLink(requestContext, routeCollection, linkText, routeName, null /* protocol */, null /* hostName */, null /* fragment */, routeValues, htmlAttributes);
        //}

        //public static string GenerateRouteLink(RequestContext requestContext, RouteCollection routeCollection, string linkText, string routeName, string protocol, string hostName, string fragment, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        //{
        //    return GenerateLinkInternal(requestContext, routeCollection, linkText, routeName, null /* actionName */, null /* controllerName */, protocol, hostName, fragment, routeValues, htmlAttributes, false /* includeImplicitMvcValues */);
        //}

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

        //internal object GetModelStateValue(string key, Type destinationType)
        //{
        //    ModelState modelState;
        //    if (ViewData.ModelState.TryGetValue(key, out modelState))
        //    {
        //        if (modelState.Value != null)
        //        {
        //            return modelState.Value.ConvertTo(destinationType, null /* culture */);
        //        }
        //    }
        //    return null;
        //}

        //public IDictionary<string, object> GetUnobtrusiveValidationAttributes(string name)
        //{
        //    return GetUnobtrusiveValidationAttributes(name, metadata: null);
        //}

        //// Only render attributes if unobtrusive client-side validation is enabled, and then only if we've
        //// never rendered validation for a field with this name in this form. Also, if there's no form context,
        //// then we can't render the attributes (we'd have no <form> to attach them to).
        //public IDictionary<string, object> GetUnobtrusiveValidationAttributes(string name, ModelMetadata metadata)
        //{
        //    Dictionary<string, object> results = new Dictionary<string, object>();

        //    // The ordering of these 3 checks (and the early exits) is for performance reasons.
        //    if (!ViewContext.UnobtrusiveJavaScriptEnabled)
        //    {
        //        return results;
        //    }

        //    FormContext formContext = ViewContext.GetFormContextForClientValidation();
        //    if (formContext == null)
        //    {
        //        return results;
        //    }

        //    string fullName = ViewData.TemplateInfo.GetFullHtmlFieldName(name);
        //    if (formContext.RenderedField(fullName))
        //    {
        //        return results;
        //    }

        //    formContext.RenderedField(fullName, true);

        //    IEnumerable<ModelClientValidationRule> clientRules = ClientValidationRuleFactory(name, metadata);
        //    UnobtrusiveValidationAttributesGenerator.GetValidationAttributes(clientRules, results);

        //    return results;
        //}

        /// <summary>
        /// Wraps HTML markup in an IHtmlString, which will enable HTML markup to be
        /// rendered to the output without getting HTML encoded.
        /// </summary>
        /// <param name="value">HTML markup string.</param>
        /// <returns>An IHtmlString that represents HTML markup.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "For consistency, all helpers are instance methods.")]
        public HtmlString Raw(string value)
        {
            return new HtmlString(value);
        }

        /// <summary>
        /// Wraps HTML markup from the string representation of an object in an IHtmlString,
        /// which will enable HTML markup to be rendered to the output without getting HTML encoded.
        /// </summary>
        /// <param name="value">object with string representation as HTML markup</param>
        /// <returns>An IHtmlString that represents HTML markup.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "For consistency, all helpers are instance methods.")]
        public HtmlString Raw(object value)
        {
            return new HtmlString(value == null ? null : value.ToString());
        }

        //internal virtual void RenderPartialInternal<TModel>(string partialViewName, ViewData viewData, TModel model, TextWriter writer, IViewEngine viewEngine)
        //{
        //    if (String.IsNullOrEmpty(partialViewName))
        //    {
        //        throw new ArgumentException("MvcResources.Common_NullOrEmpty", "partialViewName");
        //    }

        //    ViewData<TModel> newViewData = null;

        //    if (model == null)
        //    {
        //        if (viewData == null)
        //        {
        //            newViewData = new ViewData<TModel>(ViewContext.ViewData);
        //        }
        //        else
        //        {
        //            newViewData = new ViewData<TModel>(viewData);
        //        }
        //    }
        //    else
        //    {
        //        if (viewData == null)
        //        {
        //            newViewData = new ViewData(model);
        //        }
        //        else
        //        {
        //            newViewData = new ViewData(viewData) { Model = model };
        //        }
        //    }

        //    ViewContext newViewContext = new ViewContext(ViewContext, ViewContext.View, newViewData, ViewContext.TempData, writer);
        //    IView view = FindPartialView(newViewContext, partialViewName, viewEngine);
        //    view.Render(newViewContext, writer);
        //}
    }
}
