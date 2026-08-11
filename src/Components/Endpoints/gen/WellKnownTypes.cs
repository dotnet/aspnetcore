// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators;

internal sealed class WellKnownTypes
{
    public const string ComponentsAssemblyName = "Microsoft.AspNetCore.Components";
    public const string MetadataContextMetadataName = "Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext";
    public const string BindableModelAttributeMetadataName = "Microsoft.AspNetCore.Components.Web.BindableModelAttribute";

    private WellKnownTypes(Compilation compilation)
    {
        MetadataContext = compilation.GetTypeByMetadataName(MetadataContextMetadataName);
        BindableModelAttribute = compilation.GetTypeByMetadataName(BindableModelAttributeMetadataName);
        JSInvokableAttribute = compilation.GetTypeByMetadataName("Microsoft.JSInterop.JSInvokableAttribute");
        Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        ValueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        ValueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
    }

    public INamedTypeSymbol? MetadataContext { get; }
    public INamedTypeSymbol? BindableModelAttribute { get; }
    public INamedTypeSymbol? JSInvokableAttribute { get; }
    public INamedTypeSymbol? Task { get; }
    public INamedTypeSymbol? TaskOfT { get; }
    public INamedTypeSymbol? ValueTask { get; }
    public INamedTypeSymbol? ValueTaskOfT { get; }

    public static WellKnownTypes? Create(Compilation compilation)
    {
        var types = new WellKnownTypes(compilation);
        return types.MetadataContext is null ? null : types;
    }
}
