// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Form-related extensions for <see cref="IHtmlHelper"/>.
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
    public static MvcForm BeginForm(this IHtmlHelper htmlHelper)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        // Generates <form action="{current url}" method="post">.
        return htmlHelper.BeginForm(
            actionName: null,
            controllerName: null,
            routeValues: null,
            method: FormMethod.Post,
            antiforgery: null,
            htmlAttributes: null);
    }

    /// <summary>
    /// Renders a &lt;form&gt; start tag to the response. The &lt;form&gt;'s <c>action</c> attribute value will
    /// match the current request.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="antiforgery">
    /// If <c>true</c>, &lt;form&gt; elements will include an antiforgery token.
    /// If <c>false</c>, suppresses the generation an &lt;input&gt; of type "hidden" with an antiforgery token.
    /// If <c>null</c>, &lt;form&gt; elements will include an antiforgery token.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    public static MvcForm BeginForm(this IHtmlHelper htmlHelper, bool? antiforgery)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        // Generates <form action="{current url}" method="post">.
        return htmlHelper.BeginForm(
            actionName: null,
            controllerName: null,
            routeValues: null,
            method: FormMethod.Post,
            antiforgery: antiforgery,
            htmlAttributes: null);
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
    public static MvcForm BeginForm(this IHtmlHelper htmlHelper, FormMethod method)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginForm(
            actionName: null,
            controllerName: null,
            routeValues: null,
            method: method,
            antiforgery: null,
            htmlAttributes: null);
    }

    /// <summary>
    /// Renders a &lt;form&gt; start tag to the response. When the user submits the form, the
    /// current action will process the request.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    public static MvcForm BeginForm(
        this IHtmlHelper htmlHelper,
        FormMethod method,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginForm(
            actionName: null,
            controllerName: null,
            routeValues: null,
            method: method,
            antiforgery: null,
            htmlAttributes: htmlAttributes);
    }

    /// <summary>
    /// Renders a &lt;form&gt; start tag to the response. When the user submits the form, the
    /// current action will process the request.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="method">The HTTP method for processing the form, either GET or POST.</param>
    /// <param name="antiforgery">
    /// If <c>true</c>, &lt;form&gt; elements will include an antiforgery token.
    /// If <c>false</c>, suppresses the generation an &lt;input&gt; of type "hidden" with an antiforgery token.
    /// If <c>null</c>, &lt;form&gt; elements will include an antiforgery token only if
    /// <paramref name="method"/> is not <see cref="FormMethod.Get"/>.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    public static MvcForm BeginForm(
        this IHtmlHelper htmlHelper,
        FormMethod method,
        bool? antiforgery,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginForm(
            actionName: null,
            controllerName: null,
            routeValues: null,
            method: method,
            antiforgery: antiforgery,
            htmlAttributes: htmlAttributes);
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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    public static MvcForm BeginForm(this IHtmlHelper htmlHelper, object routeValues)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginForm(
            actionName: null,
            controllerName: null,
            routeValues: routeValues,
            method: FormMethod.Post,
            antiforgery: null,
            htmlAttributes: null);
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
        this IHtmlHelper htmlHelper,
        string actionName,
        string controllerName)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginForm(
            actionName,
            controllerName,
            routeValues: null,
            method: FormMethod.Post,
            antiforgery: null,
            htmlAttributes: null);
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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    public static MvcForm BeginForm(
        this IHtmlHelper htmlHelper,
        string actionName,
        string controllerName,
        object routeValues)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginForm(
            actionName,
            controllerName,
            routeValues,
            FormMethod.Post,
            antiforgery: null,
            htmlAttributes: null);
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
        this IHtmlHelper htmlHelper,
        string actionName,
        string controllerName,
        FormMethod method)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginForm(
            actionName,
            controllerName,
            routeValues: null,
            method: method,
            antiforgery: null,
            htmlAttributes: null);
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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
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
        this IHtmlHelper htmlHelper,
        string actionName,
        string controllerName,
        object routeValues,
        FormMethod method)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginForm(
            actionName,
            controllerName,
            routeValues,
            method,
            antiforgery: null,
            htmlAttributes: null);
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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    public static MvcForm BeginForm(
        this IHtmlHelper htmlHelper,
        string actionName,
        string controllerName,
        FormMethod method,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginForm(
            actionName,
            controllerName,
            routeValues: null,
            method: method,
            antiforgery: null,
            htmlAttributes: htmlAttributes);
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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    public static MvcForm BeginRouteForm(this IHtmlHelper htmlHelper, object routeValues)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginRouteForm(
            routeName: null,
            routeValues: routeValues,
            method: FormMethod.Post,
            antiforgery: null,
            htmlAttributes: null);
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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <param name="antiforgery">
    /// If <c>true</c>, &lt;form&gt; elements will include an antiforgery token.
    /// If <c>false</c>, suppresses the generation an &lt;input&gt; of type "hidden" with an antiforgery token.
    /// If <c>null</c>, &lt;form&gt; elements will include an antiforgery token.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    public static MvcForm BeginRouteForm(this IHtmlHelper htmlHelper, object routeValues, bool? antiforgery)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginRouteForm(
            routeName: null,
            routeValues: routeValues,
            method: FormMethod.Post,
            antiforgery: antiforgery,
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
    public static MvcForm BeginRouteForm(this IHtmlHelper htmlHelper, string routeName)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginRouteForm(
            routeName,
            routeValues: null,
            method: FormMethod.Post,
            antiforgery: null,
            htmlAttributes: null);
    }

    /// <summary>
    /// Renders a &lt;form&gt; start tag to the response. The route with name <paramref name="routeName"/>
    /// generates the &lt;form&gt;'s <c>action</c> attribute value.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
    /// <param name="routeName">The name of the route.</param>
    /// <param name="antiforgery">
    /// If <c>true</c>, &lt;form&gt; elements will include an antiforgery token.
    /// If <c>false</c>, suppresses the generation an &lt;input&gt; of type "hidden" with an antiforgery token.
    /// If <c>null</c>, &lt;form&gt; elements will include an antiforgery token.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    public static MvcForm BeginRouteForm(this IHtmlHelper htmlHelper, string routeName, bool? antiforgery)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginRouteForm(
            routeName,
            routeValues: null,
            method: FormMethod.Post,
            antiforgery: antiforgery,
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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    public static MvcForm BeginRouteForm(
        this IHtmlHelper htmlHelper,
        string routeName,
        object routeValues)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginRouteForm(
            routeName,
            routeValues,
            FormMethod.Post,
            antiforgery: null,
            htmlAttributes: null);
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
        this IHtmlHelper htmlHelper,
        string routeName,
        FormMethod method)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginRouteForm(
            routeName,
            routeValues: null,
            method: method,
            antiforgery: null,
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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
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
        this IHtmlHelper htmlHelper,
        string routeName,
        object routeValues,
        FormMethod method)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginRouteForm(
            routeName,
            routeValues,
            method,
            antiforgery: null,
            htmlAttributes: null);
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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>
    /// An <see cref="MvcForm"/> instance which renders the &lt;/form&gt; end tag when disposed.
    /// </returns>
    /// <remarks>
    /// In this context, "renders" means the method writes its output using <see cref="ViewContext.Writer"/>.
    /// </remarks>
    public static MvcForm BeginRouteForm(
        this IHtmlHelper htmlHelper,
        string routeName,
        FormMethod method,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);

        return htmlHelper.BeginRouteForm(
            routeName,
            routeValues: null,
            method: method,
            antiforgery: null,
            htmlAttributes: htmlAttributes);
    }
}
