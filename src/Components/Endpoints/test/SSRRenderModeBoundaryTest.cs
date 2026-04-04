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

    public static IEnumerable<object[]> ComponentKeyTestData()
    {
        yield return new object[] { "test-string-key", "test-string-key" };
        yield return new object[] { 42, "42" };
        yield return new object[] { Guid.Parse("12345678-1234-1234-1234-123456789012"), "12345678-1234-1234-1234-123456789012" };
        yield return new object[] { 123.45, "123.45" };
        yield return new object[] { new DateTime(2023, 12, 25, 10, 30, 0, DateTimeKind.Utc), "12/25/2023 10:30:00" };
        yield return new object[] { null, string.Empty };
        yield return new object[] { new object(), string.Empty };
    }

    [Theory]
    [MemberData(nameof(ComponentKeyTestData))]
    public void GetComponentMarkerKey_WorksWithVariousKeyTypes(object componentKey, string expectedFormattedKey)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var boundary = new SSRRenderModeBoundary(httpContext, typeof(TestComponent), new InteractiveServerRenderMode());

        // Act
        var markerKey = boundary.GetComponentMarkerKey(1, componentKey);

        // Assert
        Assert.Equal(expectedFormattedKey, markerKey.FormattedComponentKey);
        Assert.NotEmpty(markerKey.LocationHash);
    }

    class ServerRenderModeSubclass : InteractiveServerRenderMode { }
    class WebAssemblyRenderModeSubclass : InteractiveWebAssemblyRenderMode { }
    class AutoRenderModeSubclass : InteractiveAutoRenderMode { }
}
