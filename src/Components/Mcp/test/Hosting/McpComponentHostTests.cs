// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Mcp.DependencyInjection;
using Microsoft.AspNetCore.Components.Mcp.FormMapping;
using Microsoft.AspNetCore.Components.Mcp.Hosting;
using Microsoft.AspNetCore.Components.Mcp.Protocol;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Components.Mcp;

public class McpComponentHostTests
{
    [Fact]
    public async Task EachOperation_OwnsFreshComponentsAndScope_AndDisposesInOrder()
    {
        await using var services = CreateServices();
        var host = CreateHost<BoundOperation>(services);
        var probe = services.GetRequiredService<OperationProbe>();

        var first = await host.RenderAsync(CreateRequest("first"));
        var second = await host.RenderAsync(CreateRequest("second"));

        Assert.Contains("operation-form", first.Html);
        Assert.Contains("operation-form", second.Html);
        Assert.Null(first.Redirect);
        Assert.Null(second.Redirect);
        Assert.Equal(2, probe.ComponentIds.Distinct().Count());
        Assert.Equal(2, probe.ScopeIds.Distinct().Count());
        Assert.Equal(new[] { "first", "second" }, probe.Users);
        Assert.Equal(new[] { "component", "scope", "component", "scope" }, probe.DisposalEvents);
        Assert.Equal(0, probe.Effects);
    }

    [Fact]
    public async Task ActualEditForm_ValidatesMappedInputBeforeBusinessEffects()
    {
        await using var services = CreateServices();
        var host = CreateHost<BoundOperation>(services);
        var probe = services.GetRequiredService<OperationProbe>();

        var valid = await host.RenderAsync(CreateRequest("valid", "bound-value"));
        var invalid = await host.RenderAsync(CreateRequest("invalid", ""));

        Assert.Contains("effect:bound-value", valid.Html);
        Assert.Contains("The Name field is required.", invalid.Html);
        Assert.DoesNotContain("effect:", invalid.Html);
        Assert.Equal(1, probe.Effects);
        Assert.Equal(new[] { "bound-value", "" }, probe.InitializedValues);
        Assert.Equal(new[] { "component", "scope", "component", "scope" }, probe.DisposalEvents);
    }

    [Fact]
    public async Task Cancellation_DoesNotDisposeScopeWhileInitializationStillUsesIt()
    {
        await using var services = CreateServices();
        var probe = services.GetRequiredService<OperationProbe>();
        probe.InitializationGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = CreateHost<BoundOperation>(services);
        using var cancellation = new CancellationTokenSource();

        var operation = host.RenderAsync(CreateRequest("canceled", "not-submitted"), cancellation.Token);
        await probe.InitializationStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();

        Assert.False(operation.IsCompleted);
        Assert.Empty(probe.DisposalEvents);

        probe.InitializationGate.SetResult();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.Equal(0, probe.Effects);
        Assert.Equal(new[] { "component", "scope" }, probe.DisposalEvents);
    }

    [Fact]
    public async Task CacheView_IsTransparentAndExecutesChildrenForEveryOperation()
    {
        await using var services = CreateServices();
        var host = CreateHost<CacheRoot>(services);
        var probe = services.GetRequiredService<OperationProbe>();

        var first = await host.RenderAsync(CreateRequest("first"));
        var second = await host.RenderAsync(CreateRequest("second"));

        Assert.Equal("cache-child:1", first.Html);
        Assert.Equal("cache-child:2", second.Html);
        Assert.Equal(2, probe.CacheChildExecutions);
    }

    [Fact]
    public async Task OrdinaryHttpScopes_KeepTheirDefaultComponentServices()
    {
        await using var services = CreateServices();
        await using var httpScope = services.CreateAsyncScope();
        await using var mcpScope = services.CreateAsyncScope();
        var request = CreateRequest("mcp");
        mcpScope.ServiceProvider.GetRequiredService<McpOperationContext>().Initialize(
            request, new Uri("https://example.test/"), CancellationToken.None);

        Assert.IsNotType<McpNavigationManager>(httpScope.ServiceProvider.GetRequiredService<NavigationManager>());
        Assert.IsNotType<McpFormValueMapper>(httpScope.ServiceProvider.GetRequiredService<IFormValueMapper>());
        Assert.IsNotType<McpAuthenticationStateProvider>(httpScope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>());
        Assert.IsType<McpNavigationManager>(mcpScope.ServiceProvider.GetRequiredService<NavigationManager>());
        Assert.IsType<McpFormValueMapper>(mcpScope.ServiceProvider.GetRequiredService<IFormValueMapper>());
        var authentication = await mcpScope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>().GetAuthenticationStateAsync();
        Assert.Same(request.User, authentication.User);
        Assert.Equal(request.Location.AbsoluteUri, mcpScope.ServiceProvider.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task ShellPhase_DefersRequiredDataButDoesNotWeakenFullRendering()
    {
        await using var services = CreateServices();
        var application = new McpComponentApplication("required", typeof(RequiredRoot),
            new Uri("https://example.test/"), new Uri("https://example.test/form"));
        await using (var shellScope = services.CreateAsyncScope())
        {
            var result = await shellScope.ServiceProvider.GetRequiredService<McpComponentHost>().RenderAsync(
                application, CreateRequest("shell") with { IsShellRendering = true });
            Assert.Equal("authored-loading", result.Html);
        }

        await using (var invalidScope = services.CreateAsyncScope())
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                invalidScope.ServiceProvider.GetRequiredService<McpComponentHost>().RenderAsync(application, CreateRequest("full")));
        }

        await using (var fullScope = services.CreateAsyncScope())
        {
            var result = await fullScope.ServiceProvider.GetRequiredService<McpComponentHost>().RenderAsync(application,
                CreateRequest("full"), McpComponentParameters.Capture(new Dictionary<string, object?>
                {
                    [nameof(RequiredRoot.InitialData)] = "required-data",
                }));
            Assert.Equal("required-data", result.Html);
        }
    }

