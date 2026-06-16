// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

public class ClientValidationDataTest
{
    private const string CarrierElementName = "blazor-client-validation-data";

    [Fact]
    public async Task Renders_BlazorClientValidationData_WhenMarkerSetRegistryPopulatedAndProviderReturnsNonNull()
    {
        var editContext = new EditContext(new object());
        editContext.Properties[typeof(ClientValidationMarker)] = true; // any non-null value satisfies the marker contract
        RegisterField(editContext, "F");

        var renderer = CreateRenderer(provider: new FakeProvider(SampleDescriptor()));

        var elementName = await RenderClientValidationDataAndGetCarrierElementName(renderer, editContext);

        Assert.Equal(CarrierElementName, elementName);
    }

    [Fact]
    public async Task NoOp_WhenMarkerNotSet()
    {
        var editContext = new EditContext(new object());
        // Marker deliberately not written.
        RegisterField(editContext, "F");

        var renderer = CreateRenderer(provider: new FakeProvider(SampleDescriptor()));

        var elementName = await RenderClientValidationDataAndGetCarrierElementName(renderer, editContext);

        Assert.Null(elementName);
    }

    [Fact]
    public async Task NoOp_WhenNoFieldsRegistered()
    {
        // Marker is set and the provider would return a descriptor, but no input registered a
        // field (e.g. interactive render modes, where InputBase does not register). The component
        // short-circuits at the registry check before resolving the provider.
        var editContext = new EditContext(new object());
        editContext.Properties[typeof(ClientValidationMarker)] = true;

        var renderer = CreateRenderer(provider: new FakeProvider(SampleDescriptor()));

        var elementName = await RenderClientValidationDataAndGetCarrierElementName(renderer, editContext);

        Assert.Null(elementName);
    }

    [Fact]
    public async Task NoOp_WhenProviderNotRegistered()
    {
        // Server / WASM / interactive paths: marker is set by a validator, but no
        // ClientValidationProvider is registered in DI, so the optional Services lookup
        // returns null and the component renders nothing.
        var editContext = new EditContext(new object());
        editContext.Properties[typeof(ClientValidationMarker)] = true; // any non-null value satisfies the marker contract
        RegisterField(editContext, "F");

        var renderer = CreateRenderer(provider: null);

        var elementName = await RenderClientValidationDataAndGetCarrierElementName(renderer, editContext);

        Assert.Null(elementName);
    }

    [Theory]
    [InlineData(/* providerReturnsNull */ true)]
    [InlineData(/* providerReturnsNull */ false)]
    public async Task NoOp_WhenProviderReturnsNullOrEmptyDescriptor(bool providerReturnsNull)
    {
        // Both null and an empty-fields descriptor must short-circuit before serialization,
        // so no <blazor-client-validation-data> element is emitted.
        var editContext = new EditContext(new object());
        editContext.Properties[typeof(ClientValidationMarker)] = true; // any non-null value satisfies the marker contract
        RegisterField(editContext, "F");

        var descriptor = providerReturnsNull
            ? null
            : new ClientValidationFormDescriptor(Array.Empty<ClientValidationFieldDescriptor>());

        var renderer = CreateRenderer(provider: new FakeProvider(descriptor));

        var elementName = await RenderClientValidationDataAndGetCarrierElementName(renderer, editContext);

        Assert.Null(elementName);
    }

    [Fact]
    public async Task EditForm_RendersClientValidationDataInsideEditContextCascade()
    {
        // End-to-end at the render layer: <EditForm><DataAnnotationsValidator/></EditForm>
        // must produce a <blazor-client-validation-data> element with the serialized rules.
        //
        // This pins three things at once:
        //   (a) DataAnnotationsValidator successfully writes the marker.
        //   (b) ClientValidationData is inside the EditContext cascade scope so it resolves
        //       the cascading parameter (its [CascadingParameter] EditContext is populated).
        //   (c) Render order: validators inside ChildContent initialize before
        //       ClientValidationData renders, so the marker is observable.
        var renderer = CreateRenderer(provider: new FakeProvider(new ClientValidationFormDescriptor(
            new List<ClientValidationFieldDescriptor>
            {
                new(nameof(EditFormTestModel.Name), new List<ClientValidationRule>
                {
                    new("required", "Name is required."),
                }),
            })));

        var host = new EditFormHostComponent { Model = new EditFormTestModel() };
        var hostId = renderer.AssignRootComponentId(host);
        await renderer.RenderRootComponentAsync(hostId);

        // Walk every component frame in the batch and look for an element frame whose name
        // matches the carrier. ClientValidationData is a nested component reached through
        // CascadingValue<EditContext>, so a recursive walk is needed.
        var found = WalkAllFramesForElement(renderer, CarrierElementName);
        Assert.True(found, $"Expected to find <{CarrierElementName}> emitted by ClientValidationData inside EditForm.");
    }

