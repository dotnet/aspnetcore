// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Mcp.FormMapping;
using Microsoft.AspNetCore.Components.Mcp.Rendering;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Mcp;

public class McpHtmlRendererTests
{
    [Fact]
    public async Task NamedEditForm_BindsDuringInitialization_AndDispatchesItsActualCallback()
    {
        await using var services = CreateServices();
        await using var scope = services.CreateAsyncScope();
        var probe = scope.ServiceProvider.GetRequiredService<Probe>();
        scope.ServiceProvider.GetRequiredService<McpFormDataProvider>().Initialize(
            "submit", new Dictionary<string, StringValues>
            {
                ["Input.Name"] = "bound-name",
                ["Input.Values[0]"] = "17",
                ["Input.Values[1]"] = "29",
            }, new FormFileCollection());
        await using var renderer = CreateRenderer(scope.ServiceProvider);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = renderer.BeginRenderingComponent(typeof(BoundForm), ParameterView.Empty);
            await root.QuiescenceTask;
            Assert.Equal("bound-name", probe.InitializedName);
            Assert.Equal(new[] { 17, 29 }, probe.InitializedValues);
            Assert.Contains("name=\"_handler\" value=\"submit\"", root.ToHtmlString());
            Assert.Equal(0, probe.SubmitCount);

            await renderer.DispatchSubmitAsync("submit");

            Assert.Equal(1, probe.SubmitCount);
            Assert.Equal("bound-name", probe.SubmittedName);
            Assert.Contains("submitted:bound-name", root.ToHtmlString());
            Assert.Equal(0, probe.AfterRenderCount);
        });
    }

    [Fact]
    public async Task AsyncFormProducer_IsDiscoverableAfterQuiescence()
    {
        await using var services = CreateServices();
        await using var scope = services.CreateAsyncScope();
        var probe = scope.ServiceProvider.GetRequiredService<Probe>();
        probe.InitializationGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var renderer = CreateRenderer(scope.ServiceProvider);

        var root = await renderer.Dispatcher.InvokeAsync(
            () => renderer.BeginRenderingComponent(typeof(BoundForm), ParameterView.Empty));
        Assert.False(root.QuiescenceTask.IsCompleted);
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await Assert.ThrowsAsync<McpFormSubmissionException>(() => renderer.DispatchSubmitAsync("submit"));
        });

        probe.InitializationGate.SetResult();
        await root.QuiescenceTask;
        await renderer.Dispatcher.InvokeAsync(() => renderer.DispatchSubmitAsync("submit"));

        Assert.Equal(1, probe.SubmitCount);
    }

    [Fact]
    public async Task MissingNamedForm_DoesNotInvokeAnotherCallback()
    {
        await using var services = CreateServices();
        await using var scope = services.CreateAsyncScope();
        var probe = scope.ServiceProvider.GetRequiredService<Probe>();
        await using var renderer = CreateRenderer(scope.ServiceProvider);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = renderer.BeginRenderingComponent(typeof(BoundForm), ParameterView.Empty);
            await root.QuiescenceTask;

            var exception = await Assert.ThrowsAsync<McpFormSubmissionException>(() => renderer.DispatchSubmitAsync("missing"));

            Assert.Contains("no form on the page", exception.Message);
            Assert.Equal(0, probe.SubmitCount);
        });
    }

    [Fact]
    public async Task AmbiguousNamedForm_IsAnApplicationError()
    {
        await using var services = CreateServices();
        await using var scope = services.CreateAsyncScope();
        var probe = scope.ServiceProvider.GetRequiredService<Probe>();
        await using var renderer = CreateRenderer(scope.ServiceProvider);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = renderer.BeginRenderingComponent(typeof(DuplicateForms), ParameterView.Empty);
            await root.QuiescenceTask;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.DispatchSubmitAsync("submit"));

            Assert.Contains("more than one named submit event", exception.Message);
            Assert.Equal(0, probe.SubmitCount);
        });
    }

    [Fact]
    public async Task DisposingRenderer_DisposesItsComponentTree()
    {
        await using var services = CreateServices();
        await using var scope = services.CreateAsyncScope();
        var probe = scope.ServiceProvider.GetRequiredService<Probe>();
        var renderer = CreateRenderer(scope.ServiceProvider);
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = renderer.BeginRenderingComponent(typeof(BoundForm), ParameterView.Empty);
            await root.QuiescenceTask;
        });

        await renderer.DisposeAsync();

        Assert.Equal(1, probe.DisposeCount);
        Assert.Equal(0, probe.SubmitCount);
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<Probe>();
        services.AddScoped<McpFormDataProvider>();
        services.AddScoped(_ => new FormDataMapperOptions());
        services.AddScoped<IFormValueMapper, McpFormValueMapper>();
        services.AddSupplyValueFromFormProvider();
        services.AddScoped<NavigationManager, TestNavigationManager>();
        return services.BuildServiceProvider();
    }

    private static McpHtmlRenderer CreateRenderer(IServiceProvider services)
        => new(services, services.GetRequiredService<ILoggerFactory>());

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://example.test/", "https://example.test/form");
    }

    private sealed class Probe
    {
        public TaskCompletionSource? InitializationGate { get; set; }
        public string? InitializedName { get; set; }
        public int[] InitializedValues { get; set; } = [];
        public int SubmitCount { get; set; }
        public string? SubmittedName { get; set; }
        public int AfterRenderCount { get; set; }
        public int DisposeCount { get; set; }
    }

    private sealed class BoundForm : ComponentBase, IDisposable
    {
        private bool _ready;
        private string? _submittedName;

        [Inject] private Probe Probe { get; set; } = default!;
        [SupplyParameterFromForm] private InputModel Input { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            Input ??= new();
            Probe.InitializedName = Input.Name;
            Probe.InitializedValues = Input.Values.ToArray();
            if (Probe.InitializationGate is { } gate)
            {
                await gate.Task;
            }

            _ready = true;
        }

        protected override void OnAfterRender(bool firstRender) => Probe.AfterRenderCount++;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (!_ready)
            {
                return;
            }

            builder.OpenComponent<EditForm>(0);
            builder.AddComponentParameter(1, nameof(EditForm.Model), Input);
            builder.AddComponentParameter(2, nameof(EditForm.FormName), "submit");
            builder.AddComponentParameter(3, nameof(EditForm.OnValidSubmit), EventCallback.Factory.Create<EditContext>(this, () =>
            {
                Probe.SubmitCount++;
                Probe.SubmittedName = Input.Name;
                _submittedName = Input.Name;
            }));
            builder.CloseComponent();
            if (_submittedName is not null)
            {
                builder.AddContent(4, $"submitted:{_submittedName}");
            }
        }

        public void Dispose() => Probe.DisposeCount++;
    }

    private sealed class DuplicateForms : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BoundForm>(0);
            builder.CloseComponent();
            builder.OpenComponent<BoundForm>(1);
            builder.CloseComponent();
        }
    }

    public sealed class InputModel
    {
        public string Name { get; set; } = "";
        public List<int> Values { get; set; } = [];
    }
}
