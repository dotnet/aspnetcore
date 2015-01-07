// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;form&gt; elements.
    /// </summary>
    [ContentBehavior(ContentBehavior.Append)]
    public class FormTagHelper : TagHelper
    {
        private const string ActionAttributeName = "asp-action";
        private const string AntiForgeryAttributeName = "asp-anti-forgery";
        private const string ControllerAttributeName = "asp-controller";
        private const string RouteAttributePrefix = "asp-route-";
        private const string HtmlActionAttributeName = "action";

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal ViewContext ViewContext { get; set; }

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal IHtmlGenerator Generator { get; set; }

        /// <summary>
        /// The name of the action method.
        /// </summary>
        [HtmlAttributeName(ActionAttributeName)]
        public string Action { get; set; }

        /// <summary>
        /// The name of the controller.
        /// </summary>
        [HtmlAttributeName(ControllerAttributeName)]
        public string Controller { get; set; }

        /// <summary>
        /// The HTTP method for processing the form, either GET or POST.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Whether the anti-forgery token should be generated. 
        /// </summary>
        /// <value>Defaults to <c>false</c> if user provides an <c>action</c> attribute; <c>true</c> otherwise.</value>
        [HtmlAttributeName(AntiForgeryAttributeName)]
        public bool? AntiForgery { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Does nothing if user provides an <c>action</c> attribute and <see cref="AntiForgery"/> is <c>null</c> or
        /// <c>false</c>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <c>action</c> attribute is provided and <see cref="Action"/> or <see cref="Controller"/> are
        /// non-<c>null</c> or if the user provided <c>asp-route-*</c> attributes.
        /// </exception>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var antiForgeryDefault = true;
            var routePrefixedAttributes = output.FindPrefixedAttributes(RouteAttributePrefix);

            // If "action" is already set, it means the user is attempting to use a normal <form>.
            if (output.Attributes.ContainsKey(HtmlActionAttributeName))
            {
                if (Action != null || Controller != null || routePrefixedAttributes.Any())
                {
                    // User also specified bound attributes we cannot use.
                    throw new InvalidOperationException(
                        Resources.FormatFormTagHelper_CannotOverrideAction(
                            "<form>",
                            HtmlActionAttributeName,
                            ActionAttributeName,
                            ControllerAttributeName,
                            RouteAttributePrefix));
                }

                // User is using the FormTagHelper like a normal <form> tag. Anti-forgery default should be false to
                // not force the anti-forgery token on the user.
                antiForgeryDefault = false;

                // Restore method attribute.
                if (Method != null)
                {
                    output.CopyHtmlAttribute(nameof(Method), context);
                }
            }
            else
            {
                var routeValues = GetRouteValues(output, routePrefixedAttributes);
                var tagBuilder = Generator.GenerateForm(ViewContext,
                                                        Action,
                                                        Controller,
                                                        routeValues,
                                                        Method,
                                                        htmlAttributes: null);

                if (tagBuilder != null)
                {
                    output.MergeAttributes(tagBuilder);
                    output.Content += tagBuilder.InnerHtml;
                    output.SelfClosing = false;
                }
            }

            if (AntiForgery ?? antiForgeryDefault)
            {
                var antiForgeryTagBuilder = Generator.GenerateAntiForgery(ViewContext);
                if (antiForgeryTagBuilder != null)
                {
                    output.Content += antiForgeryTagBuilder.ToString(TagRenderMode.SelfClosing);
                }
            }
        }

        // TODO: https://github.com/aspnet/Razor/issues/89 - We will not need this method once #89 is completed.
        private static Dictionary<string, object> GetRouteValues(
            TagHelperOutput output, IEnumerable<KeyValuePair<string, string>> routePrefixedAttributes)
        {
            Dictionary<string, object> routeValues = null;
            if (routePrefixedAttributes.Any())
            {
                // Prefixed values should be treated as bound attributes, remove them from the output.
                output.RemoveRange(routePrefixedAttributes);

                // Generator.GenerateForm does not accept a Dictionary<string, string> for route values.
                routeValues = routePrefixedAttributes.ToDictionary(
                    attribute => attribute.Key.Substring(RouteAttributePrefix.Length),
                    attribute => (object)attribute.Value);
            }

            return routeValues;
        }
    }
}