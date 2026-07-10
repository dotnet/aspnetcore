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
    public async Task Renders_ProviderFragment_WhenActivatedRegistryPopulatedAndProviderReturnsFragment()
    {
        var editContext = new EditContext(new object());
        Activate(editContext);
        RegisterField(editContext, "F");

        var renderer = CreateRenderer(provider: new FakeProvider(CarrierFragment()));

        var elementName = await RenderClientValidationDataAndGetCarrierElementName(renderer, editContext);

        Assert.Equal(CarrierElementName, elementName);
    }

    [Fact]
    public async Task NoOp_WhenNotActivated()
    {
        var editContext = new EditContext(new object());
        // Activation flag deliberately not written.
        RegisterField(editContext, "F");

        var renderer = CreateRenderer(provider: new FakeProvider(CarrierFragment()));

        var elementName = await RenderClientValidationDataAndGetCarrierElementName(renderer, editContext);

        Assert.Null(elementName);
    }

    [Fact]
    public async Task NoOp_WhenNoFieldsRegistered()
    {
        // Activated and the provider would return a fragment, but no input registered a field
        // (e.g. interactive render modes, where InputBase does not register). The component
        // short-circuits at the registry check before invoking the provider.
        var editContext = new EditContext(new object());
        Activate(editContext);

        var renderer = CreateRenderer(provider: new FakeProvider(CarrierFragment()));

        var elementName = await RenderClientValidationDataAndGetCarrierElementName(renderer, editContext);

        Assert.Null(elementName);
    }

    [Fact]
    public async Task NoOp_WhenProviderNotRegistered()
    {
        // Server / WASM / interactive paths: a validator activates client validation, but no
        // ClientValidationProvider is registered in DI, so the optional Services lookup returns
        // null and the component renders nothing.
        var editContext = new EditContext(new object());
        Activate(editContext);
        RegisterField(editContext, "F");

        var renderer = CreateRenderer(provider: null);

        var elementName = await RenderClientValidationDataAndGetCarrierElementName(renderer, editContext);

        Assert.Null(elementName);
    }

    [Fact]
    public async Task NoOp_WhenProviderReturnsNullFragment()
    {
        // The provider decides there is nothing to emit (e.g. none of the rendered fields are
        // validated on the server) and returns null, so no carrier element is rendered.
        var editContext = new EditContext(new object());
        Activate(editContext);
        RegisterField(editContext, "F");

        var renderer = CreateRenderer(provider: new FakeProvider(fragment: null));

        var elementName = await RenderClientValidationDataAndGetCarrierElementName(renderer, editContext);

        Assert.Null(elementName);
    }

    [Fact]
    public async Task EditForm_RendersClientValidationDataInsideEditContextCascade()
    {
        // End-to-end at the render layer: <EditForm><DataAnnotationsValidator/></EditForm> must
        // reach ClientValidationData, which renders the provider's fragment (the carrier element).
        //
        // This pins three things at once:
        //   (a) DataAnnotationsValidator successfully writes the activation flag.
        //   (b) ClientValidationData is inside the EditContext cascade scope so it resolves the
        //       cascading parameter (its [CascadingParameter] EditContext is populated).
        //   (c) Render order: validators and inputs inside ChildContent initialize before
        //       ClientValidationData renders, so the flag and the registered fields are observable.
        var renderer = CreateRenderer(provider: new FakeProvider(CarrierFragment()));

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

    // Mirrors DataAnnotationsValidator: writes the activation flag keyed by the validator type.
    private static void Activate(EditContext editContext)
        => editContext.Properties[typeof(DataAnnotationsValidator)] = true;

    private static void RegisterField(EditContext editContext, string name)
        => RenderedFieldRegistry.GetOrCreate(editContext).Register(editContext.Field(name), name);

    // A stand-in for the fragment a real ClientValidationProvider returns: emits the carrier element.
    private static RenderFragment CarrierFragment()
        => builder =>
        {
            builder.OpenElement(0, CarrierElementName);
            builder.AddAttribute(1, "data-rules", "{}");
            builder.CloseElement();
        };

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
                // ClientValidationData reads before invoking the provider.
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
        private readonly RenderFragment? _fragment;
        public FakeProvider(RenderFragment? fragment) => _fragment = fragment;
        public override RenderFragment? RenderClientValidationRules(
            EditContext editContext,
            IReadOnlyDictionary<FieldIdentifier, string> renderedFields) => _fragment;
    }

    private sealed class NullFormValueMapper : IFormValueMapper
    {
        public bool CanMap(Type valueType, string scopeName, string? formName) => false;
        public void Map(FormValueMappingContext context) { }
    }
}
