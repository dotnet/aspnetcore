// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Mcp.Hosting;
using Microsoft.AspNetCore.Components.Mcp.Protocol;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace Microsoft.AspNetCore.Components.Mcp;

public class McpComponentInvocationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualSdkOperations_ResolveFreshExecutionPipelineInTheirOwnScope(bool existingTool)
    {
        var observation = new Observation();
        await using var services = CreateServices(observation, existingTool);
        var input = new Pipe();
        var output = new Pipe();
        await using var server = McpServer.Create(new StreamServerTransport(input.Reader.AsStream(), output.Writer.AsStream()),
            services.GetRequiredService<IOptions<McpServerOptions>>().Value, services.GetRequiredService<ILoggerFactory>(), services);
        using var lifetime = new CancellationTokenSource();
        var serverTask = server.RunAsync(lifetime.Token);
        try
        {
            await using var client = await McpClient.CreateAsync(new StreamClientTransport(input.Writer.AsStream(), output.Reader.AsStream()));
            var opened = await client.CallToolAsync(existingTool ? "existing" : "lifecycle");
            Assert.False(opened.IsError == true, string.Join("\n", observation.Errors));
            var view = opened.Meta!["blazor"]!.AsObject();
            var first = ReadScope(view);
            var posted = await client.CallToolAsync("lifecycle_dispatch", new Dictionary<string, object?>
            {
                ["viewId"] = view["viewId"]!.GetValue<string>(),
                ["location"] = view["location"]!.GetValue<string>(),
                ["method"] = "POST",
                ["fields"] = new Dictionary<string, string[]> { ["_handler"] = ["save"], ["Input.Value"] = ["saved"] },
            });
            Assert.False(posted.IsError == true, string.Join("\n", posted.Content.OfType<TextContentBlock>().Select(block => block.Text)));
            var second = ReadScope(posted.Meta!["blazor"]!.AsObject());
            Assert.Contains("saved", posted.Meta!["blazor"]!["document"]!.GetValue<string>());
            var fresh = await client.CallToolAsync("lifecycle_dispatch", new Dictionary<string, object?>
            {
                ["viewId"] = view["viewId"]!.GetValue<string>(),
                ["location"] = view["location"]!.GetValue<string>(),
                ["method"] = "GET",
            });
            Assert.False(fresh.IsError == true);
            var third = ReadScope(fresh.Meta!["blazor"]!.AsObject());
            Assert.Equal(3, new[] { first, second, third }.Distinct().Count());
            Assert.Equal(new[] { "component", "scope", "component", "scope", "component", "scope" }, observation.Disposal);
            Assert.Equal(new[] { "", "saved", "" }, observation.InitialValues);
            Assert.Equal(1, observation.Effects);
            Assert.Equal(existingTool ? 1 : 0, observation.Openings);
            Assert.Equal(existingTool ? 1 : 0, observation.Projections);
            if (existingTool)
            {
                Assert.Equal(first, Assert.Single(observation.ProjectedScopes));
            }
            Assert.Equal(3, observation.Hosts.Count);
            Assert.Equal(3, observation.Hosts.Distinct(ReferenceEqualityComparer.Instance).Count());
            Assert.Equal(3, observation.Invokers.Count);
            Assert.Equal(3, observation.Invokers.Distinct(ReferenceEqualityComparer.Instance).Count());
            Assert.Contains("effect:1 value:", fresh.Meta!["blazor"]!["document"]!.GetValue<string>());
            await client.CallToolAsync("lifecycle_dispatch", new Dictionary<string, object?>
            {
                ["viewId"] = view["viewId"]!.GetValue<string>(),
                ["method"] = "CLOSE",
            });
        }
        finally
        {
            lifetime.Cancel();
            await serverTask;
        }
    }

    [Fact]
    public async Task ActualSdkClose_WaitsForTheEntireInvocationScopeToFinishDisposing()
    {
        var observation = new Observation();
        await using var services = CreateServices(observation, existingTool: false);
        var input = new Pipe();
        var output = new Pipe();
        await using var server = McpServer.Create(new StreamServerTransport(input.Reader.AsStream(), output.Writer.AsStream()),
            services.GetRequiredService<IOptions<McpServerOptions>>().Value, services.GetRequiredService<ILoggerFactory>(), services);
        using var lifetime = new CancellationTokenSource();
        var serverTask = server.RunAsync(lifetime.Token);
        try
        {
            await using var client = await McpClient.CreateAsync(new StreamClientTransport(input.Writer.AsStream(), output.Reader.AsStream()));
            var opened = await client.CallToolAsync("lifecycle");
            Assert.False(opened.IsError == true);
            var view = opened.Meta!["blazor"]!.AsObject();
            var posted = client.CallToolAsync("lifecycle_dispatch", new Dictionary<string, object?>
            {
                ["viewId"] = view["viewId"]!.GetValue<string>(),
                ["location"] = view["location"]!.GetValue<string>(),
                ["method"] = "POST",
                ["fields"] = new Dictionary<string, string[]> { ["_handler"] = ["save"], ["Input.Value"] = ["pending"] },
            }).AsTask();
            await observation.ActionStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var close = client.CallToolAsync("lifecycle_dispatch", new Dictionary<string, object?>
            {
                ["viewId"] = view["viewId"]!.GetValue<string>(),
                ["method"] = "CLOSE",
            }).AsTask();
            await observation.ActionCancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.False(posted.IsCompleted);
            Assert.False(close.IsCompleted);
            Assert.Equal(new[] { "component", "scope" }, observation.Disposal);
            observation.ActionRelease.SetResult();
            await observation.ScopeDisposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.False(posted.IsCompleted);
            Assert.False(close.IsCompleted);
            Assert.Equal(new[] { "component", "scope", "component" }, observation.Disposal);
            observation.ScopeDisposalRelease.SetResult();
            await posted.WaitAsync(TimeSpan.FromSeconds(10));
            var closed = await close.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.False(closed.IsError == true);
            Assert.Equal(0, observation.Effects);
            Assert.Equal(new[] { "component", "scope", "component", "scope", "scope" }, observation.Disposal);
        }
        finally
        {
            observation.ActionRelease.TrySetResult();
            observation.ScopeDisposalRelease.TrySetResult();
            lifetime.Cancel();
            await serverTask;
        }
    }

    [Fact]
    public async Task ActualSdkResourceReads_RenderFreshScopedShellsWithoutOpeningOrProjection()
    {
        var observation = new Observation();
        await using var services = CreateServices(observation, existingTool: true);
        var input = new Pipe();
        var output = new Pipe();
        await using var server = McpServer.Create(new StreamServerTransport(input.Reader.AsStream(), output.Writer.AsStream()),
            services.GetRequiredService<IOptions<McpServerOptions>>().Value, services.GetRequiredService<ILoggerFactory>(), services);
        using var lifetime = new CancellationTokenSource();
        var serverTask = server.RunAsync(lifetime.Token);
        try
        {
            await using var client = await McpClient.CreateAsync(new StreamClientTransport(input.Writer.AsStream(), output.Reader.AsStream()));
            var first = Assert.IsType<TextResourceContents>(Assert.Single((await client.ReadResourceAsync("ui://blazor/lifecycle")).Contents));
            var second = Assert.IsType<TextResourceContents>(Assert.Single((await client.ReadResourceAsync("ui://blazor/lifecycle")).Contents));
            Assert.Contains("shell:True", first.Text);
            Assert.Contains("shell:True", second.Text);
            Assert.NotEqual(first.Text, second.Text);
            Assert.Equal(2, observation.Hosts.Distinct(ReferenceEqualityComparer.Instance).Count());
            Assert.Empty(observation.Invokers);
            Assert.Equal(0, observation.Openings);
            Assert.Equal(0, observation.Projections);
            Assert.Equal(0, observation.Effects);
            Assert.Equal(new[] { "component", "scope", "component", "scope" }, observation.Disposal);
        }
        finally
        {
            lifetime.Cancel();
            await serverTask;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualSdkOpening_WaitsForInitializationAndInvocationScopeDisposal(bool existingTool)
    {
        var observation = new Observation
        {
            InitializationRelease = new(TaskCreationOptions.RunContinuationsAsynchronously),
        };
        await using var services = CreateServices(observation, existingTool);
        var input = new Pipe();
        var output = new Pipe();
        await using var server = McpServer.Create(new StreamServerTransport(input.Reader.AsStream(), output.Writer.AsStream()),
            services.GetRequiredService<IOptions<McpServerOptions>>().Value, services.GetRequiredService<ILoggerFactory>(), services);
        using var lifetime = new CancellationTokenSource();
        var serverTask = server.RunAsync(lifetime.Token);
        try
        {
            await using var client = await McpClient.CreateAsync(new StreamClientTransport(input.Writer.AsStream(), output.Reader.AsStream()));
            var opening = client.CallToolAsync(existingTool ? "existing" : "lifecycle").AsTask();
            await observation.InitializationStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.False(opening.IsCompleted);
            Assert.Empty(observation.Disposal);
            Assert.Single(observation.Hosts);
            Assert.Single(observation.Invokers);
            Assert.Equal(existingTool ? 1 : 0, observation.Projections);
            observation.InitializationRelease.SetResult();
            await observation.ScopeDisposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.False(opening.IsCompleted);
            Assert.Equal(new[] { "component" }, observation.Disposal);
            observation.ScopeDisposalRelease.SetResult();
            var opened = await opening.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.False(opened.IsError == true);
            Assert.Equal(new[] { "component", "scope" }, observation.Disposal);
            var view = opened.Meta!["blazor"]!.AsObject();
            await client.CallToolAsync("lifecycle_dispatch", new Dictionary<string, object?>
            {
                ["viewId"] = view["viewId"]!.GetValue<string>(),
                ["method"] = "CLOSE",
            });
        }
        finally
        {
            observation.InitializationRelease.TrySetResult();
            observation.ScopeDisposalRelease.TrySetResult();
            lifetime.Cancel();
            await serverTask;
        }
    }

    private static Guid ReadScope(JsonObject view)
    {
        var html = view["document"]!.GetValue<string>();
        const string prefix = "scope:";
        var start = html.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
        return Guid.Parse(html.Substring(start, 36));
    }

    private static ServiceProvider CreateServices(Observation observation, bool existingTool)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.ApplicationName).Returns(nameof(McpComponentInvocationTests));
        environment.SetupGet(value => value.WebRootFileProvider).Returns(new NullFileProvider());
        services.AddSingleton(environment.Object);
        services.AddSingleton(observation);
        services.AddLogging(builder => builder.AddProvider(new CaptureLogger(observation)));
        services.AddScoped<ScopeProbe>();
        var builder = services.AddMcpServer();
        if (existingTool)
        {
            builder.WithTools([McpServerTool.Create(() =>
            {
                observation.Openings++;
                return "initial";
            }, new() { Name = "existing" })])
                .WithRazorComponents<InvocationComponent>("existing", _ =>
                {
                    var scope = Assert.IsType<ScopeProbe>(observation.ActivationScope.Value);
                    Assert.False(scope.Disposed);
                    observation.ProjectedScopes.Enqueue(scope.Id);
                    observation.Projections++;
                    return new Dictionary<string, object?>();
                }, Configure);
        }
        else
        {
            builder.WithRazorComponents<InvocationComponent>(Configure);
        }

        services.AddScoped(provider =>
        {
            provider.GetRequiredService<ScopeProbe>();
            var host = new McpComponentHost(provider);
            observation.Hosts.Enqueue(host);
            provider.GetRequiredService<ScopeProbe>().Host = host;
            return host;
        });
        var invokerDescriptor = services.Single(descriptor => descriptor.ServiceType == typeof(McpComponentInvoker));
        services.Remove(invokerDescriptor);
        services.AddKeyedScoped<McpComponentInvoker>(invokerDescriptor.ServiceKey, (provider, key) =>
        {
            var invoker = (McpComponentInvoker)invokerDescriptor.KeyedImplementationFactory!(provider, key);
            observation.Invokers.Enqueue(invoker);
            var scope = provider.GetRequiredService<ScopeProbe>();
            scope.Invoker = invoker;
            observation.ActivationScope.Value = scope;
            return invoker;
        });
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        static void Configure(RazorComponentMcpOptions options)
        {
            options.Name = "lifecycle";
            options.BaseUri = new Uri("https://example.test/");
        }
    }

    private sealed class Observation
    {
        public ConcurrentQueue<McpComponentHost> Hosts { get; } = new();
        public ConcurrentQueue<McpComponentInvoker> Invokers { get; } = new();
        public ConcurrentQueue<string> Disposal { get; } = new();
        public ConcurrentQueue<string> InitialValues { get; } = new();
        public AsyncLocal<ScopeProbe?> ActivationScope { get; } = new();
        public ConcurrentQueue<Guid> ProjectedScopes { get; } = new();
        public TaskCompletionSource ActionStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource InitializationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource? InitializationRelease { get; init; }
        public TaskCompletionSource ActionRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ActionCancellationObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ScopeDisposalStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ScopeDisposalRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Guid PendingScopeId;
        public int Effects;
        public int Openings;
        public int Projections;
        public ConcurrentQueue<string> Errors { get; } = new();
    }

    private sealed class ScopeProbe(Observation observation) : IAsyncDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public McpComponentHost Host { get; set; } = default!;
        public McpComponentInvoker Invoker { get; set; } = default!;
        public bool Disposed { get; private set; }

        public async ValueTask DisposeAsync()
        {
            if (Id == observation.PendingScopeId)
            {
                observation.ScopeDisposalStarted.SetResult();
                await observation.ScopeDisposalRelease.Task;
            }

            Disposed = true;
            observation.Disposal.Enqueue("scope");
        }
    }

    private sealed class InvocationComponent : ComponentBase, IDisposable
    {
        [Inject] private ScopeProbe Scope { get; set; } = default!;
        [Inject] private IServiceProvider Services { get; set; } = default!;
        [Inject] private McpComponentRequestContext Context { get; set; } = default!;
        [CascadingParameter] private McpComponentRequestContext CascadedContext { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private Observation Observation { get; set; } = default!;
        [SupplyParameterFromForm] private InputModel Input { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            Input ??= new();
            Assert.Same(Scope.Host, Services.GetRequiredService<McpComponentHost>());
            Assert.Same(Context, CascadedContext);
            if (Context.IsShellRendering)
            {
                Assert.True(Context.IsActive);
                Assert.Equal(Context.Location.AbsoluteUri, Navigation.Uri);
                return;
            }

            var application = Services.GetServices<McpComponentApplication>().Single();
            Assert.Same(Scope.Invoker, Services.GetRequiredKeyedService<McpComponentInvoker>(application));
            Assert.True(Context.IsActive);
            Assert.Equal(Context.Location.AbsoluteUri, Navigation.Uri);
            Observation.InitialValues.Enqueue(Input.Value);
            if (Observation.InitializationRelease is { } release)
            {
                Observation.PendingScopeId = Scope.Id;
                Observation.InitializationStarted.SetResult();
                await release.Task;
                Assert.False(Scope.Disposed);
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Context.IsShellRendering)
            {
                builder.AddContent(0, $"shell:{Context.IsShellRendering} scope:{Scope.Id}");
                return;
            }

            builder.AddContent(0, $"scope:{Scope.Id} effect:{Observation.Effects} value:{Input.Value}");
            builder.OpenComponent<EditForm>(1);
            builder.AddComponentParameter(2, nameof(EditForm.Model), Input);
            builder.AddComponentParameter(3, nameof(EditForm.FormName), "save");
            builder.AddComponentParameter(4, nameof(EditForm.OnSubmit), EventCallback.Factory.Create<EditContext>(this, SubmitAsync));
            builder.AddComponentParameter(5, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(_ => _ => { }));
            builder.CloseComponent();
        }

        public void Dispose() => Observation.Disposal.Enqueue("component");

        private async Task SubmitAsync()
        {
            if (Input.Value == "pending")
            {
                Observation.PendingScopeId = Scope.Id;
                using var registration = Context.CancellationToken.Register(() => Observation.ActionCancellationObserved.TrySetResult());
                Observation.ActionStarted.SetResult();
                await Observation.ActionRelease.Task;
                Assert.False(Scope.Disposed);
                Context.CancellationToken.ThrowIfCancellationRequested();
            }

            Observation.Effects++;
        }
    }

    public sealed class InputModel
    {
        public string Value { get; set; } = "";
    }

    private sealed class CaptureLogger(Observation observation) : ILoggerProvider, ILogger
    {
        public ILogger CreateLogger(string categoryName) => this;
        public void Dispose() { }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                observation.Errors.Enqueue(exception?.ToString() ?? formatter(state, exception));
            }
        }
    }
}
