// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RenderTreeBuilder;

internal sealed class WellKnownTypes
{
    public static bool TryCreate(Compilation compilation, [NotNullWhen(returnValue: true)] out WellKnownTypes? wellKnownTypes)
    {
        wellKnownTypes = default;

        const string RenderTreeBuilder = "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder";
        if (compilation.GetTypeByMetadataName(RenderTreeBuilder) is not { } renderTreeBuilder)
        {
            return false;
        }

        wellKnownTypes = new()
        {
            RenderTreeBuilder = renderTreeBuilder
        };

        return true;
    }

    public INamedTypeSymbol RenderTreeBuilder { get; private init; }
}