    private sealed class RequiredRoot : ComponentBase
    {
        [CascadingParameter] private McpComponentRequestContext Context { get; set; } = default!;
        [Parameter] public string InitialData { get; set; } = default!;

        protected override void OnInitialized()
        {
            if (!Context.IsShellRendering)
            {
                ArgumentNullException.ThrowIfNull(InitialData);
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => builder.AddContent(0, Context.IsShellRendering ? "authored-loading" : InitialData);
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.ApplicationName).Returns(nameof(McpComponentHostTests));
        environment.SetupGet(value => value.WebRootFileProvider).Returns(new NullFileProvider());
        services.AddSingleton(environment.Object);
        services.AddMcpComponentServices();
        services.AddSingleton<OperationProbe>();
        services.AddScoped<ScopeTracker>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static OperationHost CreateHost<TComponent>(IServiceProvider services) where TComponent : IComponent
        => new(services, new McpComponentApplication("test", typeof(TComponent), new Uri("https://example.test/"), new Uri("https://example.test/form")));

    private sealed class OperationHost(IServiceProvider services, McpComponentApplication application)
    {
        public async Task<McpRenderResult> RenderAsync(McpComponentRequest request, CancellationToken cancellationToken = default)
        {
            await using var scope = services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<McpComponentHost>().RenderAsync(application, request, cancellationToken);
        }
    }

    private static McpComponentRequest CreateRequest(string user, string? name = null)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, user)], "test"));
        return new(new Uri("https://example.test/form"), principal,
            name is null ? null : "operation-form",
            name is null ? null : new Dictionary<string, StringValues> { ["Input.Name"] = name });
    }

    private sealed class OperationProbe
    {
        public ConcurrentQueue<Guid> ComponentIds { get; } = new();
        public ConcurrentQueue<Guid> ScopeIds { get; } = new();
        public ConcurrentQueue<string?> Users { get; } = new();
        public ConcurrentQueue<string> InitializedValues { get; } = new();
        public ConcurrentQueue<string> DisposalEvents { get; } = new();
        public TaskCompletionSource InitializationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource? InitializationGate { get; set; }
        public int Effects;
        public int CacheChildExecutions;
    }

    private sealed class ScopeTracker(OperationProbe probe) : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void Dispose() => probe.DisposalEvents.Enqueue("scope");
    }

    private sealed class BoundOperation : ComponentBase, IDisposable
    {
        private readonly Guid _id = Guid.NewGuid();
        private string? _effect;

        [Inject] private OperationProbe Probe { get; set; } = default!;
        [Inject] private ScopeTracker Scope { get; set; } = default!;
        [Inject] private McpOperationContext Context { get; set; } = default!;
        [SupplyParameterFromForm] private OperationInput Input { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            Input ??= new();
            Probe.ComponentIds.Enqueue(_id);
            Probe.ScopeIds.Enqueue(Scope.Id);
            Probe.Users.Enqueue(Context.Request.User.Identity?.Name);
            if (Context.Request.HandlerName is not null)
            {
                Probe.InitializedValues.Enqueue(Input.Name);
            }

            Probe.InitializationStarted.TrySetResult();
            if (Probe.InitializationGate is { } gate)
            {
                await gate.Task;
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<EditForm>(0);
            builder.AddComponentParameter(1, nameof(EditForm.Model), Input);
            builder.AddComponentParameter(2, nameof(EditForm.FormName), "operation-form");
            builder.AddComponentParameter(3, nameof(EditForm.OnValidSubmit), EventCallback.Factory.Create<EditContext>(this, () =>
            {
                Interlocked.Increment(ref Probe.Effects);
                _effect = Input.Name;
            }));
            builder.AddComponentParameter(4, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(_ => content =>
            {
                content.OpenComponent<DataAnnotationsValidator>(0);
                content.CloseComponent();
                content.OpenComponent<ValidationSummary>(1);
                content.CloseComponent();
            }));
            builder.CloseComponent();
            if (_effect is not null)
            {
                builder.AddContent(5, $"effect:{_effect}");
            }
        }

        public void Dispose() => Probe.DisposalEvents.Enqueue("component");
    }

    public sealed class OperationInput
    {
        [Required]
        public string Name { get; set; } = "";
    }

    private sealed class CacheRoot : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CacheView>(0);
            builder.AddComponentParameter(1, nameof(CacheView.ChildContent), (RenderFragment)(content =>
            {
                content.OpenComponent<CacheChild>(0);
                content.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class CacheChild : ComponentBase
    {
        private int _execution;

        [Inject] private OperationProbe Probe { get; set; } = default!;

        protected override void OnInitialized() => _execution = Interlocked.Increment(ref Probe.CacheChildExecutions);

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => builder.AddContent(0, $"cache-child:{_execution}");
    }
}
