// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Resolves components for an application.
/// </summary>
internal class RouteTableFactory
{
    public static readonly RouteTableFactory Instance = new();
    public static readonly IComparer<InboundRouteEntry> RouteOrder = Comparer<InboundRouteEntry>.Create((x, y) =>
    {
        var result = RouteComparison(x, y);
        return result != 0 ? result : string.Compare(x.RoutePattern.RawText, y.RoutePattern.RawText, StringComparison.OrdinalIgnoreCase);
    });

    private readonly ConcurrentDictionary<RouteKey, RouteTable> _cache = new();

    public RouteTable Create(RouteKey routeKey, IServiceProvider serviceProvider)
    {
        if (_cache.TryGetValue(routeKey, out var resolvedComponents))
        {
            return resolvedComponents;
        }

        var componentTypes = GetRouteableComponents(routeKey);
        var routeTable = Create(componentTypes, serviceProvider);
        _cache.TryAdd(routeKey, routeTable);
        return routeTable;
    }

    public void ClearCaches() => _cache.Clear();

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Application code does not get trimmed, and the framework does not define routable components.")]
    private static List<Type> GetRouteableComponents(RouteKey routeKey)
    {
        var routeableComponents = new List<Type>();
        if (routeKey.AppAssembly is not null)
        {
            GetRouteableComponents(routeableComponents, routeKey.AppAssembly);
        }

        if (routeKey.AdditionalAssemblies is not null)
        {
            foreach (var assembly in routeKey.AdditionalAssemblies)
            {
                // We don't need process the assembly if it's the app assembly.
                if (assembly != routeKey.AppAssembly)
                {
                    GetRouteableComponents(routeableComponents, assembly);
                }
            }
        }

        return routeableComponents;

        static void GetRouteableComponents(List<Type> routeableComponents, Assembly assembly)
        {
            foreach (var type in assembly.ExportedTypes)
            {
                if (typeof(IComponent).IsAssignableFrom(type)
                    && type.IsDefined(typeof(RouteAttribute))
                    && !type.IsDefined(typeof(ExcludeFromInteractiveRoutingAttribute)))
                {
                    routeableComponents.Add(type);
                }
            }
        }
    }

    internal static RouteTable Create(List<Type> componentTypes, IServiceProvider serviceProvider)
    {
        var templatesByHandler = new Dictionary<Type, string[]>();
        foreach (var componentType in componentTypes)
        {
            // We're deliberately using inherit = false here.
            //
            // RouteAttribute is defined as non-inherited, because inheriting a route attribute always causes an
            // ambiguity. You end up with two components (base class and derived class) with the same route.
            var templates = GetTemplates(componentType);

            templatesByHandler.Add(componentType, templates);
        }
        return Create(templatesByHandler, serviceProvider);
    }

    private static string[] GetTemplates(Type componentType)
    {
        var routeAttributes = componentType.GetCustomAttributes(typeof(RouteAttribute), inherit: false);
        var templates = new string[routeAttributes.Length];
        for (var i = 0; i < routeAttributes.Length; i++)
        {
            var attribute = (RouteAttribute)routeAttributes[i];
            templates[i] = attribute.Template;
        }

        return templates;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Application code does not get trimmed, and the framework does not define routable components.")]
    internal static RouteTable Create(Dictionary<Type, string[]> templatesByHandler, IServiceProvider serviceProvider)
    {
        var routeOptions = Options.Create(new RouteOptions());
        if (!OperatingSystem.IsBrowser() || RegexConstraintSupport.IsEnabled)
        {
            routeOptions.Value.SetParameterPolicy("regex", typeof(RegexInlineRouteConstraint));
        }
        var builder = new TreeRouteBuilder(
            serviceProvider.GetRequiredService<ILoggerFactory>(),
            new DefaultInlineConstraintResolver(routeOptions, serviceProvider));

        foreach (var (type, templates) in templatesByHandler)
        {
            var result = ComputeTemplateGroupInfo(templates);

            var parsedTemplates = result.ParsedTemplates;
            var allRouteParameterNames = result.AllRouteParameterNames;

            foreach (var (parsedTemplate, routeParameterNames) in parsedTemplates)
            {
                var unusedRouteParameterNames = GetUnusedParameterNames(allRouteParameterNames!, routeParameterNames!);
                builder.MapInbound(type, parsedTemplate, unusedRouteParameterNames);
            }
        }

        DetectAmbiguousRoutes(builder);

        return new RouteTable(builder.Build());
    }

    private static TemplateGroupInfo ComputeTemplateGroupInfo(string[] templates)
    {
        var result = new TemplateGroupInfo(templates);
        for (var i = 0; i < templates.Length; i++)
        {
            var parsedTemplate = RoutePatternParser.Parse(templates[i]);
            var parameterNames = GetParameterNames(parsedTemplate);
            result.ParsedTemplates[i] = (parsedTemplate, parameterNames);

            foreach (var parameterName in parameterNames)
            {
                result.AllRouteParameterNames.Add(parameterName);
            }
        }

        return result;
    }

    private struct TemplateGroupInfo(string[] templates)
    {
        public HashSet<string> AllRouteParameterNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public (RoutePattern, HashSet<string>)[] ParsedTemplates { get; set; } = new (RoutePattern, HashSet<string>)[templates.Length];
    }

    internal static InboundRouteEntry CreateEntry([DynamicallyAccessedMembers(Component)] Type pageType, string template)
    {
        var templates = GetTemplates(pageType);
        var result = ComputeTemplateGroupInfo(templates);

        RoutePattern? parsedTemplate = null;
        HashSet<string>? routeParameterNames = null;
        for (var i = 0; i < result.ParsedTemplates.Length; i++)
        {
            var (parsed, parameters) = result.ParsedTemplates[i];
            if (string.Equals(parsed.RawText, template, StringComparison.OrdinalIgnoreCase))
            {
                parsedTemplate = parsed;
                routeParameterNames = parameters;
                break;
            }
        }

        if (parsedTemplate == null)
        {
            throw new InvalidOperationException($"Unable to find the provided template '{template}'");
        }

        return new InboundRouteEntry()
        {
            Handler = pageType,
            RoutePattern = parsedTemplate,
            UnusedRouteParameterNames = GetUnusedParameterNames(result.AllRouteParameterNames, routeParameterNames!),
        };
    }
    private static void DetectAmbiguousRoutes(TreeRouteBuilder builder)
    {
        var seen = new HashSet<InboundRouteEntry>(new InboundRouteEntryAmbiguityEqualityComparer());
        seen.EnsureCapacity(builder.InboundEntries.Count);

        for (var i = 0; i < builder.InboundEntries.Count; i++)
        {
            var current = builder.InboundEntries[i];

            if (!seen.Add(current))
            {
                seen.TryGetValue(current, out var existing);
                var existingText = existing!.RoutePattern.RawText!.Trim('/');
                var currentText = current.RoutePattern.RawText!.Trim('/');
                throw new InvalidOperationException($"""
                    The following routes are ambiguous:
                    '{existingText}' in '{existing.Handler.FullName}'
                    '{currentText}' in '{current.Handler.FullName}'

                    """);
            }
        }
    }

    private static HashSet<string> GetParameterNames(RoutePattern routeTemplate)
    {
        var parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in routeTemplate.Parameters)
        {
            parameterNames.Add(parameter.Name!);
        }

        return parameterNames;
    }

