// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure;

internal static class RouteParameterHelper
{
    public static string ResolveRouteParameterName(ISymbol parameterSymbol, WellKnownTypes wellKnownTypes)
    {
        var fromRouteMetadata = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromRouteMetadata);
        if (!parameterSymbol.HasAttributeImplementingInterface(fromRouteMetadata, out var attributeData))
        {
            return parameterSymbol.Name; // No route metadata attribute!
        }

        foreach (var namedArgument in attributeData.NamedArguments)
        {
            if (namedArgument.Key == "Name")
            {
                var routeParameterNameConstant = namedArgument.Value;
                var routeParameterName = (string)routeParameterNameConstant.Value!;
                return routeParameterName; // Have attribute & name is specified.
            }
        }

        return parameterSymbol.Name; // We have the attribute, but name isn't specified!
    }
}
