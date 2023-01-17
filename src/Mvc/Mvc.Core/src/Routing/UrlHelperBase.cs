// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// An abstraction for <see cref="IUrlHelper" />.
/// </summary>
public abstract class UrlHelperBase : IUrlHelper
{
    // Perf: Share the StringBuilder object across multiple calls of GenerateURL for this UrlHelper
    private StringBuilder? _stringBuilder;

    // Perf: Reuse the RouteValueDictionary across multiple calls of Action for this UrlHelper
    private readonly RouteValueDictionary _routeValueDictionary;

    /// <summary>
    /// Initializes an instance of a <see cref="UrlHelperBase"/>
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    protected UrlHelperBase(ActionContext actionContext)
    {
        ArgumentNullException.ThrowIfNull(actionContext);

        ActionContext = actionContext;
        AmbientValues = actionContext.RouteData.Values;
        _routeValueDictionary = new RouteValueDictionary();
    }

    /// <summary>
    /// Gets the <see cref="RouteValueDictionary"/> associated with the current request.
    /// </summary>
    protected RouteValueDictionary AmbientValues { get; }

    /// <inheritdoc />
    public ActionContext ActionContext { get; }

    /// <inheritdoc />
    public virtual bool IsLocalUrl([NotNullWhen(true)][StringSyntax(StringSyntaxAttribute.Uri)] string? url) => CheckIsLocalUrl(url);

    /// <inheritdoc />
    [return: NotNullIfNotNull("contentPath")]
    public virtual string? Content(string? contentPath) => Content(ActionContext.HttpContext, contentPath);

    /// <inheritdoc />
    public virtual string? Link(string? routeName, object? values)
    {
        return RouteUrl(new UrlRouteContext()
        {
            RouteName = routeName,
            Values = values,
            Protocol = ActionContext.HttpContext.Request.Scheme,
            Host = ActionContext.HttpContext.Request.Host.ToUriComponent()
        });
    }

    /// <inheritdoc />
    public abstract string? Action(UrlActionContext actionContext);

    /// <inheritdoc />
    public abstract string? RouteUrl(UrlRouteContext routeContext);

    /// <summary>
    /// Gets a <see cref="RouteValueDictionary"/> using the specified values.
    /// </summary>
    /// <param name="values">The values to use.</param>
    /// <returns>A <see cref="RouteValueDictionary"/> with the specified values.</returns>
    protected RouteValueDictionary GetValuesDictionary(object? values)
    {
        // Perf: RouteValueDictionary can be cast to IDictionary<string, object>, but it is
        // special cased to avoid allocating boxed Enumerator.
        if (values is RouteValueDictionary routeValuesDictionary)
        {
            _routeValueDictionary.Clear();
            foreach (var kvp in routeValuesDictionary)
            {
                _routeValueDictionary.Add(kvp.Key, kvp.Value);
            }

            return _routeValueDictionary;
        }

        if (values is IDictionary<string, object> dictionaryValues)
        {
            _routeValueDictionary.Clear();
            foreach (var kvp in dictionaryValues)
            {
                _routeValueDictionary.Add(kvp.Key, kvp.Value);
            }

            return _routeValueDictionary;
        }

        return new RouteValueDictionary(values);
    }

    /// <summary>
    /// Generate a url using the specified values.
    /// </summary>
    /// <param name="protocol">The protocol.</param>
    /// <param name="host">The host.</param>
    /// <param name="virtualPath">The virtual path.</param>
    /// <param name="fragment">The fragment.</param>
    /// <returns>The generated url</returns>
    protected string? GenerateUrl(string? protocol, string? host, string? virtualPath, string? fragment)
    {
        if (virtualPath == null)
        {
            return null;
        }

        // Perf: In most of the common cases, GenerateUrl is called with a null protocol, host and fragment.
        // In such cases, we might not need to build any URL as the url generated is mostly same as the virtual path available in pathData.
        // For such common cases, this FastGenerateUrl method saves a string allocation per GenerateUrl call.
        if (TryFastGenerateUrl(protocol, host, virtualPath, fragment, out var url))
        {
            return url;
        }

        var builder = GetStringBuilder();
        try
        {
            var pathBase = ActionContext.HttpContext.Request.PathBase;

            if (string.IsNullOrEmpty(protocol) && string.IsNullOrEmpty(host))
            {
                AppendPathAndFragment(builder, pathBase, virtualPath, fragment);
                // We're returning a partial URL (just path + query + fragment), but we still want it to be rooted.
                if (builder.Length == 0 || builder[0] != '/')
                {
                    builder.Insert(0, '/');
                }
            }
            else
            {
                protocol = string.IsNullOrEmpty(protocol) ? "http" : protocol;
                builder.Append(protocol);

                builder.Append(Uri.SchemeDelimiter);

                host = string.IsNullOrEmpty(host) ? ActionContext.HttpContext.Request.Host.Value : host;
                builder.Append(host);
                AppendPathAndFragment(builder, pathBase, virtualPath, fragment);
            }

            var path = builder.ToString();
            return path;
        }
        finally
        {
            // Clear the StringBuilder so that it can reused for the next call.
            builder.Clear();
        }
    }

