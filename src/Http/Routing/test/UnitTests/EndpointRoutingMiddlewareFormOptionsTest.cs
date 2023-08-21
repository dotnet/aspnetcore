// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Routing;

public class EndpointRoutingMiddlewareFormOptionsTest
{
    [Fact]
    public async Task LogsWhenFormOptionsAreNotApplied()
    {
        var sink = new TestSink(TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);

        var httpContext = CreateHttpContext();
        var presetFormFeature = new FormFeature(httpContext.Request, FormOptions.Default);
        presetFormFeature.Form = new FormCollection(new());
        httpContext.Features.Set<IFormFeature>(presetFormFeature);

        var formOptionsMetadata = new FormOptionsMetadata(bufferBody: false, valueCountLimit: 54);
        var middleware = CreateMiddleware(
            logger: logger,
            matcherFactory: new TestMatcherFactory(isHandled: true, setEndpointCallback: c =>
            {
                c.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(formOptionsMetadata), "myapp"));
            }));

        // Act
        await middleware.Invoke(httpContext);

        var formFeature = httpContext.Features.Get<IFormFeature>();
        var formOptions = Assert.IsType<FormFeature>(formFeature).FormOptions;
        // Configured values are set
        Assert.False(formOptions.BufferBody);
        Assert.Equal(FormReader.DefaultValueCountLimit, formOptions.ValueCountLimit);
        // Unset values are set to default instead of null
        Assert.Equal(FormOptions.DefaultMemoryBufferThreshold, formOptions.MemoryBufferThreshold);

        // Logs that FormOptions were not applied
        var write = Assert.Single(sink.Writes.Where(w => w.EventId.Name == "CannotApplyFormOptions"));
        Assert.Equal("Unable to apply configured form options since the request form has already been read.", write.Message);
    }

    [Fact]
    public async Task SupportsSettingFormOptionsFromMetadata()
    {
        var sink = new TestSink(TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);

        var httpContext = CreateHttpContext();

        var formOptionsMetadata = new FormOptionsMetadata(bufferBody: false, valueCountLimit: 54);
        var middleware = CreateMiddleware(
            logger: logger,
            matcherFactory: new TestMatcherFactory(isHandled: true, setEndpointCallback: c =>
            {
                c.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(formOptionsMetadata), "myapp"));
            }));

        // Act
        await middleware.Invoke(httpContext);

        var formFeature = httpContext.Features.Get<IFormFeature>();
        var formOptions = Assert.IsType<FormFeature>(formFeature).FormOptions;
        // Configured values are set
        Assert.False(formOptions.BufferBody);
        Assert.Equal(54, formOptions.ValueCountLimit);
        // Unset values are set to default instead of null
        Assert.Equal(FormOptions.DefaultMemoryBufferThreshold, formOptions.MemoryBufferThreshold);

        // Logs that FormOptions were applied
        var write = Assert.Single(sink.Writes.Where(w => w.EventId.Name == "AppliedFormOptions"));
        Assert.Equal("Applied the configured form options on the current request.", write.Message);
    }

    [Fact]
    public async Task SupportsMergingSettingsFromMultipleFormOptionsMetadata()
    {
        var sink = new TestSink(TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);

        var httpContext = CreateHttpContext();

        var formOptionsMetadata1 = new FormOptionsMetadata(bufferBody: false, valueCountLimit: 54);
        var formOptionsMetadata2 = new FormOptionsMetadata(valueLengthLimit: 45);
        var formOptionsMetadata3 = new FormOptionsMetadata(bufferBody: true);
        var middleware = CreateMiddleware(
            logger: logger,
            matcherFactory: new TestMatcherFactory(isHandled: true, setEndpointCallback: c =>
            {
                c.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(formOptionsMetadata1, formOptionsMetadata2, formOptionsMetadata3), "myapp"));
            }));

        // Act
        await middleware.Invoke(httpContext);

        var formFeature = httpContext.Features.Get<IFormFeature>();
        var formOptions = Assert.IsType<FormFeature>(formFeature).FormOptions;
        // Favor the most specific (last) value set for a property
        Assert.True(formOptions.BufferBody);
        Assert.Equal(54, formOptions.ValueCountLimit);
        Assert.Equal(45, formOptions.ValueLengthLimit);
        // Unset values are set to default instead of null
        Assert.Equal(FormOptions.DefaultMemoryBufferThreshold, formOptions.MemoryBufferThreshold);

        // Logs that FormOptions were applied
        var write = Assert.Single(sink.Writes.Where(w => w.EventId.Name == "AppliedFormOptions"));
        Assert.Equal("Applied the configured form options on the current request.", write.Message);
    }

    [Fact]
    public async Task SupportsMergingSettingsFromMetadataAndServices()
    {
        var sink = new TestSink(TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);
        var httpContext = CreateHttpContext();

        var formOptionsMetadata = new FormOptionsMetadata(bufferBody: false, valueCountLimit: 54);
        var middleware = CreateMiddleware(
            logger: logger,
            matcherFactory: new TestMatcherFactory(isHandled: true, setEndpointCallback: c =>
            {
                c.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(formOptionsMetadata), "myapp"));
            }),
            formOptions: new FormOptions { BufferBody = true, ValueLengthLimit = 45});

        // Act
        await middleware.Invoke(httpContext);

        var formFeature = httpContext.Features.Get<IFormFeature>();
        var formOptions = Assert.IsType<FormFeature>(formFeature).FormOptions;
        // Favor the most specific (last) value set for a property
        Assert.False(formOptions.BufferBody);
        Assert.Equal(54, formOptions.ValueCountLimit);
        Assert.Equal(45, formOptions.ValueLengthLimit);
        // Unset values are set to default instead of null
        Assert.Equal(FormOptions.DefaultMemoryBufferThreshold, formOptions.MemoryBufferThreshold);
    }

    [Fact]
    public async Task SettingEndpointManuallyDoesNotOverwriteOptions()
    {
        var sink = new TestSink(TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);
        var httpContext = CreateHttpContext();
        var endpointMetadata = new FormOptionsMetadata(bufferBody: true, valueCountLimit: 70);
        var endpoint = new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(endpointMetadata), "myapp");

        var formOptionsMetadata = new FormOptionsMetadata(bufferBody: false, valueCountLimit: 54);
        var middleware = CreateMiddleware(
            logger: logger,
            matcherFactory: new TestMatcherFactory(isHandled: true, setEndpointCallback: c =>
            {
                c.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(formOptionsMetadata), "myapp"));
            }));

        // Act
        await middleware.Invoke(httpContext);
        httpContext.SetEndpoint(endpoint);

        var formFeature = httpContext.Features.Get<IFormFeature>();
        var formOptions = Assert.IsType<FormFeature>(formFeature).FormOptions;
        // Favor the most specific (last) value set for a property
        Assert.False(formOptions.BufferBody);
        Assert.Equal(54, formOptions.ValueCountLimit);
        // Unset values are set to default instead of null
        Assert.Equal(FormOptions.DefaultMemoryBufferThreshold, formOptions.MemoryBufferThreshold);
    }

    [Fact]
    public async Task OptionsNotSetForNonFormRequests()
    {
        var sink = new TestSink(TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);
        var httpContext = new DefaultHttpContext();
        var endpointMetadata = new FormOptionsMetadata(bufferBody: true, valueCountLimit: 70);
        var endpoint = new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(endpointMetadata), "myapp");

        var formOptionsMetadata = new FormOptionsMetadata(bufferBody: false, valueCountLimit: 54);
        var middleware = CreateMiddleware(
            logger: logger,
            matcherFactory: new TestMatcherFactory(isHandled: true, setEndpointCallback: c =>
            {
                c.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(formOptionsMetadata), "myapp"));
            }));

        // Act
        await middleware.Invoke(httpContext);
        httpContext.SetEndpoint(endpoint);

        var formFeature = httpContext.Features.Get<IFormFeature>();
        Assert.Null(formFeature);
    }

    private HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new TestServiceProvider(),
            Request =
            {
                ContentType = "multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq",
                Method = "POST",
            }
        };

        return httpContext;
    }

    private EndpointRoutingMiddleware CreateMiddleware(
        ILogger<EndpointRoutingMiddleware> logger = null,
        MatcherFactory matcherFactory = null,
        DiagnosticListener listener = null,
        RequestDelegate next = null,
        FormOptions formOptions = null)
    {
        next ??= c => Task.CompletedTask;
        logger ??= new Logger<EndpointRoutingMiddleware>(NullLoggerFactory.Instance);
        matcherFactory ??= new TestMatcherFactory(true);
        listener ??= new DiagnosticListener("Microsoft.AspNetCore");
        var metrics = new RoutingMetrics(new TestMeterFactory());

        var middleware = new EndpointRoutingMiddleware(
            matcherFactory,
            logger,
            new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>()),
            new DefaultEndpointDataSource(),
            listener,
            Options.Create(new RouteOptions()),
            Options.Create(formOptions ?? new FormOptions()),
            metrics,
            next);

        return middleware;
    }
}
