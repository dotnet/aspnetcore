// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.Mvc;

using WellKnownType = WellKnownTypeData.WellKnownType;

public partial class MvcAnalyzer
{
    /// <summary>
    /// This tries to detect [Authorize] attributes that are unwittingly overridden by [AllowAnonymous] attributes that are "farther" away from a controller.
    /// </summary>
    /// <remarks>
    /// This might report the same [Authorize] attribute multiple times if it's on a shared base type, but we'd have to disable parallelization of the
    /// entire MvcAnalyzer to avoid that. We assume that this scenario is rare enough and that overreporting is benign enough to not warrant the performance hit.
    /// See AuthorizeOnControllerBaseWithMultipleChildren_AllowAnonymousOnControllerBaseBaseType_HasMultipleDiagnostics.
    /// </remarks>
    private static void DetectOverriddenAuthorizeAttributeOnController(SymbolAnalysisContext context, WellKnownTypes wellKnownTypes,
        INamedTypeSymbol controllerSymbol, List<AttributeInfo> authorizeAttributes, out string? allowAnonClass)
    {
        Debug.Assert(authorizeAttributes.Count is 0);

        var isCheckingBaseType = false;
        allowAnonClass = null;

        foreach (var currentClass in controllerSymbol.GetTypeHierarchy())
        {
            FindAuthorizeAndAllowAnonymous(wellKnownTypes, currentClass, isCheckingBaseType, authorizeAttributes, out var foundAllowAnonymous);
            if (foundAllowAnonymous)
            {
                // Anything we find after this would be farther away, so we can short circuit.
                ReportOverriddenAuthorizeAttributeDiagnosticsIfAny(context, authorizeAttributes, currentClass.Name);
                // Keep track of the nearest class with [AllowAnonymous] for later reporting of action-level [Authorize] attributes.
                allowAnonClass = currentClass.Name;
                return;
            }

            isCheckingBaseType = true;
        }
    }

    /// <summary>
    /// This tries to detect [Authorize] attributes that are unwittingly overridden by [AllowAnonymous] attributes that are "farther" away from a controller action.
    /// To do so, it first searches the action method and then the controller class. It repeats this process for each virtual method the action may override and for
    /// each base class the controller may inherit from. Since it searches for the attributes closest to the action first, it short circuits as soon as [AllowAnonymous] is found.
    /// If it has already detected a closer [Authorize] attribute, it reports a diagnostic at the [Authorize] attribute's location indicating that it will be overridden.
    /// </summary>
    private static void DetectOverriddenAuthorizeAttributeOnAction(SymbolAnalysisContext context, WellKnownTypes wellKnownTypes,
        IMethodSymbol actionSymbol, List<AttributeInfo> authorizeAttributes, string? allowAnonClass)
    {
        Debug.Assert(authorizeAttributes.Count is 0);

        var isCheckingBaseType = false;
        var currentMethod = actionSymbol;

        foreach (var currentClass in actionSymbol.ContainingType.GetTypeHierarchy())
        {
            bool foundAllowAnonymous;

            if (currentMethod is not null && IsSameSymbol(currentMethod.ContainingType, currentClass))
            {
                FindAuthorizeAndAllowAnonymous(wellKnownTypes, currentMethod, isCheckingBaseType, authorizeAttributes, out foundAllowAnonymous);
                if (foundAllowAnonymous)
                {
                    // [AllowAnonymous] was found on the action method. Anything we find after this would be farther away, so we short circuit.
                    ReportOverriddenAuthorizeAttributeDiagnosticsIfAny(context, authorizeAttributes, currentMethod.ContainingType.Name, currentMethod.Name);
                    return;
                }

                currentMethod = currentMethod.OverriddenMethod;

                // We've already checked the controller and any base classes for overridden attributes in DetectOverriddenAuthorizeAttributeOnController.
                // If there are no more base methods, and we are not tracking any unreported [Authorize] attributes that might be overridden by a class, we're done.
                if (currentMethod is null && (authorizeAttributes.Count is 0 || !isCheckingBaseType))
                {
                    if (allowAnonClass is not null)
                    {
                        // We don't use allowAnonClass once we start checking overrides to avoid false positives. But if we found [Authorize] directly on a non-virtual
                        // action, we can report it without rechecking the controller or its base types for [AllowAnonymous] when given a non-null allowAnonClass.
                        ReportOverriddenAuthorizeAttributeDiagnosticsIfAny(context, authorizeAttributes, allowAnonClass);
                    }

                    return;
                }
            }

            // Now, we're mostly trying to detect [Authorize] on virtual actions which are not covered by allowAnonClass. Overridden [Authorize] attributes on classes
            // have mostly been reported already, but we still need to track those too just in case there is [AllowAnonymous] on a base method farther away.
            FindAuthorizeAndAllowAnonymous(wellKnownTypes, currentClass, isCheckingBaseType, authorizeAttributes, out foundAllowAnonymous);
            if (foundAllowAnonymous)
            {
                // We are only concerned with method-level [Authorize] attributes that are overridden by the [AllowAnonymous] found on this class.
                // Any child classes should have already been reported in DetectOverriddenAuthorizeAttributeOnController.
                ReportOverriddenAuthorizeAttributeDiagnosticsIfAny(context, authorizeAttributes.Where(a => a.IsTargetingMethod), currentClass.Name);
                return;
            }

            isCheckingBaseType = true;
        }

        Debug.Assert(currentMethod is null);
    }

