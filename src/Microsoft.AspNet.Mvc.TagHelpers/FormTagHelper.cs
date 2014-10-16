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
        private const string RouteAttributePrefix = "route-";

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal ViewContext ViewContext { get; set; }

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal IHtmlGenerator Generator { get; set; }

        /// <summary>
        /// The name of the action method.
        /// </summary>
        /// <remarks>
        /// If value contains a '/' this <see cref="ITagHelper"/> will do nothing.
        /// </remarks>
        public string Action { get; set; }

        /// <summary>
        /// The name of the controller.
        /// </summary>
        public string Controller { get; set; }

        /// <summary>
        /// The HTTP method for processing the form, either GET or POST.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Whether the anti-forgery token should be generated. Defaults to <c>true</c> if <see cref="Action"/> is not
        /// a URL, <c>false</c> otherwise.
        /// </summary>
        [HtmlAttributeName("anti-forgery")]
        public bool? AntiForgery { get; set; }

        /// <inheritdoc />
        /// <remarks>Does nothing if <see cref="Action"/> contains a '/'.</remarks>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            bool antiForgeryDefault = true;

            var routePrefixedAttributes = output.FindPrefixedAttributes(RouteAttributePrefix);

            // If Action contains a '/' it means the user is attempting to use the FormTagHelper as a normal form.
            if (Action != null && Action.Contains('/'))
            {
                if (Controller != null || routePrefixedAttributes.Any())
                {
                    // We don't know how to generate a form action since a Controller attribute was also provided.
                    throw new InvalidOperationException(
                        Resources.FormatFormTagHelper_CannotDetermineAction(
                            "<form>",
                            nameof(Action).ToLowerInvariant(),
                            nameof(Controller).ToLowerInvariant(),
                            RouteAttributePrefix));
                }

                // User is using the FormTagHelper like a normal <form> tag, anti-forgery default should be false to 
                // not force the anti-forgery token onto the user.
                antiForgeryDefault = false;

                // Restore Action, Method and Route HTML attributes if they were provided, user wants non-TagHelper <form>.
                output.CopyHtmlAttribute(nameof(Action), context);

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
                    // We don't want to do a full merge because we want the TagHelper content to take precedence.
                    output.Merge(tagBuilder);
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

        // TODO: We will not need this method once https://github.com/aspnet/Razor/issues/89 is completed.
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