    /// <summary>
    /// Generates a URI from the provided components.
    /// </summary>
    /// <param name="protocol">The URI scheme/protocol.</param>
    /// <param name="host">The URI host.</param>
    /// <param name="path">The URI path and remaining portions (path, query, and fragment).</param>
    /// <returns>
    /// An absolute URI if the <paramref name="protocol"/> or <paramref name="host"/> is specified, otherwise generates a
    /// URI with an absolute path.
    /// </returns>
    protected string? GenerateUrl(string? protocol, string? host, string? path)
    {
        // This method is similar to GenerateUrl, but it's used for EndpointRouting. It ignores pathbase and fragment
        // because those have already been incorporated.
        if (path == null)
        {
            return null;
        }

        // Perf: In most of the common cases, GenerateUrl is called with a null protocol, host and fragment.
        // In such cases, we might not need to build any URL as the url generated is mostly same as the virtual path available in pathData.
        // For such common cases, this FastGenerateUrl method saves a string allocation per GenerateUrl call.
        if (TryFastGenerateUrl(protocol, host, path, fragment: null, out var url))
        {
            return url;
        }

        var builder = GetStringBuilder();
        try
        {
            if (string.IsNullOrEmpty(protocol) && string.IsNullOrEmpty(host))
            {
                AppendPathAndFragment(builder, pathBase: null, path, fragment: null);

                // We're returning a partial URL (just path + query + fragment), but we still want it to be rooted.
                if (builder.Length == 0 || builder[0] != '/')
                {
                    builder.Insert(0, '/');
                }
            }
            else
            {
                protocol = string.IsNullOrEmpty(protocol) ? "http" : protocol;
                builder.Append(protocol);

                builder.Append(Uri.SchemeDelimiter);

                host = string.IsNullOrEmpty(host) ? ActionContext.HttpContext.Request.Host.Value : host;
                builder.Append(host);
                AppendPathAndFragment(builder, pathBase: null, path, fragment: null);
            }

            return builder.ToString();
        }
        finally
        {
            // Clear the StringBuilder so that it can reused for the next call.
            builder.Clear();
        }
    }

    internal static void NormalizeRouteValuesForAction(
        string? action,
        string? controller,
        RouteValueDictionary values,
        RouteValueDictionary? ambientValues)
    {
        object? obj = null;
        if (action == null)
        {
            if (!values.ContainsKey("action") &&
                (ambientValues?.TryGetValue("action", out obj) ?? false))
            {
                values["action"] = obj;
            }
        }
        else
        {
            values["action"] = action;
        }

        if (controller == null)
        {
            if (!values.ContainsKey("controller") &&
                (ambientValues?.TryGetValue("controller", out obj) ?? false))
            {
                values["controller"] = obj;
            }
        }
        else
        {
            values["controller"] = controller;
        }
    }

    internal static void NormalizeRouteValuesForPage(
        ActionContext? context,
        string? page,
        string? handler,
        RouteValueDictionary values,
        RouteValueDictionary? ambientValues)
    {
        object? value = null;
        if (string.IsNullOrEmpty(page))
        {
            if (!values.ContainsKey("page") &&
                (ambientValues?.TryGetValue("page", out value) ?? false))
            {
                values["page"] = value;
            }
        }
        else
        {
            values["page"] = CalculatePageName(context, ambientValues, page);
        }

        if (string.IsNullOrEmpty(handler))
        {
            if (!values.ContainsKey("handler") &&
                (ambientValues?.ContainsKey("handler") ?? false))
            {
                // Clear out form action unless it's explicitly specified in the routeValues.
                values["handler"] = null;
            }
        }
        else
        {
            values["handler"] = handler;
        }
    }

