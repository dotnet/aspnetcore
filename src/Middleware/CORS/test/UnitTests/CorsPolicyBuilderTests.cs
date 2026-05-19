// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cors.Infrastructure;

public class CorsPolicyBuilderTests
{
    [Fact]
    public void Constructor_WithPolicy_AddsTheGivenPolicy()
    {
        // Arrange
        Func<string, bool> isOriginAllowed = origin => true;
        var originalPolicy = new CorsPolicy();
        originalPolicy.Origins.Add("http://existing.com");
        originalPolicy.Headers.Add("Existing");
        originalPolicy.Methods.Add("GET");
        originalPolicy.ExposedHeaders.Add("ExistingExposed");
        originalPolicy.SupportsCredentials = true;
        originalPolicy.PreflightMaxAge = TimeSpan.FromSeconds(12);
        originalPolicy.IsOriginAllowed = isOriginAllowed;

        // Act
        var builder = new CorsPolicyBuilder(originalPolicy);

        // Assert
        var corsPolicy = builder.Build();

        Assert.False(corsPolicy.AllowAnyHeader);
        Assert.False(corsPolicy.AllowAnyMethod);
        Assert.False(corsPolicy.AllowAnyOrigin);
        Assert.True(corsPolicy.SupportsCredentials);
        Assert.NotSame(originalPolicy.Headers, corsPolicy.Headers);
        Assert.Equal(originalPolicy.Headers, corsPolicy.Headers);
        Assert.NotSame(originalPolicy.Methods, corsPolicy.Methods);
        Assert.Equal(originalPolicy.Methods, corsPolicy.Methods);
        Assert.NotSame(originalPolicy.Origins, corsPolicy.Origins);
        Assert.Equal(originalPolicy.Origins, corsPolicy.Origins);
        Assert.NotSame(originalPolicy.ExposedHeaders, corsPolicy.ExposedHeaders);
        Assert.Equal(originalPolicy.ExposedHeaders, corsPolicy.ExposedHeaders);
        Assert.Equal(TimeSpan.FromSeconds(12), corsPolicy.PreflightMaxAge);
        Assert.Same(originalPolicy.IsOriginAllowed, corsPolicy.IsOriginAllowed);
    }

    [Fact]
    public void ConstructorWithPolicy_HavingNullPreflightMaxAge_AddsTheGivenPolicy()
    {
        // Arrange
        var originalPolicy = new CorsPolicy();
        originalPolicy.Origins.Add("http://existing.com");

        // Act
        var builder = new CorsPolicyBuilder(originalPolicy);

        // Assert
        var corsPolicy = builder.Build();

        Assert.Null(corsPolicy.PreflightMaxAge);
        Assert.False(corsPolicy.AllowAnyHeader);
        Assert.False(corsPolicy.AllowAnyMethod);
        Assert.False(corsPolicy.AllowAnyOrigin);
        Assert.NotSame(originalPolicy.Origins, corsPolicy.Origins);
        Assert.Equal(originalPolicy.Origins, corsPolicy.Origins);
        Assert.Empty(corsPolicy.Headers);
        Assert.Empty(corsPolicy.Methods);
        Assert.Empty(corsPolicy.ExposedHeaders);
    }

    [Fact]
    public void Constructor_WithNoOrigin()
    {
        // Arrange & Act
        var builder = new CorsPolicyBuilder();

        // Assert
        var corsPolicy = builder.Build();
        Assert.False(corsPolicy.AllowAnyHeader);
        Assert.False(corsPolicy.AllowAnyMethod);
        Assert.False(corsPolicy.AllowAnyOrigin);
        Assert.False(corsPolicy.SupportsCredentials);
        Assert.Empty(corsPolicy.ExposedHeaders);
        Assert.Empty(corsPolicy.Headers);
        Assert.Empty(corsPolicy.Methods);
        Assert.Empty(corsPolicy.Origins);
        Assert.Null(corsPolicy.PreflightMaxAge);
    }

    [Theory]
    [InlineData("")]
    [InlineData("http://example.com,http://example2.com")]
    public void Constructor_WithParamsOrigin_InitializesOrigin(string origin)
    {
        // Arrange
        var origins = origin.Split(',');

        // Act
        var builder = new CorsPolicyBuilder(origins);

        // Assert
        var corsPolicy = builder.Build();
        Assert.False(corsPolicy.AllowAnyHeader);
        Assert.False(corsPolicy.AllowAnyMethod);
        Assert.False(corsPolicy.AllowAnyOrigin);
        Assert.False(corsPolicy.SupportsCredentials);
        Assert.Empty(corsPolicy.ExposedHeaders);
        Assert.Empty(corsPolicy.Headers);
        Assert.Empty(corsPolicy.Methods);
        Assert.Equal(origins.ToList(), corsPolicy.Origins);
        Assert.Null(corsPolicy.PreflightMaxAge);
    }

