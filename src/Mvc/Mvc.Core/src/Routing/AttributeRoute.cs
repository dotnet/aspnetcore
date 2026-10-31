// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.DependencyInjection;
using Resources = Microsoft.AspNetCore.Mvc.Core.Resources;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal sealed class AttributeRoute : IRouter
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private readonly IServiceProvider _services;
    private readonly Func<ActionDescriptor[], IRouter> _handlerFactory;

    private TreeRouter? _router;

    public AttributeRoute(
        IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
        IServiceProvider services,
        Func<ActionDescriptor[], IRouter> handlerFactory)
    {
        ArgumentNullException.ThrowIfNull(actionDescriptorCollectionProvider);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handlerFactory);

        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        _services = services;
        _handlerFactory = handlerFactory;
    }

    /// <inheritdoc />
    public VirtualPathData GetVirtualPath(VirtualPathContext context)
    {
        var router = GetTreeRouter();
        return router.GetVirtualPath(context);
    }

    /// <inheritdoc />
    public Task RouteAsync(RouteContext context)
    {
        var router = GetTreeRouter();
        return router.RouteAsync(context);
    }

    private TreeRouter GetTreeRouter()
    {
        var actions = _actionDescriptorCollectionProvider.ActionDescriptors;

        // This is a safe-race. We'll never set router back to null after initializing
        // it on startup.
        if (_router == null || _router.Version != actions.Version)
        {
            var builder = _services.GetRequiredService<TreeRouteBuilder>();
            AddEntries(builder, actions);
            _router = builder.Build(actions.Version);
        }

        return _router;
    }

    // internal for testing
    internal void AddEntries(TreeRouteBuilder builder, ActionDescriptorCollection actions)
    {
        var routeInfos = GetRouteInfos(actions.Items);

        // We're creating one TreeRouteLinkGenerationEntry per action. This allows us to match the intended
        // action by expected route values, and then use the TemplateBinder to generate the link.
        foreach (var routeInfo in routeInfos)
        {
            if (routeInfo.SuppressLinkGeneration)
            {
                continue;
            }

            var defaults = new RouteValueDictionary();
            foreach (var kvp in routeInfo.ActionDescriptor.RouteValues)
            {
                defaults.Add(kvp.Key, kvp.Value);
            }

            try
            {
                // We use the `NullRouter` as the route handler because we don't need to do anything for link
                // generations. The TreeRouter does it all for us.
                builder.MapOutbound(
                    NullRouter.Instance,
                    routeInfo.RouteTemplate,
                    defaults,
                    routeInfo.RouteName,
                    routeInfo.Order);
            }
            catch (RouteCreationException routeCreationException)
            {
                throw new RouteCreationException(
                    "An error occurred while adding a route to the route builder. " +
                    $"Route name '{routeInfo.RouteName}' and template '{routeInfo.RouteTemplate!.TemplateText}'.",
                    routeCreationException);
            }
        }

        // We're creating one AttributeRouteMatchingEntry per group, so we need to identify the distinct set of
        // groups. It's guaranteed that all members of the group have the same template and precedence,
        // so we only need to hang on to a single instance of the RouteInfo for each group.
        var groups = GetInboundRouteGroups(routeInfos);
        foreach (var group in groups)
        {
            var handler = _handlerFactory(group.ToArray());

            // Note that because we only support 'inline' defaults, each routeInfo group also has the same
            // set of defaults.
            //
            // We then inject the route group as a default for the matcher so it gets passed back to MVC
            // for use in action selection.
            builder.MapInbound(
                handler,
                group.Key.RouteTemplate,
                group.Key.RouteName,
                group.Key.Order);
        }
    }

    private static IEnumerable<IGrouping<RouteInfo, ActionDescriptor>> GetInboundRouteGroups(List<RouteInfo> routeInfos)
    {
        return routeInfos
            .Where(routeInfo => !routeInfo.SuppressPathMatching)
            .GroupBy(r => r, r => r.ActionDescriptor, RouteInfoEqualityComparer.Instance);
    }

    private static List<RouteInfo> GetRouteInfos(IReadOnlyList<ActionDescriptor> actions)
    {
        var routeInfos = new List<RouteInfo>();
        var errors = new List<RouteInfo>();

        // This keeps a cache of 'Template' objects. It's a fairly common case that multiple actions
        // will use the same route template string; thus, the `Template` object can be shared.
        //
        // For a relatively simple route template, the `Template` object will hold about 500 bytes
        // of memory, so sharing is worthwhile.
        var templateCache = new Dictionary<string, RouteTemplate>(StringComparer.OrdinalIgnoreCase);

        var attributeRoutedActions = actions.Where(a => a.AttributeRouteInfo?.Template != null);
        foreach (var action in attributeRoutedActions)
        {
            var routeInfo = GetRouteInfo(templateCache, action);
            if (routeInfo.ErrorMessage == null)
            {
                routeInfos.Add(routeInfo);
            }
            else
            {
                errors.Add(routeInfo);
            }
        }

        if (errors.Count > 0)
        {
            var allErrors = string.Join(
                Environment.NewLine + Environment.NewLine,
                errors.Select(
                    e => Resources.FormatAttributeRoute_IndividualErrorMessage(
                        e.ActionDescriptor.DisplayName,
                        Environment.NewLine,
                        e.ErrorMessage)));

            var message = Resources.FormatAttributeRoute_AggregateErrorMessage(Environment.NewLine, allErrors);
            throw new RouteCreationException(message);
        }

        return routeInfos;
    }

    private static RouteInfo GetRouteInfo(
        Dictionary<string, RouteTemplate> templateCache,
        ActionDescriptor action)
    {
        var routeInfo = new RouteInfo()
        {
            ActionDescriptor = action,
        };

        try
        {
            var template = action.AttributeRouteInfo!.Template!;
            if (!templateCache.TryGetValue(template, out var parsedTemplate))
            {
                // Parsing with throw if the template is invalid.
                parsedTemplate = TemplateParser.Parse(template);
                templateCache.Add(template, parsedTemplate);
            }

            routeInfo.RouteTemplate = parsedTemplate;
            routeInfo.SuppressPathMatching = action.AttributeRouteInfo.SuppressPathMatching;
            routeInfo.SuppressLinkGeneration = action.AttributeRouteInfo.SuppressLinkGeneration;
        }
        catch (Exception ex)
        {
            routeInfo.ErrorMessage = ex.Message;
            return routeInfo;
        }

        foreach (var kvp in action.RouteValues)
        {
            foreach (var parameter in routeInfo.RouteTemplate.Parameters)
            {
                if (string.Equals(kvp.Key, parameter.Name, StringComparison.OrdinalIgnoreCase))
                {
                    routeInfo.ErrorMessage = Resources.FormatAttributeRoute_CannotContainParameter(
                        routeInfo.RouteTemplate.TemplateText,
                        kvp.Key,
                        kvp.Value);

                    return routeInfo;
                }
            }
        }

        routeInfo.Order = action.AttributeRouteInfo.Order;
        routeInfo.RouteName = action.AttributeRouteInfo.Name;

        return routeInfo;
    }

    private sealed class RouteInfo
    {
        public ActionDescriptor ActionDescriptor { get; init; } = default!;

        public string? ErrorMessage { get; set; }

        public int Order { get; set; }

        public string? RouteName { get; set; }

        public RouteTemplate? RouteTemplate { get; set; }

        public bool SuppressPathMatching { get; set; }

        public bool SuppressLinkGeneration { get; set; }
    }

    private sealed class RouteInfoEqualityComparer : IEqualityComparer<RouteInfo>
    {
        public static readonly RouteInfoEqualityComparer Instance = new();

        public bool Equals(RouteInfo? x, RouteInfo? y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            else if (x == null || y == null)
            {
                return false;
            }
            else if (x.Order != y.Order)
            {
                return false;
            }
            else
            {
                return string.Equals(
                    x.RouteTemplate!.TemplateText,
                    y.RouteTemplate!.TemplateText,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        public int GetHashCode(RouteInfo obj)
        {
            if (obj == null)
            {
                return 0;
            }

            var hash = new HashCode();
            hash.Add(obj.Order);
            hash.Add(obj.RouteTemplate!.TemplateText, StringComparer.OrdinalIgnoreCase);
            return hash.ToHashCode();
        }
    }

    // Used only to hook up link generation, and it doesn't need to do anything.
    private sealed class NullRouter : IRouter
    {
        public static readonly NullRouter Instance = new NullRouter();

        public VirtualPathData? GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            throw new NotImplementedException();
        }
    }
}