    [return: NotNullIfNotNull("contentPath")]
    internal static string? Content(HttpContext httpContext, string? contentPath)
    {
        if (string.IsNullOrEmpty(contentPath))
        {
            return null;
        }
        else if (contentPath[0] == '~')
        {
            var segment = new PathString(contentPath.Substring(1));
            var applicationPath = httpContext.Request.PathBase;

            var path = applicationPath.Add(segment);
            Debug.Assert(path.HasValue);
            return path.Value;
        }

        return contentPath;
    }

    internal static bool CheckIsLocalUrl([NotNullWhen(true)] string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        // Allows "/" or "/foo" but not "//" or "/\".
        if (url[0] == '/')
        {
            // url is exactly "/"
            if (url.Length == 1)
            {
                return true;
            }

            // url doesn't start with "//" or "/\"
            if (url[1] != '/' && url[1] != '\\')
            {
                return !HasControlCharacter(url.AsSpan(1));
            }

            return false;
        }

        // Allows "~/" or "~/foo" but not "~//" or "~/\".
        if (url[0] == '~' && url.Length > 1 && url[1] == '/')
        {
            // url is exactly "~/"
            if (url.Length == 2)
            {
                return true;
            }

            // url doesn't start with "~//" or "~/\"
            if (url[2] != '/' && url[2] != '\\')
            {
                return !HasControlCharacter(url.AsSpan(2));
            }

            return false;
        }

        return false;

        static bool HasControlCharacter(ReadOnlySpan<char> readOnlySpan)
        {
            // URLs may not contain ASCII control characters.
            for (var i = 0; i < readOnlySpan.Length; i++)
            {
                if (char.IsControl(readOnlySpan[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static object CalculatePageName(ActionContext? context, RouteValueDictionary? ambientValues, string pageName)
    {
        Debug.Assert(pageName.Length > 0);
        // Paths not qualified with a leading slash are treated as relative to the current page.
        if (pageName[0] != '/')
        {
            // OK now we should get the best 'normalized' version of the page route value that we can.
            string? currentPagePath;
            if (context != null)
            {
                currentPagePath = NormalizedRouteValue.GetNormalizedRouteValue(context, "page");
            }
            else if (ambientValues != null)
            {
                currentPagePath = Convert.ToString(ambientValues["page"], CultureInfo.InvariantCulture);
            }
            else
            {
                currentPagePath = null;
            }

            if (string.IsNullOrEmpty(currentPagePath))
            {
                // Disallow the use sibling page routing, a Razor page specific feature, from a non-page action.
                // OR - this is a call from LinkGenerator where the HttpContext was not specified.
                //
                // We can't use a relative path in either case, because we don't know the base path.
                throw new InvalidOperationException(Resources.FormatUrlHelper_RelativePagePathIsNotSupported(
                    pageName,
                    nameof(LinkGenerator),
                    nameof(HttpContext)));
            }

            return ViewEnginePath.CombinePath(currentPagePath, pageName);
        }

        return pageName;
    }

    // for unit testing
    internal static void AppendPathAndFragment(StringBuilder builder, PathString pathBase, string virtualPath, string? fragment)
    {
        if (!pathBase.HasValue)
        {
            if (virtualPath.Length == 0)
            {
                builder.Append('/');
            }
            else
            {
                if (!virtualPath.StartsWith('/'))
                {
                    builder.Append('/');
                }

                builder.Append(virtualPath);
            }
        }
        else
        {
            if (virtualPath.Length == 0)
            {
                builder.Append(pathBase.Value);
            }
            else
            {
                builder.Append(pathBase.Value);

                if (pathBase.Value.EndsWith('/'))
                {
                    builder.Length--;
                }

                if (!virtualPath.StartsWith('/'))
                {
                    builder.Append('/');
                }

                builder.Append(virtualPath);
            }
        }

        if (!string.IsNullOrEmpty(fragment))
        {
            builder.Append('#').Append(fragment);
        }
    }

    private bool TryFastGenerateUrl(
        string? protocol,
        string? host,
        string virtualPath,
        string? fragment,
        [NotNullWhen(true)] out string? url)
    {
        var pathBase = ActionContext.HttpContext.Request.PathBase;
        url = null;

        if (string.IsNullOrEmpty(protocol)
            && string.IsNullOrEmpty(host)
            && string.IsNullOrEmpty(fragment)
            && !pathBase.HasValue)
        {
            if (virtualPath.Length == 0)
            {
                url = "/";
                return true;
            }
            else if (virtualPath.StartsWith('/'))
            {
                url = virtualPath;
                return true;
            }
        }

        return false;
    }

    private StringBuilder GetStringBuilder()
    {
        if (_stringBuilder == null)
        {
            _stringBuilder = new StringBuilder();
        }

        return _stringBuilder;
    }
}
