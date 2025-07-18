// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class SSRRenderModeBoundaryTest
{
    // While most aspects of SSRRenderModeBoundary are only interesting to test E2E,
    // the configuration validation aspect is better covered as unit tests because
    // otherwise we would need many different E2E test app configurations.

    [Fact]
    public void DoesNotAssertAboutConfiguredRenderModesOnUnknownEndpoints()
    {
        // Arrange: an endpoint with no ConfiguredRenderModesMetadata
        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(), null));

        // Act/Assert: no exception means we're OK
        new SSRRenderModeBoundary(httpContext, typeof(TestComponent), new InteractiveServerRenderMode());
        new SSRRenderModeBoundary(httpContext, typeof(TestComponent), new InteractiveWebAssemblyRenderMode());
        new SSRRenderModeBoundary(httpContext, typeof(TestComponent), new InteractiveAutoRenderMode());
    }

    [Fact]
    public void ThrowsIfServerRenderModeUsedAndNotConfigured()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        PrepareEndpoint(httpContext, new WebAssemblyRenderModeSubclass());

        // Act/Assert
        var ex = Assert.Throws<InvalidOperationException>(() => new SSRRenderModeBoundary(
            httpContext, typeof(TestComponent), new ServerRenderModeSubclass()));
        Assert.Contains($"A component of type '{typeof(TestComponent)}' has render mode '{nameof(ServerRenderModeSubclass)}'", ex.Message);
        Assert.Contains($"add a call to 'AddInteractiveServerRenderMode'", ex.Message);
    }

    [Fact]
    public void ThrowsIfWebAssemblyRenderModeUsedAndNotConfigured()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        PrepareEndpoint(httpContext, new ServerRenderModeSubclass());

        // Act/Assert
        var ex = Assert.Throws<InvalidOperationException>(() => new SSRRenderModeBoundary(
            httpContext, typeof(TestComponent), new WebAssemblyRenderModeSubclass()));
        Assert.Contains($"A component of type '{typeof(TestComponent)}' has render mode '{nameof(WebAssemblyRenderModeSubclass)}'", ex.Message);
        Assert.Contains($"add a call to 'AddInteractiveWebAssemblyRenderMode'", ex.Message);
    }

    [Fact]
    public void ThrowsIfAutoRenderModeUsedAndServerNotConfigured()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        PrepareEndpoint(httpContext, new WebAssemblyRenderModeSubclass());

        // Act/Assert
        var ex = Assert.Throws<InvalidOperationException>(() => new SSRRenderModeBoundary(
            httpContext, typeof(TestComponent), new AutoRenderModeSubclass()));
        Assert.Contains($"A component of type '{typeof(TestComponent)}' has render mode '{nameof(AutoRenderModeSubclass)}'", ex.Message);
        Assert.Contains($"add a call to 'AddInteractiveServerRenderMode'", ex.Message);
    }

    [Fact]
    public void ThrowsIfAutoRenderModeUsedAndWebAssemblyNotConfigured()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        PrepareEndpoint(httpContext, new ServerRenderModeSubclass());

        // Act/Assert
        var ex = Assert.Throws<InvalidOperationException>(() => new SSRRenderModeBoundary(
            httpContext, typeof(TestComponent), new AutoRenderModeSubclass()));
        Assert.Contains($"A component of type '{typeof(TestComponent)}' has render mode '{nameof(AutoRenderModeSubclass)}'", ex.Message);
        Assert.Contains($"add a call to 'AddInteractiveWebAssemblyRenderMode'", ex.Message);
    }

    private static void PrepareEndpoint(HttpContext httpContext, params IComponentRenderMode[] configuredModes)
    {
        httpContext.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(
            new ConfiguredRenderModesMetadata(configuredModes)), null));
    }

    class TestComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
            => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters)
            => throw new NotImplementedException();
    }

    [Fact]
    public void GetComponentMarkerKey_WorksWithStringKey()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var boundary = new SSRRenderModeBoundary(httpContext, typeof(TestComponent), new InteractiveServerRenderMode());
        var stringKey = "test-string-key";

        // Act
        var markerKey = boundary.GetComponentMarkerKey(1, stringKey);

        // Assert
        Assert.Equal(stringKey, markerKey.FormattedComponentKey);
        Assert.NotEmpty(markerKey.LocationHash);
    }

    [Fact]
    public void GetComponentMarkerKey_WorksWithIntKey()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var boundary = new SSRRenderModeBoundary(httpContext, typeof(TestComponent), new InteractiveServerRenderMode());
        var intKey = 42;

        // Act
        var markerKey = boundary.GetComponentMarkerKey(1, intKey);

        // Assert
        Assert.Equal("42", markerKey.FormattedComponentKey);
        Assert.NotEmpty(markerKey.LocationHash);
    }

    [Fact]
    public void GetComponentMarkerKey_WorksWithGuidKey()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var boundary = new SSRRenderModeBoundary(httpContext, typeof(TestComponent), new InteractiveServerRenderMode());
        var guidKey = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        var markerKey = boundary.GetComponentMarkerKey(1, guidKey);

        // Assert
        Assert.Equal("12345678-1234-1234-1234-123456789012", markerKey.FormattedComponentKey);
        Assert.NotEmpty(markerKey.LocationHash);
    }

    [Fact]
    public void GetComponentMarkerKey_WorksWithDoubleKey()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var boundary = new SSRRenderModeBoundary(httpContext, typeof(TestComponent), new InteractiveServerRenderMode());
        var doubleKey = 123.45;

        // Act
        var markerKey = boundary.GetComponentMarkerKey(1, doubleKey);

        // Assert
        Assert.Equal("123.45", markerKey.FormattedComponentKey);
        Assert.NotEmpty(markerKey.LocationHash);
    }

    [Fact]
    public void GetComponentMarkerKey_WorksWithDateTimeKey()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var boundary = new SSRRenderModeBoundary(httpContext, typeof(TestComponent), new InteractiveServerRenderMode());
        var dateTimeKey = new DateTime(2023, 12, 25, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var markerKey = boundary.GetComponentMarkerKey(1, dateTimeKey);

        // Assert
        Assert.Equal("12/25/2023 10:30:00", markerKey.FormattedComponentKey);
        Assert.NotEmpty(markerKey.LocationHash);
    }

    [Fact]
    public void GetComponentMarkerKey_WorksWithNullKey()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var boundary = new SSRRenderModeBoundary(httpContext, typeof(TestComponent), new InteractiveServerRenderMode());

        // Act
        var markerKey = boundary.GetComponentMarkerKey(1, null);

        // Assert
        Assert.Equal(string.Empty, markerKey.FormattedComponentKey);
        Assert.NotEmpty(markerKey.LocationHash);
    }

    [Fact]
    public void GetComponentMarkerKey_WorksWithNonFormattableKey()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var boundary = new SSRRenderModeBoundary(httpContext, typeof(TestComponent), new InteractiveServerRenderMode());
        var nonFormattableKey = new object();

        // Act
        var markerKey = boundary.GetComponentMarkerKey(1, nonFormattableKey);

        // Assert
        Assert.Equal(string.Empty, markerKey.FormattedComponentKey);
        Assert.NotEmpty(markerKey.LocationHash);
    }

    class ServerRenderModeSubclass : InteractiveServerRenderMode { }
    class WebAssemblyRenderModeSubclass : InteractiveWebAssemblyRenderMode { }
    class AutoRenderModeSubclass : InteractiveAutoRenderMode { }
}
