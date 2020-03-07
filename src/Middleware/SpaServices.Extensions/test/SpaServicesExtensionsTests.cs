// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.AspNetCore.SpaServices.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SpaServices.Extensions.Tests
{
    public class SpaServicesExtensionsTests
    {
        [Fact]
        public void UseSpa_ThrowsInvalidOperationException_IfRootpathNotSet()
        {
            // Arrange
            var applicationbuilder = GetApplicationBuilder(GetServiceProvider());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => applicationbuilder.UseSpa(rb => { }));

            Assert.Equal("No RootPath was set on the SpaStaticFilesOptions.", exception.Message);
        }

        [Fact]
        public async Task UseSpa_KillsRds_WhenAppIsStopped()
        {
            var serviceProvider = GetServiceProvider(s => s.RootPath = "/");
            var applicationbuilder = new ApplicationBuilder(serviceProvider);
            var applicationLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
            var diagnosticListener = serviceProvider.GetRequiredService<DiagnosticListener>();
            var listener = new NpmStartedDiagnosticListener();
            diagnosticListener.SubscribeWithAdapter(listener);

            applicationbuilder.UseSpa(b =>
            {
                b.Options.SourcePath = Directory.GetCurrentDirectory();
                b.UseReactDevelopmentServer(GetPlatformSpecificWaitCommand());
            });

            await Assert_NpmKilled_WhenAppIsStopped(applicationLifetime, listener);
        }

        [Fact]
        public async Task UseSpa_KillsAngularCli_WhenAppIsStopped()
        {
            var serviceProvider = GetServiceProvider(s => s.RootPath = "/");
            var applicationbuilder = new ApplicationBuilder(serviceProvider);
            var applicationLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
            var diagnosticListener = serviceProvider.GetRequiredService<DiagnosticListener>();
            var listener = new NpmStartedDiagnosticListener();
            diagnosticListener.SubscribeWithAdapter(listener);

            applicationbuilder.UseSpa(b =>
            {
                b.Options.SourcePath = Directory.GetCurrentDirectory();
                b.UseAngularCliServer(GetPlatformSpecificWaitCommand());
            });

            await Assert_NpmKilled_WhenAppIsStopped(applicationLifetime, listener);
        }

        private async Task Assert_NpmKilled_WhenAppIsStopped(IHostApplicationLifetime applicationLifetime, NpmStartedDiagnosticListener listener)
        {
            // Give node a moment to start up
            await Task.WhenAny(listener.NpmStarted, Task.Delay(TimeSpan.FromSeconds(30)));

            Process npmProcess = null;
            var npmExitEvent = new ManualResetEventSlim();
            if (listener.NpmStarted.IsCompleted)
            {
                npmProcess = listener.NpmStarted.Result.Process;
                Assert.False(npmProcess.HasExited);
                npmProcess.Exited += (_, __) => npmExitEvent.Set();
            }

            // Act
            applicationLifetime.StopApplication();

            // Assert
            AssertNoErrors();
            Assert.True(listener.NpmStarted.IsCompleted, "npm wasn't launched");

            npmExitEvent.Wait(TimeSpan.FromSeconds(30));
            Assert.True(npmProcess.HasExited, "npm wasn't killed");
        }

        private class NpmStartedDiagnosticListener
        {
            private readonly TaskCompletionSource<(ProcessStartInfo ProcessStartInfo, Process Process)> _npmStartedTaskCompletionSource
                = new TaskCompletionSource<(ProcessStartInfo ProcessStartInfo, Process Process)>();

            public Task<(ProcessStartInfo ProcessStartInfo, Process Process)> NpmStarted
                => _npmStartedTaskCompletionSource.Task;

            [DiagnosticName("Microsoft.AspNetCore.NodeServices.Npm.NpmStarted")]
            public virtual void OnNpmStarted(ProcessStartInfo processStartInfo, Process process)
            {
                _npmStartedTaskCompletionSource.TrySetResult((processStartInfo, process));
            }
        }

        private string GetPlatformSpecificWaitCommand()
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "waitWindows" : "wait";

        private IApplicationBuilder GetApplicationBuilder(IServiceProvider serviceProvider = null)
        {
            if (serviceProvider == null)
            {
                serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict).Object;
            }

            var applicationbuilderMock = new Mock<IApplicationBuilder>();
            applicationbuilderMock
                .Setup(s => s.ApplicationServices)
                .Returns(serviceProvider);

            return applicationbuilderMock.Object;
        }

        private IServiceProvider GetServiceProvider(Action<SpaStaticFilesOptions> configuration = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSpaStaticFiles(configuration);
            services.AddSingleton<ILoggerFactory>(ListLoggerFactory);
            services.AddSingleton(typeof(IHostApplicationLifetime), new TestHostApplicationLifetime());
            services.AddSingleton(typeof(IWebHostEnvironment), new TestWebHostEnvironment());

            var listener = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton(listener);
            services.AddSingleton<DiagnosticSource>(listener);

            return services.BuildServiceProvider();
        }

        private void AssertNoErrors()
        {
            var builder = new StringBuilder();
            foreach (var line in ListLoggerFactory.Log)
            {
                if (line.Level < LogLevel.Error)
                {
                    continue;
                }
                builder.AppendLine(line.Message);
            }

            Assert.True(builder.Length == 0, builder.ToString());
        }

        private ListLoggerFactory ListLoggerFactory { get; } = new ListLoggerFactory(c => c == "Microsoft.AspNetCore.SpaServices");

        private class TestHostApplicationLifetime : IHostApplicationLifetime
        {
            CancellationTokenSource _applicationStoppingSource;
            CancellationTokenSource _applicationStoppedSource;

            public TestHostApplicationLifetime()
            {
                _applicationStoppingSource = new CancellationTokenSource();
                ApplicationStopping = _applicationStoppingSource.Token;

                _applicationStoppedSource = new CancellationTokenSource();
                ApplicationStopped = _applicationStoppedSource.Token;
            }

            public CancellationToken ApplicationStarted => CancellationToken.None;

            public CancellationToken ApplicationStopping { get; }

            public CancellationToken ApplicationStopped { get; }

            public void StopApplication()
            {
                _applicationStoppingSource.Cancel();
                _applicationStoppedSource.Cancel();
            }
        }

        private class TestWebHostEnvironment : IWebHostEnvironment
        {
            public string EnvironmentName { get; set; }
            public string ApplicationName { get; set; }
            public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
            public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
            public IFileProvider ContentRootFileProvider { get; set; }
            public IFileProvider WebRootFileProvider { get; set; }
        }
    }
}
