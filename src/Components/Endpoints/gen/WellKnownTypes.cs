// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators;

// Every symbol the generator looks up, resolved once per compilation. A null member means the
// application does not reference the assembly that declares it, which the collectors treat as
// "nothing of this kind to describe" rather than as an error.
internal sealed class WellKnownTypes
{
    public const string ComponentsAssemblyName = "Microsoft.AspNetCore.Components";

    public const string MetadataContextMetadataName = "Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext";
    public const string BindableModelAttributeMetadataName = "Microsoft.AspNetCore.Components.Web.BindableModelAttribute";

    private WellKnownTypes(Compilation compilation)
    {
        MetadataContext = compilation.GetTypeByMetadataName(MetadataContextMetadataName);
        BindableModelAttribute = compilation.GetTypeByMetadataName(BindableModelAttributeMetadataName);
        ComponentInterface = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.IComponent");
        ParameterAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.ParameterAttribute");
        CascadingParameterAttributeBase = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.CascadingParameterAttributeBase");
        InjectAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.InjectAttribute");
        PersistentStateAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.PersistentStateAttribute");
        RouteAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.RouteAttribute");
        LayoutAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.LayoutAttribute");
        RenderModeAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.RenderModeAttribute");
        ExcludeFromInteractiveRoutingAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.ExcludeFromInteractiveRoutingAttribute");
        AttributeUsageAttribute = compilation.GetTypeByMetadataName("System.AttributeUsageAttribute");
        JSInvokableAttribute = compilation.GetTypeByMetadataName("Microsoft.JSInterop.JSInvokableAttribute");
        Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        ValueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        ValueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
    }

    public INamedTypeSymbol? MetadataContext { get; }
    public INamedTypeSymbol? BindableModelAttribute { get; }
    public INamedTypeSymbol? ComponentInterface { get; }
    public INamedTypeSymbol? ParameterAttribute { get; }
    public INamedTypeSymbol? CascadingParameterAttributeBase { get; }
    public INamedTypeSymbol? InjectAttribute { get; }
    public INamedTypeSymbol? PersistentStateAttribute { get; }
    public INamedTypeSymbol? RouteAttribute { get; }
    public INamedTypeSymbol? LayoutAttribute { get; }
    public INamedTypeSymbol? RenderModeAttribute { get; }
    public INamedTypeSymbol? ExcludeFromInteractiveRoutingAttribute { get; }
    public INamedTypeSymbol? AttributeUsageAttribute { get; }
    public INamedTypeSymbol? JSInvokableAttribute { get; }
    public INamedTypeSymbol? Task { get; }
    public INamedTypeSymbol? TaskOfT { get; }
    public INamedTypeSymbol? ValueTask { get; }
    public INamedTypeSymbol? ValueTaskOfT { get; }

    public static WellKnownTypes? Create(Compilation compilation)
    {
        var types = new WellKnownTypes(compilation);
        return types.MetadataContext is null || types.ComponentInterface is null ? null : types;
    }
}
