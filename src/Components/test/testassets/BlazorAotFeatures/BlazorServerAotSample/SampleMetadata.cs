// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorServerAotSample;

/// <summary>
/// The application's compile-time metadata. Every member is implemented by the Blazor Native AOT
/// metadata source generator, which walks the referenced Pages library.
/// </summary>
/// <remarks>
/// The <see cref="BindableModelAttribute"/> entries name the form models. The generator describes each
/// named type and everything reachable from it, so a binding expression can be walked instead of
/// compiled. The component types themselves are found automatically.
/// </remarks>
[BindableModel(ModelType = typeof(Pages.Pages.DataBinding.Person))]
[BindableModel(ModelType = typeof(Pages.Pages.Forms.EmailModel))]
[BindableModel(ModelType = typeof(Pages.Pages.Storage.Profile))]
[BindableModel(ModelType = typeof(Pages.Pages.Persistence.Snapshot))]
[BindableModel(ModelType = typeof(Pages.BindingRoot))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.CascadingValue<string>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.CascadingValue<global::Microsoft.AspNetCore.Components.Forms.EditContext>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.Forms.ValidationMessage<string>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<string>))]
[ComponentTypeInfo(typeof(global::BlazorServerAotSample.Pages.TypedList<string>))]
[ComponentTypeInfo(typeof(global::BlazorServerAotSample.Pages.TypedList<int>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.Forms.InputDate<DateTime>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.Forms.InputNumber<int>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.Forms.InputRadio<string>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.Forms.InputRadioGroup<string>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.Forms.InputSelect<string>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.Forms.Label<string>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.QuickGrid.QuickGrid<string>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.QuickGrid.PropertyColumn<string, string>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.QuickGrid.TemplateColumn<string>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.QuickGrid.Infrastructure.ColumnsCollectedNotifier<string>))]
[ComponentTypeInfo(typeof(global::Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticatorViewCore<global::Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationState>))]
internal sealed partial class SampleMetadata : RazorComponentsMetadataContext
{
    /// <summary>
    /// Describing a type for binding does not describe it for serialization, so the types the
    /// framework round-trips through JSON on this application's behalf — protected browser storage
    /// and persistent component state — are named here as well.
    /// </summary>
    public override IJsonTypeInfoResolver? JsonTypeInfoResolver => global::System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(
        SampleJsonContext.Default,
        CompositionCoreJsonContext.Default,
        CompositionInteropJsonContext.Default,
        CompositionStateJsonContext.Default,
        CustomEventJsonContext.Default,
        DynamicRootJsonContext.Default,
        GeneratedMetadataJsonContext.Default,
        JsMatrixJsonContext.Default,
        ServerStateJsonContext.Default,
        ResolverPrecedenceFirstContext.Default,
        ResolverPrecedenceSecondContext.Default);
}

[JsonSerializable(typeof(Pages.Pages.Storage.Profile))]
[JsonSerializable(typeof(Pages.Pages.Persistence.Snapshot))]
internal sealed partial class SampleJsonContext : JsonSerializerContext;
