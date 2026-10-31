// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Moq;

namespace Microsoft.AspNetCore.Authentication;

public class AuthenticationMetricsTest
{
    [Fact]
    public async Task Authenticate_Success()
    {
        // Arrange
        var authenticationHandler = new Mock<IAuthenticationHandler>();
        authenticationHandler.Setup(h => h.AuthenticateAsync()).Returns(Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "custom"))));

        var meterFactory = new TestMeterFactory();
        var httpContext = new DefaultHttpContext();
        var authenticationService = CreateAuthenticationService(authenticationHandler.Object, meterFactory);
        var meter = meterFactory.Meters.Single();

        using var authenticationRequestsCollector = new MetricCollector<double>(meterFactory, AuthenticationMetrics.MeterName, "aspnetcore.authentication.authenticate.duration");

        // Act
        await authenticationService.AuthenticateAsync(httpContext, scheme: "custom");

        // Assert
        Assert.Equal(AuthenticationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(authenticationRequestsCollector.GetMeasurementSnapshot());
        Assert.True(measurement.Value > 0);
        Assert.Equal("custom", (string)measurement.Tags["aspnetcore.authentication.scheme"]);
        Assert.Equal("success", (string)measurement.Tags["aspnetcore.authentication.result"]);
        Assert.False(measurement.Tags.ContainsKey("error.type"));
    }

    [Fact]
    public async Task Authenticate_Failure()
    {
        // Arrange
        var authenticationHandler = new Mock<IAuthenticationHandler>();
        authenticationHandler.Setup(h => h.AuthenticateAsync()).Returns(Task.FromResult(AuthenticateResult.Fail("Authentication failed")));

        var meterFactory = new TestMeterFactory();
        var httpContext = new DefaultHttpContext();
        var authenticationService = CreateAuthenticationService(authenticationHandler.Object, meterFactory);
        var meter = meterFactory.Meters.Single();

        using var authenticationRequestsCollector = new MetricCollector<double>(meterFactory, AuthenticationMetrics.MeterName, "aspnetcore.authentication.authenticate.duration");

        // Act
        await authenticationService.AuthenticateAsync(httpContext, scheme: "custom");

        // Assert
        Assert.Equal(AuthenticationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(authenticationRequestsCollector.GetMeasurementSnapshot());
        Assert.True(measurement.Value > 0);
        Assert.Equal("custom", (string)measurement.Tags["aspnetcore.authentication.scheme"]);
        Assert.Equal("failure", (string)measurement.Tags["aspnetcore.authentication.result"]);
        Assert.Equal("Microsoft.AspNetCore.Authentication.AuthenticationFailureException", (string)measurement.Tags["error.type"]);
    }

    [Fact]
    public async Task Authenticate_NoResult()
    {
        // Arrange
        var authenticationHandler = new Mock<IAuthenticationHandler>();
        authenticationHandler.Setup(h => h.AuthenticateAsync()).Returns(Task.FromResult(AuthenticateResult.NoResult()));

        var meterFactory = new TestMeterFactory();
        var httpContext = new DefaultHttpContext();
        var authenticationService = CreateAuthenticationService(authenticationHandler.Object, meterFactory);
        var meter = meterFactory.Meters.Single();

        using var authenticationRequestsCollector = new MetricCollector<double>(meterFactory, AuthenticationMetrics.MeterName, "aspnetcore.authentication.authenticate.duration");

        // Act
        await authenticationService.AuthenticateAsync(httpContext, scheme: "custom");

        // Assert
        Assert.Equal(AuthenticationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(authenticationRequestsCollector.GetMeasurementSnapshot());
        Assert.True(measurement.Value > 0);
        Assert.Equal("custom", (string)measurement.Tags["aspnetcore.authentication.scheme"]);
        Assert.Equal("none", (string)measurement.Tags["aspnetcore.authentication.result"]);
        Assert.False(measurement.Tags.ContainsKey("error.type"));
    }

    [Fact]
    public async Task Authenticate_ExceptionThrownInHandler()
    {
        // Arrange
        var authenticationHandler = new Mock<IAuthenticationHandler>();
        authenticationHandler.Setup(h => h.AuthenticateAsync()).Throws(new InvalidOperationException("An error occurred during authentication"));

        var meterFactory = new TestMeterFactory();
        var httpContext = new DefaultHttpContext();
        var authenticationService = CreateAuthenticationService(authenticationHandler.Object, meterFactory);
        var meter = meterFactory.Meters.Single();

        using var authenticationRequestsCollector = new MetricCollector<double>(meterFactory, AuthenticationMetrics.MeterName, "aspnetcore.authentication.authenticate.duration");

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => authenticationService.AuthenticateAsync(httpContext, scheme: "custom"));

        // Assert
        Assert.Equal("An error occurred during authentication", ex.Message);
        Assert.Equal(AuthenticationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(authenticationRequestsCollector.GetMeasurementSnapshot());
        Assert.True(measurement.Value > 0);
        Assert.Equal("custom", (string)measurement.Tags["aspnetcore.authentication.scheme"]);
        Assert.Equal("System.InvalidOperationException", (string)measurement.Tags["error.type"]);
        Assert.False(measurement.Tags.ContainsKey("aspnetcore.authentication.result"));
    }

    [Fact]
    public async Task Challenge()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var httpContext = new DefaultHttpContext();
        var authenticationService = CreateAuthenticationService(Mock.Of<IAuthenticationHandler>(), meterFactory);
        var meter = meterFactory.Meters.Single();

        using var challengesCollector = new MetricCollector<long>(meterFactory, AuthenticationMetrics.MeterName, "aspnetcore.authentication.challenges");

        // Act
        await authenticationService.ChallengeAsync(httpContext, scheme: "custom", properties: null);

        // Assert
        Assert.Equal(AuthenticationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(challengesCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("custom", (string)measurement.Tags["aspnetcore.authentication.scheme"]);
    }

    [Fact]
    public async Task Challenge_ExceptionThrownInHandler()
    {
        // Arrange
        var authenticationHandler = new Mock<IAuthenticationHandler>();
        authenticationHandler.Setup(h => h.ChallengeAsync(It.IsAny<AuthenticationProperties>())).Throws(new InvalidOperationException("An error occurred during challenge"));

        var meterFactory = new TestMeterFactory();
        var httpContext = new DefaultHttpContext();
        var authenticationService = CreateAuthenticationService(authenticationHandler.Object, meterFactory);
        var meter = meterFactory.Meters.Single();

        using var challengesCollector = new MetricCollector<long>(meterFactory, AuthenticationMetrics.MeterName, "aspnetcore.authentication.challenges");

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => authenticationService.ChallengeAsync(httpContext, scheme: "custom", properties: null));

        // Assert
        Assert.Equal("An error occurred during challenge", ex.Message);
        Assert.Equal(AuthenticationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(challengesCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("custom", (string)measurement.Tags["aspnetcore.authentication.scheme"]);
        Assert.Equal("System.InvalidOperationException", (string)measurement.Tags["error.type"]);
    }

    [Fact]
    public async Task Forbid()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var httpContext = new DefaultHttpContext();
        var authenticationService = CreateAuthenticationService(Mock.Of<IAuthenticationHandler>(), meterFactory);
        var meter = meterFactory.Meters.Single();

        using var forbidsCollector = new MetricCollector<long>(meterFactory, AuthenticationMetrics.MeterName, "aspnetcore.authentication.forbids");

        // Act
        await authenticationService.ForbidAsync(httpContext, scheme: "custom", properties: null);

        // Assert
        Assert.Equal(AuthenticationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(forbidsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("custom", (string)measurement.Tags["aspnetcore.authentication.scheme"]);
    }

    [Fact]
    public async Task Forbid_ExceptionThrownInHandler()
    {
        // Arrange
        var authenticationHandler = new Mock<IAuthenticationHandler>();
        authenticationHandler.Setup(h => h.ForbidAsync(It.IsAny<AuthenticationProperties>())).Throws(new InvalidOperationException("An error occurred during forbid"));

        var meterFactory = new TestMeterFactory();
        var httpContext = new DefaultHttpContext();
        var authenticationService = CreateAuthenticationService(authenticationHandler.Object, meterFactory);
        var meter = meterFactory.Meters.Single();

        using var forbidsCollector = new MetricCollector<long>(meterFactory, AuthenticationMetrics.MeterName, "aspnetcore.authentication.forbids");

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => authenticationService.ForbidAsync(httpContext, scheme: "custom", properties: null));

        // Assert
        Assert.Equal("An error occurred during forbid", ex.Message);
        Assert.Equal(AuthenticationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(forbidsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("custom", (string)measurement.Tags["aspnetcore.authentication.scheme"]);
        Assert.Equal("System.InvalidOperationException", (string)measurement.Tags["error.type"]);
    }

    [Fact]
    public async Task SignIn()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var httpContext = new DefaultHttpContext();
        var authenticationService = CreateAuthenticationService(Mock.Of<IAuthenticationSignInHandler>(), meterFactory);
        var meter = meterFactory.Meters.Single();

        using var signInsCollector = new MetricCollector<long>(meterFactory, AuthenticationMetrics.MeterName, "aspnetcore.authentication.sign_ins");

        // Act
        await authenticationService.SignInAsync(httpContext, scheme: "custom", new ClaimsPrincipal(), properties: null);

        // Assert
        Assert.Equal(AuthenticationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(signInsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("custom", (string)measurement.Tags["aspnetcore.authentication.scheme"]);
    }

    [Fact]
    public async Task SignIn_ExceptionThrownInHandler()
    {
        // Arrange
        var authenticationHandler = new Mock<IAuthenticationSignInHandler>();
        authenticationHandler.Setup(h => h.SignInAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>())).Throws(new InvalidOperationException("An error occurred during sign in"));

        var meterFactory = new TestMeterFactory();
        var httpContext = new DefaultHttpContext();
        var authenticationService = CreateAuthenticationService(authenticationHandler.Object, meterFactory);
        var meter = meterFactory.Meters.Single();

        using var signInsCollector = new MetricCollector<long>(meterFactory, AuthenticationMetrics.MeterName, "aspnetcore.authentication.sign_ins");

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => authenticationService.SignInAsync(httpContext, scheme: "custom", new ClaimsPrincipal(), properties: null));

        // Assert
        Assert.Equal("An error occurred during sign in", ex.Message);
        Assert.Equal(AuthenticationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(signInsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("custom", (string)measurement.Tags["aspnetcore.authentication.scheme"]);
        Assert.Equal("System.InvalidOperationException", (string)measurement.Tags["error.type"]);
    }

    [Fact]
    public async Task SignOut()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var meterFactory = new TestMeterFactory();
        var authenticationService = CreateAuthenticationService(Mock.Of<IAuthenticationSignOutHandler>(), meterFactory);
        var meter = meterFactory.Meters.Single();

        using var signOutsCollector = new MetricCollector<long>(meterFactory, AuthenticationMetrics.MeterName, "aspnetcore.authentication.sign_outs");

        // Act
        await authenticationService.SignOutAsync(httpContext, scheme: "custom", properties: null);

        // Assert
        Assert.Equal(AuthenticationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(signOutsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("custom", (string)measurement.Tags["aspnetcore.authentication.scheme"]);
    }

    [Fact]
    public async Task SignOut_ExceptionThrownInHandler()
    {
        // Arrange
        var authenticationHandler = new Mock<IAuthenticationSignOutHandler>();
        authenticationHandler.Setup(h => h.SignOutAsync(It.IsAny<AuthenticationProperties>())).Throws(new InvalidOperationException("An error occurred during sign out"));

        var httpContext = new DefaultHttpContext();
        var meterFactory = new TestMeterFactory();
        var authenticationService = CreateAuthenticationService(authenticationHandler.Object, meterFactory);
        var meter = meterFactory.Meters.Single();

        using var signOutsCollector = new MetricCollector<long>(meterFactory, AuthenticationMetrics.MeterName, "aspnetcore.authentication.sign_outs");

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => authenticationService.SignOutAsync(httpContext, scheme: "custom", properties: null));

        // Assert
        Assert.Equal("An error occurred during sign out", ex.Message);
        Assert.Equal(AuthenticationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(signOutsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("custom", (string)measurement.Tags["aspnetcore.authentication.scheme"]);
        Assert.Equal("System.InvalidOperationException", (string)measurement.Tags["error.type"]);
    }

    private static AuthenticationServiceImpl CreateAuthenticationService(IAuthenticationHandler authenticationHandler, TestMeterFactory meterFactory)
    {
        var authenticationHandlerProvider = new Mock<IAuthenticationHandlerProvider>();
        authenticationHandlerProvider.Setup(p => p.GetHandlerAsync(It.IsAny<HttpContext>(), "custom")).Returns(Task.FromResult(authenticationHandler));

        var claimsTransform = new Mock<IClaimsTransformation>();
        claimsTransform.Setup(t => t.TransformAsync(It.IsAny<ClaimsPrincipal>())).Returns((ClaimsPrincipal p) => Task.FromResult(p));

        var options = Options.Create(new AuthenticationOptions
        {
            DefaultSignInScheme = "custom",
            RequireAuthenticatedSignIn = false,
        });

        var metrics = new AuthenticationMetrics(meterFactory);
        var authenticationService = new AuthenticationServiceImpl(
            Mock.Of<IAuthenticationSchemeProvider>(),
            authenticationHandlerProvider.Object,
            claimsTransform.Object,
            options,
            metrics);

        return authenticationService;
    }
}
