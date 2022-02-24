// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

internal static class StartupFacts
{
    public static bool IsStartupClass(StartupSymbols symbols, INamedTypeSymbol type)
    {
        if (symbols == null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        // It's not good enough to just look for a method called ConfigureServices or Configure as a hueristic.
        // ConfigureServices might not appear in trivial cases, and Configure might be named ConfigureDevelopment
        // or something similar.
        //
        // Additionally, a startup class could be called anything and wired up explicitly.
        //
        // Since we already are analyzing the symbol it should be cheap to do a pass over the members.
        var members = type.GetMembers();
        for (var i = 0; i < members.Length; i++)
        {
            if (members[i] is IMethodSymbol method && (IsConfigureServices(symbols, method) || IsConfigure(symbols, method)))
            {
                return true;
            }
        }

        return false;
    }

    // Based on StartupLoader. The philosophy is that we want to do analysis only on things
    // that would be recognized as a ConfigureServices method to avoid false positives.
    //
    // The ConfigureServices method follows the naming pattern `Configure{Environment?}Services` (ignoring case).
    // The ConfigureServices method must be public.
    // The ConfigureServices method can be instance or static.
    // The ConfigureServices method cannot have other parameters besides IServiceCollection.
    //
    // The ConfigureServices method does not actually need to accept IServiceCollection
    // but we exclude that case because a ConfigureServices method that doesn't accept an
    // IServiceCollection can't do anything interesting to analysis.
    public static bool IsConfigureServices(StartupSymbols symbols, IMethodSymbol symbol)
    {
        if (symbol == null)
        {
            throw new ArgumentNullException(nameof(symbol));
        }

        if (symbol.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        if (symbol.Name == null ||
            !symbol.Name.StartsWith(SymbolNames.ConfigureServicesMethodPrefix, StringComparison.OrdinalIgnoreCase) ||
            !symbol.Name.EndsWith(SymbolNames.ConfigureServicesMethodSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (symbol.Parameters.Length != 1)
        {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(symbol.Parameters[0].Type, symbols.IServiceCollection);
    }

    // Based on StartupLoader. The philosophy is that we want to do analysis only on things
    // that would be recognized as a Configure method to avoid false positives.
    //
    // The Configure method follows the naming pattern `Configure{Environment?}` (ignoring case).
    // The Configure method must be public.
    // The Configure method can be instance or static.
    // The Configure method *can* have other parameters besides IApplicationBuilder.
    //
    // The Configure method does not actually need to accept IApplicationBuilder
    // but we exclude that case because a Configure method that doesn't accept an
    // IApplicationBuilder can't do anything interesting to analysis.
    public static bool IsConfigure(StartupSymbols symbols, IMethodSymbol symbol)
    {
        if (symbol == null)
        {
            throw new ArgumentNullException(nameof(symbol));
        }

        if (symbol.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        if (symbol.Name == null ||
            !symbol.Name.StartsWith(SymbolNames.ConfigureMethodPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // IApplicationBuilder can appear in any parameter, but must appear.
        for (var i = 0; i < symbol.Parameters.Length; i++)
        {
            if (SymbolEqualityComparer.Default.Equals(symbol.Parameters[i].Type, symbols.IApplicationBuilder))
            {
                return true;
            }
        }

        return false;
    }

    // Based on the three known gestures for including SignalR in a ConfigureMethod:
    // UseSignalR() // middleware
    // MapHub<>() // endpoint routing
    // MapBlazorHub() // server-side blazor
    //
    // To be slightly less brittle, we don't look at the exact symbols and instead just look
    // at method names in here. We're NOT worried about false negatives, because all of these
    // cases contain words like SignalR or Hub.
    public static bool IsSignalRConfigureMethodGesture(IMethodSymbol symbol)
    {
        if (symbol == null)
        {
            throw new ArgumentNullException(nameof(symbol));
        }

        // UseSignalR has been removed in 5.0, but we should probably still check for it in this analyzer in case the user
        // installs it into a pre-5.0 app.
        if (string.Equals(symbol.Name, SymbolNames.SignalRAppBuilderExtensions.UseSignalRMethodName, StringComparison.Ordinal) ||
            string.Equals(symbol.Name, SymbolNames.HubEndpointRouteBuilderExtensions.MapHubMethodName, StringComparison.Ordinal) ||
            string.Equals(symbol.Name, SymbolNames.ComponentEndpointRouteBuilderExtensions.MapBlazorHubMethodName, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
