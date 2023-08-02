// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Text.Encodings.Web;
#if COMPONENTS
using Microsoft.AspNetCore.Components.Routing;
#else
using Microsoft.AspNetCore.Routing.Template;
#endif
using Microsoft.Extensions.Logging;
#if !COMPONENTS
using Microsoft.Extensions.ObjectPool;
#endif
namespace Microsoft.AspNetCore.Routing.Tree;

#if !COMPONENTS
/// <summary>
/// An <see cref="IRouter"/> implementation for attribute routing.
/// </summary>
public partial class TreeRouter : IRouter
#else
internal partial class TreeRouter
#endif
{
#if !COMPONENTS
    /// <summary>
    /// Key used by routing and action selection to match an attribute
    /// route entry to a group of action descriptors.
    /// </summary>
    public static readonly string RouteGroupKey = "!__route_group";

    private readonly LinkGenerationDecisionTree _linkGenerationTree;
#endif
    private readonly UrlMatchingTree[] _trees;
#if !COMPONENTS
    private readonly IDictionary<string, OutboundMatch> _namedEntries;
    private readonly ILogger _constraintLogger;
#endif
    private readonly ILogger _logger;

#if !COMPONENTS
    /// <summary>
    /// Creates a new instance of <see cref="TreeRouter"/>.
    /// </summary>
    /// <param name="trees">The list of <see cref="UrlMatchingTree"/> that contains the route entries.</param>
    /// <param name="linkGenerationEntries">The set of <see cref="OutboundRouteEntry"/>.</param>
    /// <param name="urlEncoder">The <see cref="UrlEncoder"/>.</param>
    /// <param name="objectPool">The <see cref="ObjectPool{T}"/>.</param>
    /// <param name="routeLogger">The <see cref="ILogger"/> instance.</param>
    /// <param name="constraintLogger">The <see cref="ILogger"/> instance used
    /// in <see cref="RouteConstraintMatcher"/>.</param>
    /// <param name="version">The version of this route.</param>
    internal TreeRouter(
        UrlMatchingTree[] trees,
        IEnumerable<OutboundRouteEntry> linkGenerationEntries,
        UrlEncoder urlEncoder,
        ObjectPool<UriBuildingContext> objectPool,
        ILogger routeLogger,
        ILogger constraintLogger,
        int version)
#else
    internal TreeRouter(
        UrlMatchingTree[] trees,
        UrlEncoder urlEncoder,
        ILogger routeLogger,
        int version)
#endif
    {
        ArgumentNullException.ThrowIfNull(trees);
#if !COMPONENTS
        ArgumentNullException.ThrowIfNull(linkGenerationEntries);
#endif
        ArgumentNullException.ThrowIfNull(urlEncoder);
#if !COMPONENTS
        ArgumentNullException.ThrowIfNull(objectPool);
#endif
        ArgumentNullException.ThrowIfNull(routeLogger);
#if !COMPONENTS
        ArgumentNullException.ThrowIfNull(constraintLogger);
#endif

        _trees = trees;
        _logger = routeLogger;
#if !COMPONENTS
        _constraintLogger = constraintLogger;
        _namedEntries = new Dictionary<string, OutboundMatch>(StringComparer.OrdinalIgnoreCase);

        var outboundMatches = new List<OutboundMatch>();

        foreach (var entry in linkGenerationEntries)
        {
            var binder = new TemplateBinder(urlEncoder, objectPool, entry.RouteTemplate, entry.Defaults);
            var outboundMatch = new OutboundMatch() { Entry = entry, TemplateBinder = binder };
            outboundMatches.Add(outboundMatch);

            // Skip unnamed entries
            if (entry.RouteName == null)
            {
                continue;
            }

            // We only need to keep one OutboundMatch per route template
            // so in case two entries have the same name and the same template we only keep
            // the first entry.
            if (_namedEntries.TryGetValue(entry.RouteName, out var namedMatch) &&
                !string.Equals(
                    namedMatch.Entry.RouteTemplate.TemplateText,
                    entry.RouteTemplate.TemplateText,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    Resources.FormatAttributeRoute_DifferentLinkGenerationEntries_SameName(entry.RouteName),
                    nameof(linkGenerationEntries));
            }
            else if (namedMatch == null)
            {
                _namedEntries.Add(entry.RouteName, outboundMatch);
            }
        }

        // The decision tree will take care of ordering for these entries.
        _linkGenerationTree = new LinkGenerationDecisionTree(outboundMatches.ToArray());
#endif
        Version = version;
    }

    /// <summary>
    /// Gets the version of this route.
    /// </summary>
    public int Version { get; }

    internal IEnumerable<UrlMatchingTree> MatchingTrees => _trees;

#if !COMPONENTS
    /// <inheritdoc />
    public VirtualPathData GetVirtualPath(VirtualPathContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // If it's a named route we will try to generate a link directly and
        // if we can't, we will not try to generate it using an unnamed route.
        if (context.RouteName != null)
        {
            return GetVirtualPathForNamedRoute(context);
        }

        // The decision tree will give us back all entries that match the provided route data in the correct
        // order. We just need to iterate them and use the first one that can generate a link.
        var matches = _linkGenerationTree.GetMatches(context.Values, context.AmbientValues);

        if (matches == null)
        {
            return null;
        }

        for (var i = 0; i < matches.Count; i++)
        {
            var path = GenerateVirtualPath(context, matches[i].Match.Entry, matches[i].Match.TemplateBinder);
            if (path != null)
            {
                return path;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task RouteAsync(RouteContext context)
#else
    public void Route(RouteContext context)
#endif
    {
        foreach (var tree in _trees)
        {
#if !COMPONENTS
            var tokenizer = new PathTokenizer(context.HttpContext.Request.Path);
            var root = tree.Root;

            var treeEnumerator = new TreeEnumerator(root, tokenizer);

            // Create a snapshot before processing the route. We'll restore this snapshot before running each
            // to restore the state. This is likely an "empty" snapshot, which doesn't allocate.
            var snapshot = context.RouteData.PushState(router: null, values: null, dataTokens: null);
            while (treeEnumerator.MoveNext())
            {
                var node = treeEnumerator.Current;
                foreach (var item in node.Matches)
                {
                    var entry = item.Entry;
                    var matcher = item.TemplateMatcher;
                    try
                    {
                        if (!matcher.TryMatch(context.HttpContext.Request.Path, context.RouteData.Values))
                        {
                            continue;
                        }
                        if (!RouteConstraintMatcher.Match(
                            entry.Constraints,
                            context.RouteData.Values,
                            context.HttpContext,
                            this,
                            RouteDirection.IncomingRequest,
                            _constraintLogger))
                        {
                            continue;
                        }

                        Log.RequestMatchedRoute(_logger, entry.RouteName, entry.RouteTemplate.TemplateText);
                        context.RouteData.Routers.Add(entry.Handler);
                        await entry.Handler.RouteAsync(context);
                        if (context.Handler != null)
                        {
                            return;
                        }
                    }
                    finally
                    {
                        if (context.Handler == null)
                        {
                            // Restore the original values to prevent polluting the route data.
                            snapshot.Restore();
                        }
                    }
                }
            }
#else
            var tokenizer = new PathTokenizer(new(context.Path));
            var root = tree.Root;

            var treeEnumerator = new TreeEnumerator(root, tokenizer);

            while (treeEnumerator.MoveNext())
            {
                var node = treeEnumerator.Current;
                foreach (var item in node.Matches)
                {
                    var entry = item.Entry;
                    var matcher = item.TemplateMatcher;

                    if (!matcher.TryMatch(new(context.Path), context.RouteValues))
                    {
                        continue;
                    }

                    if (!RouteConstraintMatcher.Match(
                            entry.Constraints,
                            context.RouteValues))
                    {
                        context.RouteValues.Clear();
                        continue;
                    }

                    Log.RequestMatchedRoute(_logger, entry.RouteName, entry.RoutePattern.RawText);
                    context.Entry = entry;
                    return;
                }
            }
#endif
        }
    }

#if !COMPONENTS
    private VirtualPathData GetVirtualPathForNamedRoute(VirtualPathContext context)
    {
        if (_namedEntries.TryGetValue(context.RouteName, out var match))
        {
            var path = GenerateVirtualPath(context, match.Entry, match.TemplateBinder);
            if (path != null)
            {
                return path;
            }
        }
        return null;
    }

    private VirtualPathData GenerateVirtualPath(
        VirtualPathContext context,
        OutboundRouteEntry entry,
        TemplateBinder binder)
    {
        // In attribute the context includes the values that are used to select this entry - typically
        // these will be the standard 'action', 'controller' and maybe 'area' tokens. However, we don't
        // want to pass these to the link generation code, or else they will end up as query parameters.
        //
        // So, we need to exclude from here any values that are 'required link values', but aren't
        // parameters in the template.
        //
        // Ex:
        //      template: api/Products/{action}
        //      required values: { id = "5", action = "Buy", Controller = "CoolProducts" }
        //
        //      result: { id = "5", action = "Buy" }
        var inputValues = new RouteValueDictionary();
        foreach (var kvp in context.Values)
        {
            if (entry.RequiredLinkValues.ContainsKey(kvp.Key))
            {
                var parameter = entry.RouteTemplate.GetParameter(kvp.Key);

                if (parameter == null)
                {
                    continue;
                }
            }

            inputValues.Add(kvp.Key, kvp.Value);
        }

        var bindingResult = binder.GetValues(context.AmbientValues, inputValues);
        if (bindingResult == null)
        {
            // A required parameter in the template didn't get a value.
            return null;
        }

        var matched = RouteConstraintMatcher.Match(
            entry.Constraints,
            bindingResult.CombinedValues,
            context.HttpContext,
            this,
            RouteDirection.UrlGeneration,
            _constraintLogger);

        if (!matched)
        {
            // A constraint rejected this link.
            return null;
        }

        var pathData = entry.Handler.GetVirtualPath(context);
        if (pathData != null)
        {
            // If path is non-null then the target router short-circuited, we don't expect this
            // in typical MVC scenarios.
            return pathData;
        }

        var path = binder.BindValues(bindingResult.AcceptedValues);
        if (path == null)
        {
            return null;
        }

        return new VirtualPathData(this, path);
    }
#endif

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug,
            "Request successfully matched the route with name '{RouteName}' and template '{RouteTemplate}'",
            EventName = "RequestMatchedRoute")]
        public static partial void RequestMatchedRoute(ILogger logger, string routeName, string routeTemplate);
    }
}