    private static bool IsSameSymbol(ISymbol? x, ISymbol? y) => SymbolEqualityComparer.Default.Equals(x, y);

    private static bool IsInheritableAttribute(WellKnownTypes wellKnownTypes, INamedTypeSymbol attribute)
    {
        // [AttributeUsage] is sealed but inheritable.
        var attributeUsageAttributeType = wellKnownTypes.Get(WellKnownType.System_AttributeUsageAttribute);
        var attributeUsage = attribute.GetAttributes(attributeUsageAttributeType, inherit: true).FirstOrDefault();

        if (attributeUsage is not null)
        {
            foreach (var arg in attributeUsage.NamedArguments)
            {
                if (arg.Key == nameof(AttributeUsageAttribute.Inherited))
                {
                    return (bool)arg.Value.Value!;
                }
            }
        }

        // If [AttributeUsage] is not found or the Inherited property is not set, the default is true.
        return true;
    }

    private static bool IsMatchingAttribute(WellKnownTypes wellKnownTypes, INamedTypeSymbol attribute,
        INamedTypeSymbol commonAttribute, ITypeSymbol attributeInterface, bool mustBeInheritable)
    {
        // The "common" attribute is either [Authorize] or [AllowAnonymous] so we can skip the interface and inheritable checks.
        if (IsSameSymbol(attribute, commonAttribute))
        {
            return true;
        }

        if (!attributeInterface.IsAssignableFrom(attribute))
        {
            return false;
        }

        return !mustBeInheritable || IsInheritableAttribute(wellKnownTypes, attribute);
    }

    private static void FindAuthorizeAndAllowAnonymous(WellKnownTypes wellKnownTypes, ISymbol symbol, bool isCheckingBaseType,
        List<AttributeInfo> authorizeAttributes, out bool foundAllowAnonymous)
    {
        AttributeData? localAuthorizeAttribute = null;
        List<AttributeData>? localAuthorizeAttributeOverflow = null;
        foundAllowAnonymous = false;

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass is null)
            {
                continue;
            }

            var authInterfaceType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Authorization_IAuthorizeData);
            var authAttributeType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Authorization_AuthorizeAttribute);
            if (IsMatchingAttribute(wellKnownTypes, attribute.AttributeClass, authAttributeType, authInterfaceType, isCheckingBaseType))
            {
                if (localAuthorizeAttribute is null)
                {
                    localAuthorizeAttribute = attribute;
                }
                else
                {
                    // This is ony allocated if there are multiple [Authorize] attributes on the same symbol which we assume is rare.
                    localAuthorizeAttributeOverflow ??= [];
                    localAuthorizeAttributeOverflow.Add(attribute);
                }
            }

            var anonInterfaceType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Authorization_IAllowAnonymous);
            var anonAttributeType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Authorization_AllowAnonymousAttribute);
            if (IsMatchingAttribute(wellKnownTypes, attribute.AttributeClass, anonAttributeType, anonInterfaceType, isCheckingBaseType))
            {
                // If localAuthorizeAttribute is not null, [AllowAnonymous] came after [Authorize] on the same method or class. We assume
                // this closer [AllowAnonymous] was intended to override the [Authorize] attribute which it always does regardless of order.
                // [Authorize(...)] could still be useful for configuring the authentication scheme even if the endpoint allows anonymous requests.
                localAuthorizeAttribute = null;
                localAuthorizeAttributeOverflow?.Clear();
                foundAllowAnonymous = true;
            }
        }

        if (localAuthorizeAttribute is not null)
        {
            var isTargetingMethod = symbol is IMethodSymbol;
            authorizeAttributes.Add(new(localAuthorizeAttribute, isTargetingMethod));
            foreach (var extraAttribute in localAuthorizeAttributeOverflow ?? Enumerable.Empty<AttributeData>())
            {
                authorizeAttributes.Add(new(extraAttribute, isTargetingMethod));
            }
        }
    }

    private static void ReportOverriddenAuthorizeAttributeDiagnosticsIfAny(SymbolAnalysisContext context,
        IEnumerable<AttributeInfo> authorizeAttributes, string allowAnonClass, string? allowAnonMethod = null)
    {
        string? allowAnonLocation = null;

        foreach (var authorizeAttribute in authorizeAttributes)
        {
            if (authorizeAttribute.AttributeData.ApplicationSyntaxReference is { } syntaxReference)
            {
                allowAnonLocation ??= allowAnonMethod is null ? allowAnonClass : $"{allowAnonClass}.{allowAnonMethod}";
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.OverriddenAuthorizeAttribute,
                    syntaxReference.GetSyntax(context.CancellationToken).GetLocation(),
                    allowAnonLocation));
            }
        }
    }
}
