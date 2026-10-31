// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.AspNetCore.SpaServices.StaticFiles;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SpaServices.Extensions.Tests;

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
