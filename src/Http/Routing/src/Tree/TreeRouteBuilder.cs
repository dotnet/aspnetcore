// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using System.Data;
#if !COMPONENTS
using Microsoft.Extensions.ObjectPool;
#else
using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Components.Routing;
#endif
namespace Microsoft.AspNetCore.Routing.Tree;

#if !COMPONENTS
/// <summary>
/// Builder for <see cref="TreeRouter"/> instances.
/// </summary>
public class TreeRouteBuilder
#else
internal class TreeRouteBuilder
#endif
{
    private readonly ILogger _logger;
    private readonly ILogger _constraintLogger;
    private readonly UrlEncoder _urlEncoder;
#if !COMPONENTS
    private readonly ObjectPool<UriBuildingContext> _objectPool;
#endif
    private readonly IInlineConstraintResolver _constraintResolver;

#if !COMPONENTS
    /// <summary>
    /// Initializes a new instance of <see cref="TreeRouteBuilder"/>.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="objectPool">The <see cref="ObjectPool{UrlBuildingContext}"/>.</param>
    /// <param name="constraintResolver">The <see cref="IInlineConstraintResolver"/>.</param>
#endif
    internal TreeRouteBuilder(
        ILoggerFactory loggerFactory,
#if !COMPONENTS
        ObjectPool<UriBuildingContext> objectPool,
#endif
        IInlineConstraintResolver constraintResolver)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
#if !COMPONENTS
        ArgumentNullException.ThrowIfNull(objectPool);
#endif
        ArgumentNullException.ThrowIfNull(constraintResolver);

        _urlEncoder = UrlEncoder.Default;
#if !COMPONENTS
        _objectPool = objectPool;
#endif
        _constraintResolver = constraintResolver;

        _logger = loggerFactory.CreateLogger<TreeRouter>();
        _constraintLogger = loggerFactory.CreateLogger(typeof(RouteConstraintMatcher).FullName);
    }

#if !COMPONENTS
    /// <summary>
    /// Adds a new inbound route to the <see cref="TreeRouter"/>.
    /// </summary>
    /// <param name="handler">The <see cref="IRouter"/> for handling the route.</param>
    /// <param name="routeTemplate">The <see cref="RouteTemplate"/> of the route.</param>
    /// <param name="routeName">The route name.</param>
    /// <param name="order">The route order.</param>
    /// <returns>The <see cref="InboundRouteEntry"/>.</returns>
#endif
    public InboundRouteEntry MapInbound(
#if !COMPONENTS
        IRouter handler,
        RouteTemplate routeTemplate,
        string routeName,
        int order)
#else
        [DynamicallyAccessedMembers(Component)] Type handler,
        RoutePattern routeTemplate,
        List<string> unusedParameterNames)
#endif
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(routeTemplate);

        var entry = new InboundRouteEntry()
        {
            Handler = handler,
#if !COMPONENTS
            Order = order,
#endif
            Precedence = RoutePrecedence.ComputeInbound(routeTemplate),
#if !COMPONENTS
            RouteName = routeName,
            RouteTemplate = routeTemplate,
#else
            RoutePattern = routeTemplate,
            UnusedRouteParameterNames = unusedParameterNames
#endif
        };

#if !COMPONENTS
        var constraintBuilder = new RouteConstraintBuilder(_constraintResolver, routeTemplate.TemplateText);
#else
        var constraintBuilder = new RouteConstraintBuilder(_constraintResolver, routeTemplate.RawText);
#endif
        foreach (var parameter in routeTemplate.Parameters)
        {
#if !COMPONENTS
            if (parameter.InlineConstraints != null)
            {
                if (parameter.IsOptional)
                {
                    constraintBuilder.SetOptional(parameter.Name);
                }
                foreach (var constraint in parameter.InlineConstraints)
                {
                    constraintBuilder.AddResolvedConstraint(parameter.Name, constraint.Constraint);
                }
            }
#else
            if (parameter.ParameterPolicies != null)
            {
                if (parameter.IsOptional)
                {
                    constraintBuilder.SetOptional(parameter.Name);
                }
                foreach (var policy in parameter.ParameterPolicies)
                {
                    constraintBuilder.AddResolvedConstraint(parameter.Name, policy.Content);
                }
            }
#endif
        }

        entry.Constraints = constraintBuilder.Build();
        entry.Defaults = new RouteValueDictionary();

#if !COMPONENTS
        foreach (var parameter in entry.RouteTemplate.Parameters)
        {
            if (parameter.DefaultValue != null)
            {
                entry.Defaults.Add(parameter.Name, parameter.DefaultValue);
            }
        }
#else
        foreach (var parameter in entry.RoutePattern.Parameters)
        {
            if (parameter.Default != null)
            {
                entry.Defaults.Add(parameter.Name, parameter.Default);
            }
        }
