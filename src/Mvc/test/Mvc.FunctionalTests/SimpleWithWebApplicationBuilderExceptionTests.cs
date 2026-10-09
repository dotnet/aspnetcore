// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class SimpleWithWebApplicationBuilderExceptionTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<SimpleWebSiteWithWebApplicationBuilderException.Program>(LoggerFactory);
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<SimpleWebSiteWithWebApplicationBuilderException.Program> Factory { get; private set; }

    [Fact]
    public void ExceptionThrownFromApplicationCanBeObserved()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Factory.CreateClient());
        Assert.Equal("This application failed to start", ex.Message);
    }

    [Fact]
    public void ExceptionThrownFromHostStartCanBeObservedAfterApplicationDisposesHost()
    {
        using var factory = new StartAfterHostDisposedFactory();

        var ex = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Equal("Startup validation failed", ex.Message);
    }

    // Fails the application's app.Run() with a ValidateOnStart error, and only starts the deferred host
    // after Run() has disposed the application's host.
    private sealed class StartAfterHostDisposedFactory : WebApplicationFactory<SimpleWebSiteWithWebApplicationBuilder.Program>
    {
        private readonly TaskCompletionSource _hostDisposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_ => new DisposeNotifier(_hostDisposed));

                // Depending on DisposeNotifier makes the application's service provider create it during startup
                // validation, so the provider also disposes it when the host is disposed.
                services.AddOptions<StartupValidatedOptions>()
                    .Validate<DisposeNotifier>((_, _) => false, "Startup validation failed")
                    .ValidateOnStart();
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = builder.Build();
            _hostDisposed.Task.DefaultTimeout().GetAwaiter().GetResult();
            host.Start();
            return host;
        }
    }

    private sealed class DisposeNotifier(TaskCompletionSource disposed) : IDisposable
    {
        public void Dispose() => disposed.TrySetResult();
    }

    private sealed class StartupValidatedOptions
    {
    }
}
