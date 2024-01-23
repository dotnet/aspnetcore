// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.Mvc;

using WellKnownType = WellKnownTypeData.WellKnownType;

public partial class MvcAnalyzer
{
    private static void DetectAmbiguousActionRoutes(SymbolAnalysisContext context, WellKnownTypes wellKnownTypes, RoutePatternTree? controllerRoutePattern, List<ActionRoute> actionRoutes)
    {
        var controllerHasActionToken = controllerRoutePattern != null ? HasActionToken(controllerRoutePattern) : false;

        // Ambiguous action route detection is conservative in what it detects to avoid false positives.
        //
        // Successfully matched action routes must:
        // 1. Be in the same controller.
        // 2. Have an equivalent route.
        // 3. Have a matching HTTP method.
        // 4. Route either be the on the same action or the actions only have known safe attributes that don't impact matching.
        if (actionRoutes.Count > 0)
        {
            // Group action routes together. When multiple match in a group, then report action routes to diagnostics.
            var groupedByParent = actionRoutes
                .GroupBy(ar => new ActionRouteGroupKey(ar.ActionSymbol, ar.RouteUsageModel.RoutePattern, ar.HttpMethods, controllerHasActionToken, wellKnownTypes));

            foreach (var ambiguousGroup in groupedByParent.Where(g => g.Count() >= 2))
            {
                foreach (var ambiguousActionRoute in ambiguousGroup)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.AmbiguousActionRoute,
                        ambiguousActionRoute.RouteUsageModel.UsageContext.RouteToken.GetLocation(),
                        ambiguousActionRoute.RouteUsageModel.RoutePattern.Root.ToString()));
                }
            }
        }
    }

    /// <summary>
    /// Search route pattern for:
    /// 1. Action replacement tokens, [action]
    /// 2. Action parameter tokens, {action}
    /// </summary>
    private static bool HasActionToken(RoutePatternTree routePattern)
    {
        for (var i = 0; i < routePattern.Root.Parts.Length; i++)
        {
            if (routePattern.Root.Parts[i] is RoutePatternSegmentNode segment)
            {
                for (var j = 0; j < segment.Children.Length; j++)
                {
                    if (segment.Children[j] is RoutePatternReplacementNode replacementNode)
                    {
                        if (!replacementNode.TextToken.IsMissing)
                        {
                            var name = replacementNode.TextToken.Value!.ToString();
                            if (string.Equals(name, "action", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                    else if (segment.Children[j] is RoutePatternParameterNode parameterNode)
                    {
                        for (var k = 0; k < parameterNode.ParameterParts.Length; k++)
                        {
                            if (parameterNode.ParameterParts[k] is RoutePatternNameParameterPartNode namePartNode)
                            {
                                if (!namePartNode.ParameterNameToken.IsMissing)
                                {
                                    var name = namePartNode.ParameterNameToken.Value!.ToString();
                                    if (string.Equals(name, "action", StringComparison.OrdinalIgnoreCase))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    private readonly struct ActionRouteGroupKey : IEquatable<ActionRouteGroupKey>
    {
        public IMethodSymbol ActionSymbol { get; }
        public RoutePatternTree RoutePattern { get; }
        public ImmutableArray<string> HttpMethods { get; }
        public string ActionName { get; }
        public bool HasActionToken { get; }
        private readonly WellKnownTypes _wellKnownTypes;

        public ActionRouteGroupKey(IMethodSymbol actionSymbol, RoutePatternTree routePattern, ImmutableArray<string> httpMethods, bool controllerHasActionToken, WellKnownTypes wellKnownTypes)
        {
            Debug.Assert(!httpMethods.IsDefault);

            ActionSymbol = actionSymbol;
            RoutePattern = routePattern;
            HttpMethods = httpMethods;
            _wellKnownTypes = wellKnownTypes;
            ActionName = GetActionName(ActionSymbol, _wellKnownTypes);
            HasActionToken = controllerHasActionToken || HasActionToken(RoutePattern);
        }

        private static string GetActionName(IMethodSymbol actionSymbol, WellKnownTypes wellKnownTypes)
        {
            var actionNameAttribute = actionSymbol.GetAttributes(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_ActionNameAttribute), inherit: true).FirstOrDefault();
            if (actionNameAttribute != null && actionNameAttribute.ConstructorArguments.Length > 0 && actionNameAttribute.ConstructorArguments[0].Value is string name)
            {
                return name;
            }
            return actionSymbol.Name;
        }

        public override bool Equals(object obj)
        {
            if (obj is ActionRouteGroupKey key)
            {
                return Equals(key);
            }
            return false;
        }

        public bool Equals(ActionRouteGroupKey other)
        {
            return
                AmbiguousRoutePatternComparer.Instance.Equals(RoutePattern, other.RoutePattern) &&
                (!HasActionToken || string.Equals(ActionName, other.ActionName, StringComparison.OrdinalIgnoreCase)) &&
                HasMatchingHttpMethods(HttpMethods, other.HttpMethods) &&
                CanMatchActions(_wellKnownTypes, ActionSymbol, other.ActionSymbol);
        }

        private static bool CanMatchActions(WellKnownTypes wellKnownTypes, IMethodSymbol actionSymbol1, IMethodSymbol actionSymbol2)
        {
            // Only match routes if either they are on the same action.
            if (SymbolEqualityComparer.Default.Equals(actionSymbol1, actionSymbol2))
            {
                return true;
            }

            // Or all attributes on the actions are known to have no impact on routing.
            // This ensures we don't detect routes that might have metadata added that impacts routing.
            if (!HasUnknownAttribute(actionSymbol1, wellKnownTypes) && !HasUnknownAttribute(actionSymbol2, wellKnownTypes))
            {
                return true;
            }

            return false;
        }

        // A collection of attributes in ASP.NET Core that don't have any impact on route matching and are safe.
        // Note that route attributes such as [HttpGet] and friends are safe because we compare the route and HTTP method explicitly.
        private static readonly WellKnownType[] KnownMethodAttributeTypes = new[]
        {
            WellKnownType.Microsoft_AspNetCore_Mvc_RouteAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpDeleteAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpGetAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpHeadAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpOptionsAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpPatchAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpPostAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpPutAttribute,
            WellKnownType.Microsoft_AspNetCore_Http_EndpointDescriptionAttribute,
            WellKnownType.Microsoft_AspNetCore_Http_EndpointSummaryAttribute,
            WellKnownType.Microsoft_AspNetCore_Http_TagsAttribute,
            WellKnownType.Microsoft_AspNetCore_Routing_EndpointGroupNameAttribute,
            WellKnownType.Microsoft_AspNetCore_Routing_EndpointNameAttribute,
            WellKnownType.Microsoft_AspNetCore_Routing_ExcludeFromDescriptionAttribute,
            WellKnownType.Microsoft_AspNetCore_Cors_DisableCorsAttribute,
            WellKnownType.Microsoft_AspNetCore_Cors_EnableCorsAttribute,
            WellKnownType.Microsoft_AspNetCore_OutputCaching_OutputCacheAttribute,
            WellKnownType.Microsoft_AspNetCore_RateLimiting_DisableRateLimitingAttribute,
            WellKnownType.Microsoft_AspNetCore_RateLimiting_EnableRateLimitingAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_ActionNameAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_DisableRequestSizeLimitAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_FormatFilterAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_ProducesAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_ProducesDefaultResponseTypeAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_ProducesErrorResponseTypeAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_ProducesResponseTypeAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_RequestFormLimitsAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_RequestSizeLimitAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_RequireHttpsAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_ResponseCacheAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_ServiceFilterAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_TypeFilterAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_ApiExplorer_ApiConventionNameMatchAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_Filters_ResultFilterAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_Infrastructure_DefaultStatusCodeAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_AutoValidateAntiforgeryTokenAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_IgnoreAntiforgeryTokenAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_ViewFeatures_SaveTempDataAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_SkipStatusCodePagesAttribute,
            WellKnownType.Microsoft_AspNetCore_Mvc_ValidateAntiForgeryTokenAttribute,
            WellKnownType.Microsoft_AspNetCore_Authorization_AllowAnonymousAttribute,
            WellKnownType.Microsoft_AspNetCore_Authorization_AuthorizeAttribute
        };

        private static bool HasUnknownAttribute(IMethodSymbol actionSymbol, WellKnownTypes wellKnownTypes)
        {
            foreach (var attribute in actionSymbol.GetAttributes())
            {
                if (attribute.AttributeClass is null)
                {
                    return true;
                }

                if (!wellKnownTypes.IsType(attribute.AttributeClass, KnownMethodAttributeTypes))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasMatchingHttpMethods(ImmutableArray<string> httpMethods1, ImmutableArray<string> httpMethods2)
        {
            if (httpMethods1.IsEmpty || httpMethods2.IsEmpty)
            {
                return true;
            }

            foreach (var item1 in httpMethods1)
            {
                foreach (var item2 in httpMethods2)
                {
                    if (item2 == item1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override int GetHashCode()
        {
            var hashCode = 0;
            foreach (var method in HttpMethods)
            {
                hashCode ^= StringComparer.OrdinalIgnoreCase.GetHashCode(method);
            }
            return hashCode ^ AmbiguousRoutePatternComparer.Instance.GetHashCode(RoutePattern);
        }
    }
}
