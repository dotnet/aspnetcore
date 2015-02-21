// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// DisplayName-related extensions for <see cref="IHtmlHelper"/>.
    /// </summary>
    public static class HtmlHelperFormExtensions
    {
        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. The &lt;form&gt;'s <c>action</c> attribute value will
        /// match the current request.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginForm([NotNull] this IHtmlHelper htmlHelper)
        {
            // Generates <form action="{current url}" method="post">.
            return htmlHelper.BeginForm(actionName: null, controllerName: null, routeValues: null,
                                        method: FormMethod.Post, htmlAttributes: null);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. When the user submits the form, the
        /// current action will process the request.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginForm([NotNull] this IHtmlHelper htmlHelper, FormMethod method)
        {
            return htmlHelper.BeginForm(actionName: null, controllerName: null, routeValues: null,
                                        method: method, htmlAttributes: null);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. When the user submits the form, the
        /// current action will process the request.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginForm(
            [NotNull] this IHtmlHelper htmlHelper,
            FormMethod method,
            object htmlAttributes)
        {
            return htmlHelper.BeginForm(actionName: null, controllerName: null, routeValues: null,
                                        method: method, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. When the user submits the form, the
        /// current action will process the request.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginForm([NotNull] this IHtmlHelper htmlHelper, object routeValues)
        {
            return htmlHelper.BeginForm(actionName: null, controllerName: null, routeValues: routeValues,
                                        method: FormMethod.Post, htmlAttributes: null);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. When the user submits the form, the action with name
        /// <paramref name="actionName"/> will process the request.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="actionName">The name of the action method.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string actionName,
            string controllerName)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues: null,
                                        method: FormMethod.Post, htmlAttributes: null);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. When the user submits the form, the action with name
        /// <paramref name="actionName"/> will process the request.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="actionName">The name of the action method.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string actionName,
            string controllerName,
            object routeValues)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues,
                                        FormMethod.Post, htmlAttributes: null);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. When the user submits the form, the action with name
        /// <paramref name="actionName"/> will process the request.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="actionName">The name of the action method.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string actionName,
            string controllerName,
            FormMethod method)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues: null,
                                        method: method, htmlAttributes: null);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. When the user submits the form, the action with name
        /// <paramref name="actionName"/> will process the request.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="actionName">The name of the action method.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string actionName,
            string controllerName,
            object routeValues,
            FormMethod method)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues,
                                        method, htmlAttributes: null);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. When the user submits the form, the action with name
        /// <paramref name="actionName"/> will process the request.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="actionName">The name of the action method.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string actionName,
            string controllerName,
            FormMethod method,
            object htmlAttributes)
        {
            return htmlHelper.BeginForm(actionName, controllerName, routeValues: null,
                                        method: method, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. The first route that can provide a URL with the
        /// specified <paramref name="routeValues"/> generates the &lt;form&gt;'s <c>action</c> attribute value.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginRouteForm([NotNull] this IHtmlHelper htmlHelper, object routeValues)
        {
            return htmlHelper.BeginRouteForm(
                routeName: null,
                routeValues: routeValues,
                method: FormMethod.Post,
                htmlAttributes: null);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. The route with name <paramref name="routeName"/>
        /// generates the &lt;form&gt;'s <c>action</c> attribute value.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginRouteForm([NotNull] this IHtmlHelper htmlHelper, string routeName)
        {
            return htmlHelper.BeginRouteForm(
                routeName,
                routeValues: null,
                method: FormMethod.Post,
                htmlAttributes: null);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. The route with name <paramref name="routeName"/>
        /// generates the &lt;form&gt;'s <c>action</c> attribute value.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginRouteForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string routeName,
            object routeValues)
        {
            return htmlHelper.BeginRouteForm(routeName, routeValues, FormMethod.Post, htmlAttributes: null);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. The route with name <paramref name="routeName"/>
        /// generates the &lt;form&gt;'s <c>action</c> attribute value.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginRouteForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string routeName,
            FormMethod method)
        {
            return htmlHelper.BeginRouteForm(routeName, routeValues: null, method: method, htmlAttributes: null);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. The route with name <paramref name="routeName"/>
        /// generates the &lt;form&gt;'s <c>action</c> attribute value.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginRouteForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string routeName,
            object routeValues,
            FormMethod method)
        {
            return htmlHelper.BeginRouteForm(routeName, routeValues, method, htmlAttributes: null);
        }

        /// <summary>
        /// Renders a &lt;form&gt; start tag to the response. The route with name <paramref name="routeName"/>
        /// generates the &lt;form&gt;'s <c>action</c> attribute value.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>
        /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
        /// </returns>
        /// <remarks>
        /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public static MvcForm BeginRouteForm(
            [NotNull] this IHtmlHelper htmlHelper,
            string routeName,
            FormMethod method,
            object htmlAttributes)
        {
            return htmlHelper.BeginRouteForm(
                routeName,
                routeValues: null,
                method: method,
                htmlAttributes: htmlAttributes);
        }
    }
}