    // ---- Helpers ----

    private static void RegisterField(EditContext editContext, string name)
        => RenderedFieldRegistry.GetOrCreate(editContext).Register(editContext.Field(name), name);

    private static ClientValidationFormDescriptor SampleDescriptor()
        => new(new List<ClientValidationFieldDescriptor>
        {
            new("F", new List<ClientValidationRule> { new("required", "F is required.") }),
        });

    private static TestRenderer CreateRenderer(ClientValidationProvider? provider)
    {
        var services = new ServiceCollection();
        if (provider is not null)
        {
            services.AddSingleton(provider);
        }
        // EditForm dependencies for the wiring test; ignored by the standalone component tests.
        services.AddSingleton<IFormValueMapper, NullFormValueMapper>();
        services.AddAntiforgery();
        services.AddLogging();
        services.AddSingleton<ComponentStatePersistenceManager>();
        services.AddSingleton(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State);
        services.AddSingleton<AntiforgeryStateProvider, DefaultAntiforgeryStateProvider>();
        return new TestRenderer(services.BuildServiceProvider());
    }

    private static async Task<string?> RenderClientValidationDataAndGetCarrierElementName(
        TestRenderer renderer, EditContext editContext)
    {
        var host = new ClientValidationDataHostComponent { EditContext = editContext };
        var hostId = renderer.AssignRootComponentId(host);
        await renderer.RenderRootComponentAsync(hostId);

        return WalkAllFramesForElement(renderer, CarrierElementName) ? CarrierElementName : null;
    }

    private static bool WalkAllFramesForElement(TestRenderer renderer, string elementName)
    {
        var batch = renderer.Batches.Single();
        foreach (var componentFrame in batch.ReferenceFrames)
        {
            if (componentFrame.FrameType == RenderTreeFrameType.Component)
            {
                var frames = renderer.GetCurrentRenderTreeFrames(componentFrame.ComponentId);
                for (var i = 0; i < frames.Count; i++)
                {
                    if (frames.Array[i].FrameType == RenderTreeFrameType.Element
                        && frames.Array[i].ElementName == elementName)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // Standalone host: renders just <ClientValidationData /> inside a CascadingValue<EditContext>,
    // mirroring what EditForm does but without the rest of EditForm's surface.
    private sealed class ClientValidationDataHostComponent : ComponentBase
    {
        public EditContext EditContext { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.AddComponentParameter(1, "IsFixed", true);
            builder.AddComponentParameter(2, "Value", EditContext);
            builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<ClientValidationData>(0);
                b.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class EditFormHostComponent : ComponentBase
    {
        public EditFormTestModel Model { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<EditForm>(0);
            builder.AddComponentParameter(1, "Model", Model);
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment<EditContext>)(_ => childBuilder =>
            {
                childBuilder.OpenComponent<DataAnnotationsValidator>(0);
                childBuilder.CloseComponent();

                // A real input registers its field on the EditContext (gated on AssignedRenderMode
                // is null, which holds in the test renderer), populating the registry that
                // ClientValidationData reads before emitting the carrier.
                childBuilder.OpenComponent<InputText>(1);
                childBuilder.AddComponentParameter(2, "Value", Model.Name);
                childBuilder.AddComponentParameter(3, "ValueExpression", (Expression<Func<string>>)(() => Model.Name));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class EditFormTestModel
    {
        [Required] public string Name { get; set; } = "";
    }

    private sealed class FakeProvider : ClientValidationProvider
    {
        private readonly ClientValidationFormDescriptor? _descriptor;
        public FakeProvider(ClientValidationFormDescriptor? descriptor) => _descriptor = descriptor;
        public override ClientValidationFormDescriptor? GetFormDescriptor(
            EditContext editContext,
            IReadOnlyDictionary<FieldIdentifier, string> renderedFields) => _descriptor;
    }

    private sealed class NullFormValueMapper : IFormValueMapper
    {
        public bool CanMap(Type valueType, string scopeName, string? formName) => false;
        public void Map(FormValueMappingContext context) { }
    }
}