    [Fact]
    public void WithOrigins_AddsOrigins()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();

        // Act
        builder.WithOrigins("http://example.com", "http://example2.com");

        // Assert
        var corsPolicy = builder.Build();
        Assert.False(corsPolicy.AllowAnyOrigin);
        Assert.Equal(new List<string>() { "http://example.com", "http://example2.com" }, corsPolicy.Origins);
    }

    [Fact]
    public void WithOrigins_NormalizesOrigins()
    {
        // Arrange
        var builder = new CorsPolicyBuilder("http://www.EXAMPLE.com", "HTTPS://example2.com");

        // Assert
        var corsPolicy = builder.Build();
        Assert.Equal(new List<string>() { "http://www.example.com", "https://example2.com" }, corsPolicy.Origins);
    }

    [Fact]
    public void WithOrigins_ThrowsIfArgumentNull()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();
        string[] args = null;

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithOrigins(args));
    }

    [Fact]
    public void WithOrigins_ThrowsIfArgumentArrayContainsNull()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();
        string[] args = new string[] { null };

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithOrigins(args));
    }

    [Fact]
    public void AllowAnyOrigin_AllowsAny()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();

        // Act
        builder.AllowAnyOrigin();

        // Assert
        var corsPolicy = builder.Build();
        Assert.True(corsPolicy.AllowAnyOrigin);
        Assert.Equal(new List<string>() { "*" }, corsPolicy.Origins);
    }

    [Fact]
    public void SetIsOriginAllowed_AddsIsOriginAllowed()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();
        Func<string, bool> isOriginAllowed = origin => true;

        // Act
        builder.SetIsOriginAllowed(isOriginAllowed);

        // Assert
        var corsPolicy = builder.Build();
        Assert.Same(corsPolicy.IsOriginAllowed, isOriginAllowed);
    }

    [Fact]
    public void SetIsOriginAllowedToAllowWildcardSubdomains_AllowsWildcardSubdomains()
    {
        // Arrange
        var builder = new CorsPolicyBuilder("http://*.example.com");

        // Act
        builder.SetIsOriginAllowedToAllowWildcardSubdomains();

        // Assert
        var corsPolicy = builder.Build();
        Assert.True(corsPolicy.IsOriginAllowed("http://test.example.com"));
    }

    [Fact]
    public void SetIsOriginAllowedToAllowWildcardSubdomains_DoesNotAllowRootDomain()
    {
        // Arrange
        var builder = new CorsPolicyBuilder("http://*.example.com");

        // Act
        builder.SetIsOriginAllowedToAllowWildcardSubdomains();

        // Assert
        var corsPolicy = builder.Build();
        Assert.False(corsPolicy.IsOriginAllowed("http://example.com"));
    }

    [Fact]
    public void WithMethods_AddsMethods()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();

        // Act
        builder.WithMethods("PUT", "GET");

        // Assert
        var corsPolicy = builder.Build();
        Assert.False(corsPolicy.AllowAnyOrigin);
        Assert.Equal(new List<string>() { "PUT", "GET" }, corsPolicy.Methods);
    }

    [Fact]
    public void AllowAnyMethod_AllowsAny()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();

        // Act
        builder.AllowAnyMethod();

        // Assert
        var corsPolicy = builder.Build();
        Assert.True(corsPolicy.AllowAnyMethod);
        Assert.Equal(new List<string>() { "*" }, corsPolicy.Methods);
    }

    [Fact]
    public void WithHeaders_AddsHeaders()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();

        // Act
        builder.WithHeaders("example1", "example2");

        // Assert
        var corsPolicy = builder.Build();
        Assert.False(corsPolicy.AllowAnyHeader);
        Assert.Equal(new List<string>() { "example1", "example2" }, corsPolicy.Headers);
    }

    [Fact]
    public void AllowAnyHeaders_AllowsAny()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();

        // Act
        builder.AllowAnyHeader();

        // Assert
        var corsPolicy = builder.Build();
        Assert.True(corsPolicy.AllowAnyHeader);
        Assert.Equal(new List<string>() { "*" }, corsPolicy.Headers);
    }

    [Fact]
    public void WithExposedHeaders_AddsExposedHeaders()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();

        // Act
        builder.WithExposedHeaders("exposed1", "exposed2");

        // Assert
        var corsPolicy = builder.Build();
        Assert.Equal(new List<string>() { "exposed1", "exposed2" }, corsPolicy.ExposedHeaders);
    }

    [Fact]
    public void SetPreFlightMaxAge_SetsThePreFlightAge()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();

        // Act
        builder.SetPreflightMaxAge(TimeSpan.FromSeconds(12));

        // Assert
        var corsPolicy = builder.Build();
        Assert.Equal(TimeSpan.FromSeconds(12), corsPolicy.PreflightMaxAge);
    }

    [Fact]
    public void AllowCredential_SetsSupportsCredentials_ToTrue()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();

        // Act
        builder.AllowCredentials();

        // Assert
        var corsPolicy = builder.Build();
        Assert.True(corsPolicy.SupportsCredentials);
    }

    [Fact]
    public void DisallowCredential_SetsSupportsCredentials_ToFalse()
    {
        // Arrange
        var builder = new CorsPolicyBuilder();

        // Act
        builder.DisallowCredentials();

        // Assert
        var corsPolicy = builder.Build();
        Assert.False(corsPolicy.SupportsCredentials);
    }

    [Fact]
    public void Build_ThrowsIfConfiguredToAllowAnyOriginWithCredentials()
    {
        // Arrange
        var builder = new CorsPolicyBuilder()
            .AllowAnyOrigin()
            .AllowCredentials();

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());

        // Assert
        Assert.Equal(Resources.InsecureConfiguration, ex.Message);
    }

    [Theory]
    [InlineData("Some-String", "some-string")]
    [InlineData("x:\\Test", "x:\\test")]
    [InlineData("FTP://Some-url", "ftp://some-url")]
    public void GetNormalizedOrigin_ReturnsLowerCasedValue_IfStringIsNotHttpOrHttpsUrl(string origin, string expected)
    {
        // Act
        var normalizedOrigin = CorsPolicyBuilder.GetNormalizedOrigin(origin);

        // Assert
        Assert.Equal(expected, normalizedOrigin);
    }

    [Fact]
    public void GetNormalizedOrigin_DoesNotAddPort_IfUriDoesNotSpecifyOne()
    {
        // Arrange
        var origin = "http://www.example.com";

        // Act
        var normalizedOrigin = CorsPolicyBuilder.GetNormalizedOrigin(origin);

        // Assert
        Assert.Equal(origin, normalizedOrigin);
    }

    [Fact]
    public void GetNormalizedOrigin_LowerCasesScheme()
    {
        // Arrange
        var origin = "HTTP://www.example.com";

        // Act
        var normalizedOrigin = CorsPolicyBuilder.GetNormalizedOrigin(origin);

        // Assert
        Assert.Equal("http://www.example.com", normalizedOrigin);
    }

    [Fact]
    public void GetNormalizedOrigin_LowerCasesHost()
    {
        // Arrange
        var origin = "http://www.Example.Com";

        // Act
        var normalizedOrigin = CorsPolicyBuilder.GetNormalizedOrigin(origin);

        // Assert
        Assert.Equal("http://www.example.com", normalizedOrigin);
    }

    [Theory]
    [InlineData("http://www.Example.com:80", "http://www.example.com:80")]
    [InlineData("https://www.Example.com:8080", "https://www.example.com:8080")]
    public void GetNormalizedOrigin_PreservesPort_ForNonIdnHosts(string origin, string expected)
    {
        // Act
        var normalizedOrigin = CorsPolicyBuilder.GetNormalizedOrigin(origin);

        // Assert
        Assert.Equal(expected, normalizedOrigin);
    }

    [Theory]
    [InlineData("http://Bücher.example", "http://xn--bcher-kva.example")]
    [InlineData("http://Bücher.example.com:83", "http://xn--bcher-kva.example.com:83")]
    [InlineData("https://example.қаз", "https://example.xn--80ao21a")]
    // Note that in following case, the default port (443 for HTTPS) is not preserved.
    [InlineData("https://www.example.இந்தியா:443", "https://www.example.xn--xkc2dl3a5ee0h")]
    public void GetNormalizedOrigin_ReturnsPunyCodedOrigin(string origin, string expected)
    {
        // Act
        var normalizedOrigin = CorsPolicyBuilder.GetNormalizedOrigin(origin);

        // Assert
        Assert.Equal(expected, normalizedOrigin);
    }
}
