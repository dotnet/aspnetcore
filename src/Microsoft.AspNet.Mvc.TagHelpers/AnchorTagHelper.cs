// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;a&gt; elements.
    /// </summary>
    [TagName("a")]
    public class AnchorTagHelper : TagHelper
    {
        private const string RouteAttributePrefix = "route-";
        private const string Href = "href";

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal IHtmlGenerator Generator { get; set; }

        /// <summary>
        /// The name of the action method.
        /// </summary>
        /// <remarks>Must be <c>null</c> if <see cref="Route"/> is non-<c>null</c>.</remarks>
        public string Action { get; set; }

        /// <summary>
        /// The name of the controller.
        /// </summary>
        /// <remarks>Must be <c>null</c> if <see cref="Route"/> is non-<c>null</c>.</remarks>
        public string Controller { get; set; }

        /// <summary>
        /// The protocol for the URL, such as &quot;http&quot; or &quot;https&quot;.
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// The host name.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The URL fragment name.
        /// </summary>
        public string Fragment { get; set; }

        /// <summary>
        /// Name of the route.
        /// </summary>
        /// <remarks>
        /// Must be <c>null</c> if <see cref="Action"/> or <see cref="Controller"/> is non-<c>null</c>.
        /// </remarks>
        public string Route { get; set; }

        /// <inheritdoc />
        /// <remarks>Does nothing if user provides an "href" attribute. Throws an 
        /// <see cref="InvalidOperationException"/> if "href" attribute is provided and <see cref="Action"/>, 
        /// <see cref="Controller"/>, or <see cref="Route"/> are non-<c>null</c>.</remarks>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var routePrefixedAttributes = output.FindPrefixedAttributes(RouteAttributePrefix);

            // If there's an "href" on the tag it means it's being used as a normal anchor.
            if (output.Attributes.ContainsKey(Href))
            {
                if (Action != null || 
                    Controller != null || 
                    Route != null || 
                    Protocol != null || 
                    Host != null || 
                    Fragment != null || 
                    routePrefixedAttributes.Any())
                {
                    // User specified an href and one of the bound attributes; can't determine the href attribute.
                    throw new InvalidOperationException(
                        Resources.FormatAnchorTagHelper_CannotOverrideSpecifiedHref(
                            "<a>",
                            nameof(Action).ToLowerInvariant(),
                            nameof(Controller).ToLowerInvariant(),
                            nameof(Route).ToLowerInvariant(),
                            nameof(Protocol).ToLowerInvariant(),
                            nameof(Host).ToLowerInvariant(),
                            nameof(Fragment).ToLowerInvariant(),
                            RouteAttributePrefix,
                            Href));
                }
            }
            else
            {
                TagBuilder tagBuilder;
                var routeValues = GetRouteValues(output, routePrefixedAttributes);

                if (Route == null)
                {
                    tagBuilder = Generator.GenerateActionLink(linkText: string.Empty,
                                                              actionName: Action,
                                                              controllerName: Controller,
                                                              protocol: Protocol,
                                                              hostname: Host,
                                                              fragment: Fragment,
                                                              routeValues: routeValues,
                                                              htmlAttributes: null);
                }
                else if (Action != null || Controller != null)
                {
                    // Route and Action or Controller were specified. Can't determine the href attribute.
                    throw new InvalidOperationException(
                        Resources.FormatAnchorTagHelper_CannotDetermineHrefRouteActionOrControllerSpecified(
                            "<a>",
                            nameof(Route).ToLowerInvariant(),
                            nameof(Action).ToLowerInvariant(),
                            nameof(Controller).ToLowerInvariant(),
                            Href));
                }
                else
                {
                    tagBuilder = Generator.GenerateRouteLink(linkText: string.Empty,
                                                             routeName: Route,
                                                             protocol: Protocol,
                                                             hostName: Host,
                                                             fragment: Fragment,
                                                             routeValues: routeValues,
                                                             htmlAttributes: null);
                }

                if (tagBuilder != null)
                {
                    output.MergeAttributes(tagBuilder);
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