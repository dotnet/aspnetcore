// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

internal static class OptionsFacts
{
    public static bool IsEndpointRoutingExplicitlyDisabled(OptionsAnalysis analysis)
    {
        for (var i = 0; i < analysis.Options.Length; i++)
        {
            var item = analysis.Options[i];
            if (string.Equals(item.OptionsType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), SymbolNames.MvcOptions.MetadataName) &&
                string.Equals(item.Property.Name, SymbolNames.MvcOptions.EnableEndpointRoutingPropertyName, StringComparison.Ordinal))
            {
                return item.ConstantValue as bool? == false;
            }
        }

        return false;
    }
}