    private static List<string>? GetUnusedParameterNames(HashSet<string> allRouteParameterNames, HashSet<string> routeParameterNames)
    {
        List<string>? unusedParameters = null;
        foreach (var item in allRouteParameterNames)
        {
            if (!routeParameterNames.Contains(item))
            {
                unusedParameters ??= new();
                unusedParameters.Add(item);
            }
        }

        return unusedParameters;
    }

    /// <summary>
    /// Route precedence algorithm.
    /// We collect all the routes and sort them from most specific to
    /// less specific. The specificity of a route is given by the specificity
    /// of its segments and the position of those segments in the route.
    /// * A literal segment is more specific than a parameter segment.
    /// * A parameter segment with more constraints is more specific than one with fewer constraints
    /// * Segment earlier in the route are evaluated before segments later in the route.
    /// For example:
    /// /Literal is more specific than /Parameter
    /// /Route/With/{parameter} is more specific than /{multiple}/With/{parameters}
    /// /Product/{id:int} is more specific than /Product/{id}
    ///
    /// Routes can be ambiguous if:
    /// They are composed of literals and those literals have the same values (case insensitive)
    /// They are composed of a mix of literals and parameters, in the same relative order and the
    /// literals have the same values.
    /// For example:
    /// * /literal and /Literal
    /// /{parameter}/literal and /{something}/literal
    /// /{parameter:constraint}/literal and /{something:constraint}/literal
    ///
    /// To calculate the precedence we sort the list of routes as follows:
    /// * Shorter routes go first.
    /// * A literal wins over a parameter in precedence.
    /// * For literals with different values (case insensitive) we choose the lexical order
    /// * For parameters with different numbers of constraints, the one with more wins
    /// If we get to the end of the comparison routing we've detected an ambiguous pair of routes.
    /// </summary>
    internal static int RouteComparison(InboundRouteEntry x, InboundRouteEntry y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        var xTemplate = x.RoutePattern;
        var yTemplate = y.RoutePattern;
        var xPrecedence = RoutePrecedence.ComputeInbound(xTemplate);
        var yPrecedence = RoutePrecedence.ComputeInbound(yTemplate);

        return (yPrecedence.CompareTo(xPrecedence)) switch
        {
            -1 => 1,
            1 => -1,
            0 => 0,
            _ => throw new InvalidOperationException("Invalid comparison result."),
        };
    }

    private sealed class InboundRouteEntryAmbiguityEqualityComparer : IEqualityComparer<InboundRouteEntry>
    {
        public bool Equals(InboundRouteEntry? x, InboundRouteEntry? y)
        {
            if (x is null)
            {
                return y is null;
            }

            if (y is null)
            {
                return false;
            }

            if (x.Precedence != y.Precedence)
            {
                return false;
            }

            for (var k = 0; k < x.RoutePattern.PathSegments.Count; k++)
            {
                var leftSegment = x.RoutePattern.PathSegments[k];
                var rightSegment = y.RoutePattern.PathSegments[k];
                if (leftSegment.Parts.Count != rightSegment.Parts.Count)
                {
                    return false;
                }

                for (var l = 0; l < leftSegment.Parts.Count; l++)
                {
                    var leftPart = leftSegment.Parts[l];
                    var rightPart = rightSegment.Parts[l];
                    if (leftPart is RoutePatternLiteralPart leftLiteral &&
                        rightPart is RoutePatternLiteralPart rightLiteral &&
                        !string.Equals(leftLiteral.Content, rightLiteral.Content, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetHashCode(InboundRouteEntry obj)
        {
            var hashCode = new HashCode();
            hashCode.Add(obj.Precedence);

            for (var i = 0; i < obj.RoutePattern.PathSegments.Count; i++)
            {
                var segment = obj.RoutePattern.PathSegments[i];
                for (var j = 0; j < segment.Parts.Count; j++)
                {
                    var part = segment.Parts[j];
                    if (part is RoutePatternLiteralPart literal)
                    {
                        hashCode.Add(literal.Content, StringComparer.OrdinalIgnoreCase);
                    }
                }
            }

            return hashCode.ToHashCode();
        }
    }
}
