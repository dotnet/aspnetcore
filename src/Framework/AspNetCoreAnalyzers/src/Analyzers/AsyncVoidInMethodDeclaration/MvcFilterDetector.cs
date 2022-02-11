// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

public partial class AsyncVoidInMethodDeclarationAnalyzer
{
    private static bool IsMvcFilter(ITypeSymbol? classSymbol, WellKnownTypes wellKnownTypes)
    {
        var possibleFilterInterfaces = ImmutableArray.Create(new[]
        {
            wellKnownTypes.IActionFilter,
            wellKnownTypes.IAuthorizationFilter,
            wellKnownTypes.IExceptionFilter,
            wellKnownTypes.IResourceFilter,
            wellKnownTypes.IResultFilter,
        });

        bool isMatch = false;
        for (int i = 0; i < possibleFilterInterfaces.Length; i++)
        {
            isMatch |= (classSymbol?.AllInterfaces.Contains(possibleFilterInterfaces[i]) ?? false);
            if (isMatch)
            {
                break;
            }
        }

        return isMatch;
    }
}