#endif

        InboundEntries.Add(entry);
        return entry;
    }

#if !COMPONENTS
    /// <summary>
    /// Adds a new outbound route to the <see cref="TreeRouter"/>.
    /// </summary>
    /// <param name="handler">The <see cref="IRouter"/> for handling the link generation.</param>
    /// <param name="routeTemplate">The <see cref="RouteTemplate"/> of the route.</param>
    /// <param name="requiredLinkValues">The <see cref="RouteValueDictionary"/> containing the route values.</param>
    /// <param name="routeName">The route name.</param>
    /// <param name="order">The route order.</param>
    /// <returns>The <see cref="OutboundRouteEntry"/>.</returns>
    public OutboundRouteEntry MapOutbound(
        IRouter handler,
        RouteTemplate routeTemplate,
        RouteValueDictionary requiredLinkValues,
        string routeName,
        int order)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(routeTemplate);
        ArgumentNullException.ThrowIfNull(requiredLinkValues);

        var entry = new OutboundRouteEntry()
        {
            Handler = handler,
            Order = order,
            Precedence = RoutePrecedence.ComputeOutbound(routeTemplate),
            RequiredLinkValues = requiredLinkValues,
            RouteName = routeName,
            RouteTemplate = routeTemplate,
        };

        var constraintBuilder = new RouteConstraintBuilder(_constraintResolver, routeTemplate.TemplateText);
        foreach (var parameter in routeTemplate.Parameters)
        {
            if (parameter.InlineConstraints != null)
            {
                if (parameter.IsOptional)
                {
                    constraintBuilder.SetOptional(parameter.Name);
                }

                foreach (var constraint in parameter.InlineConstraints)
                {
                    constraintBuilder.AddResolvedConstraint(parameter.Name, constraint.Constraint);
                }
            }
        }

        entry.Constraints = constraintBuilder.Build();

        entry.Defaults = new RouteValueDictionary();
        foreach (var parameter in entry.RouteTemplate.Parameters)
        {
            if (parameter.DefaultValue != null)
            {
                entry.Defaults.Add(parameter.Name, parameter.DefaultValue);
            }
        }

        OutboundEntries.Add(entry);
        return entry;
    }

    /// <summary>
    /// Gets the list of <see cref="InboundRouteEntry"/>.
    /// </summary>
#endif
    public IList<InboundRouteEntry> InboundEntries { get; } = new List<InboundRouteEntry>();

#if !COMPONENTS
    /// <summary>
    /// Gets the list of <see cref="OutboundRouteEntry"/>.
    /// </summary>
    public IList<OutboundRouteEntry> OutboundEntries { get; } = new List<OutboundRouteEntry>();

    /// <summary>
    /// Builds a <see cref="TreeRouter"/> with the <see cref="InboundEntries"/>
    /// and <see cref="OutboundEntries"/> defined in this <see cref="TreeRouteBuilder"/>.
    /// </summary>
    /// <returns>The <see cref="TreeRouter"/>.</returns>
#endif
    public TreeRouter Build()
    {
        return Build(version: 0);
    }

#if !COMPONENTS
    /// <summary>
    /// Builds a <see cref="TreeRouter"/> with the <see cref="InboundEntries"/>
    /// and <see cref="OutboundEntries"/> defined in this <see cref="TreeRouteBuilder"/>.
    /// </summary>
    /// <param name="version">The version of the <see cref="TreeRouter"/>.</param>
    /// <returns>The <see cref="TreeRouter"/>.</returns>
#endif
    public TreeRouter Build(int version)
    {
        // Tree route builder builds a tree for each of the different route orders defined by
        // the user. When a route needs to be matched, the matching algorithm in tree router
        // just iterates over the trees in ascending order when it tries to match the route.
        var trees = new Dictionary<int, UrlMatchingTree>();

        foreach (var entry in InboundEntries)
        {
            if (!trees.TryGetValue(entry.Order, out var tree))
            {
                tree = new UrlMatchingTree(entry.Order);
                trees.Add(entry.Order, tree);
            }

            tree.AddEntry(entry);
        }

#if !COMPONENTS
        return new TreeRouter(
            trees.Values.OrderBy(tree => tree.Order).ToArray(),
            OutboundEntries,
            _urlEncoder,
            _objectPool,
            _logger,
            _constraintLogger,
            version);
#else
        return new TreeRouter(
            trees.Values.OrderBy(tree => tree.Order).ToArray(),
            _urlEncoder,
            _logger,
            version);
#endif
    }

#if !COMPONENTS
    /// <summary>
    /// Removes all <see cref="InboundEntries"/> and <see cref="OutboundEntries"/> from this
    /// <see cref="TreeRouteBuilder"/>.
    /// </summary>
#endif
    public void Clear()
    {
        InboundEntries.Clear();
#if !COMPONENTS
        OutboundEntries.Clear();
#endif
    }
}
