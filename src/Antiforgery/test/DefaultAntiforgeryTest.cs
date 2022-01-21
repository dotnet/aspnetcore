// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

public class DefaultAntiforgeryTest
{
    private const string ResponseCacheHeadersOverrideWarningMessage =
        "The 'Cache-Control' and 'Pragma' headers have been overridden and set to 'no-cache, no-store' and " +
         "'no-cache' respectively to prevent caching of this response. Any response that uses antiforgery " +
        "should not be cached.";

    [Fact]
    public async Task ChecksSSL_ValidateRequestAsync_Throws()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var options = new AntiforgeryOptions
        {
            Cookie = new CookieBuilder
            {
                SecurePolicy = CookieSecurePolicy.Always
            }
        };
        var antiforgery = GetAntiforgery(httpContext, options);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => antiforgery.ValidateRequestAsync(httpContext));
        Assert.Equal(
            @"The antiforgery system has the configuration value AntiforgeryOptions.Cookie.SecurePolicy = Always, " +
            "but the current request is not an SSL request.",
            exception.Message);
    }

    [Fact]
    public async Task ChecksSSL_IsRequestValidAsync_Throws()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var options = new AntiforgeryOptions()
        {
            Cookie = { SecurePolicy = CookieSecurePolicy.Always }
        };

        var antiforgery = GetAntiforgery(httpContext, options);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => antiforgery.IsRequestValidAsync(httpContext));
        Assert.Equal(
            @"The antiforgery system has the configuration value AntiforgeryOptions.Cookie.SecurePolicy = Always, " +
            "but the current request is not an SSL request.",
            exception.Message);
    }

    [Fact]
    public void ChecksSSL_GetAndStoreTokens_Throws()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var options = new AntiforgeryOptions()
        {
            Cookie = { SecurePolicy = CookieSecurePolicy.Always }
        };

        var antiforgery = GetAntiforgery(httpContext, options);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => antiforgery.GetAndStoreTokens(httpContext));
        Assert.Equal(
             @"The antiforgery system has the configuration value AntiforgeryOptions.Cookie.SecurePolicy = Always, " +
             "but the current request is not an SSL request.",
             exception.Message);
    }

    [Fact]
    public void ChecksSSL_GetTokens_Throws()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var options = new AntiforgeryOptions()
        {
            Cookie = { SecurePolicy = CookieSecurePolicy.Always }
        };

        var antiforgery = GetAntiforgery(httpContext, options);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => antiforgery.GetTokens(httpContext));
        Assert.Equal(
             @"The antiforgery system has the configuration value AntiforgeryOptions.Cookie.SecurePolicy = Always, " +
             "but the current request is not an SSL request.",
             exception.Message);
    }

    [Fact]
    public void ChecksSSL_SetCookieTokenAndHeader_Throws()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var options = new AntiforgeryOptions()
        {
            Cookie = { SecurePolicy = CookieSecurePolicy.Always }
        };

        var antiforgery = GetAntiforgery(httpContext, options);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => antiforgery.SetCookieTokenAndHeader(httpContext));
        Assert.Equal(
             @"The antiforgery system has the configuration value AntiforgeryOptions.Cookie.SecurePolicy = Always, " +
             "but the current request is not an SSL request.",
             exception.Message);
    }

    [Fact]
    public void GetTokens_ExistingInvalidCookieToken_GeneratesANewCookieTokenAndANewFormToken()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature();
        // Generate a new cookie.
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: false,
            isOldCookieValid: false,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        // Act
        var tokenset = antiforgery.GetTokens(context.HttpContext);

        // Assert
        Assert.Equal(context.TestTokenSet.NewCookieTokenString, tokenset.CookieToken);
        Assert.Equal(context.TestTokenSet.FormTokenString, tokenset.RequestToken);

        Assert.NotNull(antiforgeryFeature);
        Assert.True(antiforgeryFeature.HaveDeserializedCookieToken);
        Assert.Equal(context.TestTokenSet.OldCookieToken, antiforgeryFeature.CookieToken);
        Assert.True(antiforgeryFeature.HaveGeneratedNewCookieToken);
        Assert.Equal(context.TestTokenSet.NewCookieToken, antiforgeryFeature.NewCookieToken);
        Assert.Equal(context.TestTokenSet.NewCookieTokenString, antiforgeryFeature.NewCookieTokenString);
        Assert.Equal(context.TestTokenSet.RequestToken, antiforgeryFeature.NewRequestToken);
        Assert.Equal(context.TestTokenSet.FormTokenString, antiforgeryFeature.NewRequestTokenString);
    }

    [Fact]
    public void GetTokens_ExistingInvalidCookieToken_SwallowsExceptions()
    {
        // Arrange
        // Make sure the existing cookie is invalid.
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: false,
            isOldCookieValid: false);

        // Exception will cause the cookieToken to be null.
        context.TokenSerializer
            .Setup(o => o.Deserialize(context.TestTokenSet.OldCookieTokenString))
            .Throws(new Exception("should be swallowed"));
        context.TokenGenerator
            .Setup(o => o.IsCookieTokenValid(null))
            .Returns(false);

        var antiforgery = GetAntiforgery(context);

        // Act
        var tokenset = antiforgery.GetTokens(context.HttpContext);

        // Assert
        Assert.Equal(context.TestTokenSet.NewCookieTokenString, tokenset.CookieToken);
        Assert.Equal(context.TestTokenSet.FormTokenString, tokenset.RequestToken);
    }

    [Fact]
    public void GetTokens_ExistingValidCookieToken_GeneratesANewFormToken()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: true,
            isOldCookieValid: true,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        // Act
        var tokenset = antiforgery.GetTokens(context.HttpContext);

        // Assert
        Assert.Null(tokenset.CookieToken);
        Assert.Equal(context.TestTokenSet.FormTokenString, tokenset.RequestToken);

        Assert.NotNull(antiforgeryFeature);
        Assert.True(antiforgeryFeature.HaveDeserializedCookieToken);
        Assert.Equal(context.TestTokenSet.OldCookieToken, antiforgeryFeature.CookieToken);
        Assert.True(antiforgeryFeature.HaveGeneratedNewCookieToken);
        Assert.Null(antiforgeryFeature.NewCookieToken);
        Assert.Equal(context.TestTokenSet.RequestToken, antiforgeryFeature.NewRequestToken);
        Assert.Equal(context.TestTokenSet.FormTokenString, antiforgeryFeature.NewRequestTokenString);
    }

    [Fact]
    public void GetTokens_DoesNotSerializeTwice()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature
        {
            HaveDeserializedCookieToken = true,
            HaveGeneratedNewCookieToken = true,
            NewRequestToken = new AntiforgeryToken(),
            NewRequestTokenString = "serialized-form-token-from-context",
        };
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: true,
            isOldCookieValid: true,
            antiforgeryFeature: antiforgeryFeature);

        var antiforgery = GetAntiforgery(context);

        // Act
        var tokenset = antiforgery.GetTokens(context.HttpContext);

        // Assert
        Assert.Null(tokenset.CookieToken);
        Assert.Equal("serialized-form-token-from-context", tokenset.RequestToken);

        Assert.Null(antiforgeryFeature.NewCookieToken);

        // Token serializer not used.
        context.TokenSerializer.Verify(
            o => o.Deserialize(It.IsAny<string>()),
            Times.Never);
        context.TokenSerializer.Verify(
            o => o.Serialize(It.IsAny<AntiforgeryToken>()),
            Times.Never);
    }

    [Fact]
    public void GetAndStoreTokens_ExistingValidCookieToken_NotOverriden()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: true,
            isOldCookieValid: true,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        // Act
        var tokenSet = antiforgery.GetAndStoreTokens(context.HttpContext);

        // Assert
        // We shouldn't have saved the cookie because it already existed.
        context.TokenStore.Verify(
            t => t.SaveCookieToken(It.IsAny<HttpContext>(), It.IsAny<string>()),
            Times.Never);

        Assert.Null(tokenSet.CookieToken);
        Assert.Equal(context.TestTokenSet.FormTokenString, tokenSet.RequestToken);

        Assert.NotNull(antiforgeryFeature);
        Assert.True(antiforgeryFeature.HaveDeserializedCookieToken);
        Assert.Equal(context.TestTokenSet.OldCookieToken, antiforgeryFeature.CookieToken);
        Assert.True(antiforgeryFeature.HaveGeneratedNewCookieToken);
        Assert.Null(antiforgeryFeature.NewCookieToken);
        Assert.Equal(context.TestTokenSet.RequestToken, antiforgeryFeature.NewRequestToken);
        Assert.Equal(context.TestTokenSet.FormTokenString, antiforgeryFeature.NewRequestTokenString);
    }

    [Fact]
    public void GetAndStoreTokens_ExistingValidCookieToken_NotOverriden_AndSetsDoNotCacheHeaders()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: true,
            isOldCookieValid: true,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        // Act
        var tokenSet = antiforgery.GetAndStoreTokens(context.HttpContext);

        // Assert
        // We shouldn't have saved the cookie because it already existed.
        context.TokenStore.Verify(
            t => t.SaveCookieToken(It.IsAny<HttpContext>(), It.IsAny<string>()),
            Times.Never);

        Assert.Null(tokenSet.CookieToken);
        Assert.Equal(context.TestTokenSet.FormTokenString, tokenSet.RequestToken);

        Assert.NotNull(antiforgeryFeature);
        Assert.Equal(context.TestTokenSet.OldCookieToken, antiforgeryFeature.CookieToken);
        Assert.Equal("no-cache, no-store", context.HttpContext.Response.Headers.CacheControl);
        Assert.Equal("no-cache", context.HttpContext.Response.Headers.Pragma);
    }

    [Fact]
    public void GetAndStoreTokens_ExistingCachingHeaders_Overriden()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: true,
            isOldCookieValid: true,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);
        context.HttpContext.Response.Headers.CacheControl = "public";

        // Act
        var tokenSet = antiforgery.GetAndStoreTokens(context.HttpContext);

        // Assert
        // We shouldn't have saved the cookie because it already existed.
        context.TokenStore.Verify(
            t => t.SaveCookieToken(It.IsAny<HttpContext>(), It.IsAny<string>()),
            Times.Never);

        Assert.Null(tokenSet.CookieToken);
        Assert.Equal(context.TestTokenSet.FormTokenString, tokenSet.RequestToken);

        Assert.NotNull(antiforgeryFeature);
        Assert.Equal(context.TestTokenSet.OldCookieToken, antiforgeryFeature.CookieToken);
        Assert.Equal("no-cache, no-store", context.HttpContext.Response.Headers.CacheControl);
        Assert.Equal("no-cache", context.HttpContext.Response.Headers.Pragma);
    }

    private string GetAndStoreTokens_CacheHeadersArrangeAct(TestSink testSink, string headerName, string headerValue)
    {
        // Arrange
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(lf => lf.CreateLogger(typeof(DefaultAntiforgery).FullName!))
            .Returns(new TestLogger("test logger", testSink, enabled: true));
        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory.Object);
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: true,
            isOldCookieValid: true,
            antiforgeryFeature: antiforgeryFeature);
        context.HttpContext.RequestServices = services.BuildServiceProvider();
        var antiforgery = GetAntiforgery(context);
        context.HttpContext.Response.Headers[headerName] = headerValue;

        // Act
        antiforgery.GetAndStoreTokens(context.HttpContext);
        return context.HttpContext.Response.Headers[headerName].ToString();
    }

    [Theory]
    [InlineData("Cache-Control", "no-cache, no-store")]
    [InlineData("Cache-Control", "NO-CACHE, NO-STORE")]
    [InlineData("Cache-Control", "no-cache, no-store, private")]
    [InlineData("Cache-Control", "NO-CACHE, NO-STORE, PRIVATE")]
    public void GetAndStoreTokens_DoesNotOverwriteCacheControlHeader(string headerName, string headerValue)
    {
        var testSink = new TestSink();
        var actualHeaderValue = GetAndStoreTokens_CacheHeadersArrangeAct(testSink, headerName, headerValue);

        // Assert
        Assert.Equal(headerValue, actualHeaderValue);

        var hasWarningMessage = testSink.Writes
            .Where(wc => wc.LogLevel == LogLevel.Warning)
            .Select(wc => wc.State?.ToString())
            .Contains(ResponseCacheHeadersOverrideWarningMessage);
        Assert.False(hasWarningMessage);
    }

    [Theory]
    [InlineData("Cache-Control", "no-cache, private")]
    [InlineData("Cache-Control", "NO-CACHE, PRIVATE")]
    public void GetAndStoreTokens_OverwritesCacheControlHeader_IfNoStoreIsNotSet(string headerName, string headerValue)
    {
        var testSink = new TestSink();
        var actualHeaderValue = GetAndStoreTokens_CacheHeadersArrangeAct(testSink, headerName, headerValue);

        // Assert
        Assert.NotEqual(headerValue, actualHeaderValue);

        var hasWarningMessage = testSink.Writes
            .Where(wc => wc.LogLevel == LogLevel.Warning)
            .Select(wc => wc.State?.ToString())
            .Contains(ResponseCacheHeadersOverrideWarningMessage);
        Assert.True(hasWarningMessage);
    }

    [Theory]
    [InlineData("Cache-Control", "no-store, private")]
    [InlineData("Cache-Control", "NO-STORE, PRIVATE")]
    public void GetAndStoreTokens_OverwritesCacheControlHeader_IfNoCacheIsNotSet(string headerName, string headerValue)
    {
        var testSink = new TestSink();
        var actualHeaderValue = GetAndStoreTokens_CacheHeadersArrangeAct(testSink, headerName, headerValue);

        // Assert
        Assert.NotEqual(headerValue, actualHeaderValue);

        var hasWarningMessage = testSink.Writes
            .Where(wc => wc.LogLevel == LogLevel.Warning)
            .Select(wc => wc.State?.ToString())
            .Contains(ResponseCacheHeadersOverrideWarningMessage);
        Assert.True(hasWarningMessage);
    }

    [Theory]
    [InlineData("Pragma", "no-cache")]
    [InlineData("Pragma", "NO-CACHE")]
    public void GetAndStoreTokens_DoesNotOverwritePragmaHeader(string headerName, string headerValue)
    {
        var testSink = new TestSink();
        var actualHeaderValue = GetAndStoreTokens_CacheHeadersArrangeAct(testSink, headerName, headerValue);

        // Assert
        Assert.Equal(headerValue, actualHeaderValue);

        var hasWarningMessage = testSink.Writes
            .Where(wc => wc.LogLevel == LogLevel.Warning)
            .Select(wc => wc.State?.ToString())
            .Contains(ResponseCacheHeadersOverrideWarningMessage);
        Assert.False(hasWarningMessage);
    }

    [Fact]
    public void GetAndStoreTokens_NoExistingCookieToken_Saved()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: false,
            isOldCookieValid: false,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        // Act
        var tokenSet = antiforgery.GetAndStoreTokens(context.HttpContext);

        // Assert
        context.TokenStore.Verify(
            t => t.SaveCookieToken(It.IsAny<HttpContext>(), context.TestTokenSet.NewCookieTokenString),
            Times.Once);

        Assert.Equal(context.TestTokenSet.NewCookieTokenString, tokenSet.CookieToken);
        Assert.Equal(context.TestTokenSet.FormTokenString, tokenSet.RequestToken);

        Assert.NotNull(antiforgeryFeature);
        Assert.True(antiforgeryFeature.HaveDeserializedCookieToken);
        Assert.Equal(context.TestTokenSet.OldCookieToken, antiforgeryFeature.CookieToken);
        Assert.True(antiforgeryFeature.HaveGeneratedNewCookieToken);
        Assert.Equal(context.TestTokenSet.NewCookieToken, antiforgeryFeature.NewCookieToken);
        Assert.Equal(context.TestTokenSet.NewCookieTokenString, antiforgeryFeature.NewCookieTokenString);
        Assert.Equal(context.TestTokenSet.RequestToken, antiforgeryFeature.NewRequestToken);
        Assert.Equal(context.TestTokenSet.FormTokenString, antiforgeryFeature.NewRequestTokenString);
        Assert.True(antiforgeryFeature.HaveStoredNewCookieToken);
    }

    [Fact]
    public void GetAndStoreTokens_NoExistingCookieToken_Saved_AndSetsDoNotCacheHeaders()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: false,
            isOldCookieValid: false,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        // Act
        var tokenSet = antiforgery.GetAndStoreTokens(context.HttpContext);

        // Assert
        context.TokenStore.Verify(
            t => t.SaveCookieToken(It.IsAny<HttpContext>(), context.TestTokenSet.NewCookieTokenString),
            Times.Once);

        Assert.Equal(context.TestTokenSet.NewCookieTokenString, tokenSet.CookieToken);
        Assert.Equal(context.TestTokenSet.FormTokenString, tokenSet.RequestToken);

        Assert.NotNull(antiforgeryFeature);
        Assert.True(antiforgeryFeature.HaveDeserializedCookieToken);
        Assert.Equal(context.TestTokenSet.OldCookieToken, antiforgeryFeature.CookieToken);
        Assert.Equal("no-cache, no-store", context.HttpContext.Response.Headers.CacheControl);
        Assert.Equal("no-cache", context.HttpContext.Response.Headers.Pragma);
    }

    [Fact]
    public void GetAndStoreTokens_DoesNotSerializeTwice()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature
        {
            HaveDeserializedCookieToken = true,
            HaveGeneratedNewCookieToken = true,
            NewCookieToken = new AntiforgeryToken(),
            NewCookieTokenString = "serialized-cookie-token-from-context",
            NewRequestToken = new AntiforgeryToken(),
            NewRequestTokenString = "serialized-form-token-from-context",
        };
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: true,
            isOldCookieValid: true,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        context.TokenStore
            .Setup(t => t.SaveCookieToken(context.HttpContext, "serialized-cookie-token-from-context"))
            .Verifiable();

        // Act
        var tokenset = antiforgery.GetAndStoreTokens(context.HttpContext);

        // Assert
        // Token store used once, with expected arguments.
        // Passed context's cookie token though request's cookie token was valid.
        context.TokenStore.Verify(
            t => t.SaveCookieToken(context.HttpContext, "serialized-cookie-token-from-context"),
            Times.Once);

        // Token serializer not used.
        context.TokenSerializer.Verify(
            o => o.Deserialize(It.IsAny<string>()),
            Times.Never);
        context.TokenSerializer.Verify(
            o => o.Serialize(It.IsAny<AntiforgeryToken>()),
            Times.Never);

        Assert.Equal("serialized-cookie-token-from-context", tokenset.CookieToken);
        Assert.Equal("serialized-form-token-from-context", tokenset.RequestToken);

        Assert.True(antiforgeryFeature.HaveStoredNewCookieToken);
    }

    [Fact]
    public void GetAndStoreTokens_DoesNotStoreTwice()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature
        {
            HaveDeserializedCookieToken = true,
            HaveGeneratedNewCookieToken = true,
            HaveStoredNewCookieToken = true,
            NewCookieToken = new AntiforgeryToken(),
            NewCookieTokenString = "serialized-cookie-token-from-context",
            NewRequestToken = new AntiforgeryToken(),
            NewRequestTokenString = "serialized-form-token-from-context",
        };
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: true,
            isOldCookieValid: true,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        // Act
        var tokenset = antiforgery.GetAndStoreTokens(context.HttpContext);

        // Assert
        // Token store not used.
        context.TokenStore.Verify(
            t => t.SaveCookieToken(It.IsAny<HttpContext>(), It.IsAny<string>()),
            Times.Never);

        // Token serializer not used.
        context.TokenSerializer.Verify(
            o => o.Deserialize(It.IsAny<string>()),
            Times.Never);
        context.TokenSerializer.Verify(
            o => o.Serialize(It.IsAny<AntiforgeryToken>()),
            Times.Never);

        Assert.Equal("serialized-cookie-token-from-context", tokenset.CookieToken);
        Assert.Equal("serialized-form-token-from-context", tokenset.RequestToken);
    }

    [Fact]
    public async Task IsRequestValidAsync_FromStore_Failure()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(new AntiforgeryOptions(), antiforgeryFeature: antiforgeryFeature);

        string? message;
        context.TokenGenerator
            .Setup(o => o.TryValidateTokenSet(
                context.HttpContext,
                context.TestTokenSet.OldCookieToken,
                context.TestTokenSet.RequestToken,
                out message))
            .Returns(false);

        var antiforgery = GetAntiforgery(context);

        // Act
        var result = await antiforgery.IsRequestValidAsync(context.HttpContext);

        // Assert
        Assert.False(result);
        context.TokenGenerator.Verify();

        // Failed _after_ updating the AntiforgeryContext.
        Assert.NotNull(antiforgeryFeature);
        Assert.True(antiforgeryFeature.HaveDeserializedCookieToken);
        Assert.Equal(context.TestTokenSet.OldCookieToken, antiforgeryFeature.CookieToken);
        Assert.True(antiforgeryFeature.HaveDeserializedRequestToken);
        Assert.Equal(context.TestTokenSet.RequestToken, antiforgeryFeature.RequestToken);
    }

    [Fact]
    public async Task IsRequestValidAsync_FromStore_Success()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(new AntiforgeryOptions(), antiforgeryFeature: antiforgeryFeature);
        context.HttpContext.Request.Method = "POST";

        string? message;
        context.TokenGenerator
            .Setup(o => o.TryValidateTokenSet(
                context.HttpContext,
                context.TestTokenSet.OldCookieToken,
                context.TestTokenSet.RequestToken,
                out message))
            .Returns(true)
            .Verifiable();

        var antiforgery = GetAntiforgery(context);

        // Act
        var result = await antiforgery.IsRequestValidAsync(context.HttpContext);

        // Assert
        Assert.True(result);
        context.TokenGenerator.Verify();

        Assert.NotNull(antiforgeryFeature);
        Assert.True(antiforgeryFeature.HaveDeserializedCookieToken);
        Assert.Equal(context.TestTokenSet.OldCookieToken, antiforgeryFeature.CookieToken);
        Assert.True(antiforgeryFeature.HaveDeserializedRequestToken);
        Assert.Equal(context.TestTokenSet.RequestToken, antiforgeryFeature.RequestToken);
    }

    [Fact]
    public async Task IsRequestValidAsync_DoesNotDeserializeTwice()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature
        {
            HaveDeserializedCookieToken = true,
            CookieToken = new AntiforgeryToken(),
            HaveDeserializedRequestToken = true,
            RequestToken = new AntiforgeryToken(),
        };
        var context = CreateMockContext(new AntiforgeryOptions(), antiforgeryFeature: antiforgeryFeature);
        context.HttpContext.Request.Method = "POST";

        string? message;
        context.TokenGenerator
            .Setup(o => o.TryValidateTokenSet(
                context.HttpContext,
                antiforgeryFeature.CookieToken,
                antiforgeryFeature.RequestToken,
                out message))
            .Returns(true)
            .Verifiable();

        var antiforgery = GetAntiforgery(context);

        // Act
        var result = await antiforgery.IsRequestValidAsync(context.HttpContext);

        // Assert
        Assert.True(result);
        context.TokenGenerator.Verify();

        // Token serializer not used.
        context.TokenSerializer.Verify(
            o => o.Deserialize(It.IsAny<string>()),
            Times.Never);
        context.TokenSerializer.Verify(
            o => o.Serialize(It.IsAny<AntiforgeryToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("GeT")]
    [InlineData("HEAD")]
    [InlineData("options")]
    [InlineData("TrAcE")]
    public async Task IsRequestValidAsync_SkipsAntiforgery_ForSafeHttpMethods(string httpMethod)
    {
        // Arrange
        var context = CreateMockContext(new AntiforgeryOptions());
        context.HttpContext.Request.Method = httpMethod;

        string? message;
        context.TokenGenerator
            .Setup(o => o.TryValidateTokenSet(
                context.HttpContext,
                It.IsAny<AntiforgeryToken>(),
                It.IsAny<AntiforgeryToken>(),
                out message))
            .Returns(false)
            .Verifiable();

        var antiforgery = GetAntiforgery(context);

        // Act
        var result = await antiforgery.IsRequestValidAsync(context.HttpContext);

        // Assert
        Assert.True(result);
        context.TokenGenerator
            .Verify(o => o.TryValidateTokenSet(
                context.HttpContext,
                It.IsAny<AntiforgeryToken>(),
                It.IsAny<AntiforgeryToken>(),
                out message),
                Times.Never);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("post")]
    [InlineData("Delete")]
    [InlineData("Custom")]
    public async Task IsRequestValidAsync_ValidatesAntiforgery_ForNonSafeHttpMethods(string httpMethod)
    {
        // Arrange
        var context = CreateMockContext(new AntiforgeryOptions());
        context.HttpContext.Request.Method = httpMethod;

        string? message;
        context.TokenGenerator
            .Setup(o => o.TryValidateTokenSet(
                context.HttpContext,
                It.IsAny<AntiforgeryToken>(),
                It.IsAny<AntiforgeryToken>(),
                out message))
            .Returns(true)
            .Verifiable();

        var antiforgery = GetAntiforgery(context);

        // Act
        var result = await antiforgery.IsRequestValidAsync(context.HttpContext);

        // Assert
        Assert.True(result);
        context.TokenGenerator.Verify();
    }

    [Fact]
    public async Task ValidateRequestAsync_FromStore_Failure()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(new AntiforgeryOptions(), antiforgeryFeature: antiforgeryFeature);

        var message = "my-message";
        context.TokenGenerator
            .Setup(o => o.TryValidateTokenSet(
                context.HttpContext,
                context.TestTokenSet.OldCookieToken,
                context.TestTokenSet.RequestToken,
                out message))
            .Returns(false)
            .Verifiable();
        var antiforgery = GetAntiforgery(context);

        // Act & assert
        var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
            () => antiforgery.ValidateRequestAsync(context.HttpContext));
        Assert.Equal("my-message", exception.Message);
        context.TokenGenerator.Verify();

        // Failed _after_ updating the AntiforgeryContext.
        Assert.NotNull(antiforgeryFeature);
        Assert.True(antiforgeryFeature.HaveDeserializedCookieToken);
        Assert.Equal(context.TestTokenSet.OldCookieToken, antiforgeryFeature.CookieToken);
        Assert.True(antiforgeryFeature.HaveDeserializedRequestToken);
        Assert.Equal(context.TestTokenSet.RequestToken, antiforgeryFeature.RequestToken);
    }

    [Fact]
    public async Task ValidateRequestAsync_FromStore_Success()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(new AntiforgeryOptions(), antiforgeryFeature: antiforgeryFeature);

        string? message;
        context.TokenGenerator
            .Setup(o => o.TryValidateTokenSet(
                context.HttpContext,
                context.TestTokenSet.OldCookieToken,
                context.TestTokenSet.RequestToken,
                out message))
            .Returns(true)
            .Verifiable();

        var antiforgery = GetAntiforgery(context);

        // Act
        await antiforgery.ValidateRequestAsync(context.HttpContext);

        // Assert
        context.TokenGenerator.Verify();

        Assert.NotNull(antiforgeryFeature);
        Assert.True(antiforgeryFeature.HaveDeserializedCookieToken);
        Assert.Equal(context.TestTokenSet.OldCookieToken, antiforgeryFeature.CookieToken);
        Assert.True(antiforgeryFeature.HaveDeserializedRequestToken);
        Assert.Equal(context.TestTokenSet.RequestToken, antiforgeryFeature.RequestToken);
    }

    [Fact]
    public async Task ValidateRequestAsync_NoCookieToken_Throws()
    {
        // Arrange
        var context = CreateMockContext(new AntiforgeryOptions()
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = null,
        });

        var tokenSet = new AntiforgeryTokenSet(null, null, "form-field-name", null);
        context.TokenStore
            .Setup(s => s.GetRequestTokensAsync(context.HttpContext))
            .Returns(Task.FromResult(tokenSet));

        var antiforgery = GetAntiforgery(context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
            () => antiforgery.ValidateRequestAsync(context.HttpContext));
        Assert.Equal("The required antiforgery cookie \"cookie-name\" is not present.", exception.Message);
    }

    [Fact]
    public async Task ValidateRequestAsync_NonFormRequest_HeaderDisabled_Throws()
    {
        // Arrange
        var context = CreateMockContext(new AntiforgeryOptions()
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = null,
        });

        var tokenSet = new AntiforgeryTokenSet(null, "cookie-token", "form-field-name", null);
        context.TokenStore
            .Setup(s => s.GetRequestTokensAsync(context.HttpContext))
            .Returns(Task.FromResult(tokenSet));

        var antiforgery = GetAntiforgery(context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
            () => antiforgery.ValidateRequestAsync(context.HttpContext));
        Assert.Equal("The required antiforgery form field \"form-field-name\" is not present.", exception.Message);
    }

    [Fact]
    public async Task ValidateRequestAsync_NonFormRequest_NoHeaderValue_Throws()
    {
        // Arrange
        var context = CreateMockContext(new AntiforgeryOptions()
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = "header-name",
        });

        context.HttpContext.Request.ContentType = "application/json";

        var tokenSet = new AntiforgeryTokenSet(null, "cookie-token", "form-field-name", "header-name");
        context.TokenStore
            .Setup(s => s.GetRequestTokensAsync(context.HttpContext))
            .Returns(Task.FromResult(tokenSet));

        var antiforgery = GetAntiforgery(context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
            () => antiforgery.ValidateRequestAsync(context.HttpContext));
        Assert.Equal("The required antiforgery header value \"header-name\" is not present.", exception.Message);
    }

    [Fact]
    public async Task ValidateRequestAsync_FormRequest_NoRequestTokenValue_Throws()
    {
        // Arrange
        var context = CreateMockContext(new AntiforgeryOptions()
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = "header-name",
        });

        context.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";

        var tokenSet = new AntiforgeryTokenSet(null, "cookie-token", "form-field-name", "header-name");
        context.TokenStore
            .Setup(s => s.GetRequestTokensAsync(context.HttpContext))
            .Returns(Task.FromResult(tokenSet));

        var antiforgery = GetAntiforgery(context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
            () => antiforgery.ValidateRequestAsync(context.HttpContext));
        Assert.Equal(
            "The required antiforgery request token was not provided in either form field \"form-field-name\" " +
            "or header value \"header-name\".",
            exception.Message);
    }

    [Fact]
    public async Task ValidateRequestAsync_DoesNotDeserializeTwice()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature
        {
            HaveDeserializedCookieToken = true,
            CookieToken = new AntiforgeryToken(),
            HaveDeserializedRequestToken = true,
            RequestToken = new AntiforgeryToken(),
        };
        var context = CreateMockContext(new AntiforgeryOptions(), antiforgeryFeature: antiforgeryFeature);

        string? message;
        context.TokenGenerator
            .Setup(o => o.TryValidateTokenSet(
                context.HttpContext,
                antiforgeryFeature.CookieToken,
                antiforgeryFeature.RequestToken,
                out message))
            .Returns(true)
            .Verifiable();

        var antiforgery = GetAntiforgery(context);

        // Act
        await antiforgery.ValidateRequestAsync(context.HttpContext);

        // Assert (does not throw)
        context.TokenGenerator.Verify();

        // Token serializer not used.
        context.TokenSerializer.Verify(
            o => o.Deserialize(It.IsAny<string>()),
            Times.Never);
        context.TokenSerializer.Verify(
            o => o.Serialize(It.IsAny<AntiforgeryToken>()),
            Times.Never);
    }

    [Fact]
    public void SetCookieTokenAndHeader_PreserveXFrameOptionsHeader()
    {
        // Arrange
        var options = new AntiforgeryOptions();
        var antiforgeryFeature = new AntiforgeryFeature();
        var expectedHeaderValue = "DIFFERENTORIGIN";

        // Generate a new cookie.
        var context = CreateMockContext(
            options,
            useOldCookie: false,
            isOldCookieValid: false,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);
        context.HttpContext.Response.Headers["X-Frame-Options"] = expectedHeaderValue;

        // Act
        antiforgery.SetCookieTokenAndHeader(context.HttpContext);

        // Assert
        var xFrameOptions = context.HttpContext.Response.Headers["X-Frame-Options"];
        Assert.Equal(expectedHeaderValue, xFrameOptions);
    }

    [Fact]
    public void SetCookieTokenAndHeader_NewCookieToken_SetsDoNotCacheHeaders()
    {
        // Arrange
        var options = new AntiforgeryOptions();
        var antiforgeryFeature = new AntiforgeryFeature();

        // Generate a new cookie.
        var context = CreateMockContext(
            options,
            useOldCookie: false,
            isOldCookieValid: false,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        // Act
        antiforgery.SetCookieTokenAndHeader(context.HttpContext);

        // Assert
        Assert.Equal("no-cache, no-store", context.HttpContext.Response.Headers["Cache-Control"]);
        Assert.Equal("no-cache", context.HttpContext.Response.Headers["Pragma"]);
    }

    [Fact]
    public void SetCookieTokenAndHeader_ValidOldCookieToken_SetsDoNotCacheHeaders()
    {
        // Arrange
        var options = new AntiforgeryOptions();
        var antiforgeryFeature = new AntiforgeryFeature();

        // Generate a new cookie.
        var context = CreateMockContext(
            options,
            useOldCookie: true,
            isOldCookieValid: true,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        // Act
        antiforgery.SetCookieTokenAndHeader(context.HttpContext);

        // Assert
        Assert.Equal("no-cache, no-store", context.HttpContext.Response.Headers["Cache-Control"]);
        Assert.Equal("no-cache", context.HttpContext.Response.Headers["Pragma"]);
    }

    [Fact]
    public void SetCookieTokenAndHeader_OverridesExistingCachingHeaders()
    {
        // Arrange
        var options = new AntiforgeryOptions();
        var antiforgeryFeature = new AntiforgeryFeature();

        // Generate a new cookie.
        var context = CreateMockContext(
            options,
            useOldCookie: true,
            isOldCookieValid: true,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);
        context.HttpContext.Response.Headers["Cache-Control"] = "public";

        // Act
        antiforgery.SetCookieTokenAndHeader(context.HttpContext);

        // Assert
        Assert.Equal("no-cache, no-store", context.HttpContext.Response.Headers["Cache-Control"]);
        Assert.Equal("no-cache", context.HttpContext.Response.Headers["Pragma"]);
    }

    [Theory]
    [InlineData(false, "SAMEORIGIN")]
    [InlineData(true, null)]
    public void SetCookieTokenAndHeader_AddsXFrameOptionsHeader(
        bool suppressXFrameOptions,
        string expectedHeaderValue)
    {
        // Arrange
        var options = new AntiforgeryOptions()
        {
            SuppressXFrameOptionsHeader = suppressXFrameOptions
        };
        var antiforgeryFeature = new AntiforgeryFeature();

        // Generate a new cookie.
        var context = CreateMockContext(
            options,
            useOldCookie: false,
            isOldCookieValid: false,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        // Act
        antiforgery.SetCookieTokenAndHeader(context.HttpContext);

        // Assert
        var xFrameOptions = context.HttpContext.Response.Headers["X-Frame-Options"];
        Assert.Equal(expectedHeaderValue, xFrameOptions);

        Assert.NotNull(antiforgeryFeature);
        Assert.True(antiforgeryFeature.HaveDeserializedCookieToken);
        Assert.Equal(context.TestTokenSet.OldCookieToken, antiforgeryFeature.CookieToken);
        Assert.True(antiforgeryFeature.HaveGeneratedNewCookieToken);
        Assert.Equal(context.TestTokenSet.NewCookieToken, antiforgeryFeature.NewCookieToken);
        Assert.Equal(context.TestTokenSet.NewCookieTokenString, antiforgeryFeature.NewCookieTokenString);
        Assert.True(antiforgeryFeature.HaveStoredNewCookieToken);
    }

    [Fact]
    public void SetCookieTokenAndHeader_DoesNotDeserializeTwice()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature
        {
            HaveDeserializedCookieToken = true,
            HaveGeneratedNewCookieToken = true,
            NewCookieToken = new AntiforgeryToken(),
            NewCookieTokenString = "serialized-cookie-token-from-context",
            NewRequestToken = new AntiforgeryToken(),
            NewRequestTokenString = "serialized-form-token-from-context",
        };
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: true,
            isOldCookieValid: true,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        context.TokenStore
            .Setup(t => t.SaveCookieToken(context.HttpContext, "serialized-cookie-token-from-context"))
            .Verifiable();

        // Act
        antiforgery.SetCookieTokenAndHeader(context.HttpContext);

        // Assert
        // Token store used once, with expected arguments.
        // Passed context's cookie token though request's cookie token was valid.
        context.TokenStore.Verify(
            t => t.SaveCookieToken(context.HttpContext, "serialized-cookie-token-from-context"),
            Times.Once);

        // Token serializer not used.
        context.TokenSerializer.Verify(
            o => o.Deserialize(It.IsAny<string>()),
            Times.Never);
        context.TokenSerializer.Verify(
            o => o.Serialize(It.IsAny<AntiforgeryToken>()),
            Times.Never);
    }

    [Fact]
    public void SetCookieTokenAndHeader_DoesNotStoreTwice()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature
        {
            HaveDeserializedCookieToken = true,
            HaveGeneratedNewCookieToken = true,
            HaveStoredNewCookieToken = true,
            NewCookieToken = new AntiforgeryToken(),
            NewCookieTokenString = "serialized-cookie-token-from-context",
            NewRequestToken = new AntiforgeryToken(),
            NewRequestTokenString = "serialized-form-token-from-context",
        };
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: true,
            isOldCookieValid: true,
            antiforgeryFeature: antiforgeryFeature);
        var antiforgery = GetAntiforgery(context);

        // Act
        antiforgery.SetCookieTokenAndHeader(context.HttpContext);

        // Assert
        // Token serializer not used.
        context.TokenSerializer.Verify(
            o => o.Deserialize(It.IsAny<string>()),
            Times.Never);
        context.TokenSerializer.Verify(
            o => o.Serialize(It.IsAny<AntiforgeryToken>()),
            Times.Never);

        // Token store not used.
        context.TokenStore.Verify(
            t => t.SaveCookieToken(It.IsAny<HttpContext>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void SetCookieTokenAndHeader_NullCookieToken()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature
        {
            HaveDeserializedCookieToken = false,
            HaveGeneratedNewCookieToken = false,
            HaveStoredNewCookieToken = true,
            NewCookieToken = new AntiforgeryToken(),
            NewCookieTokenString = "serialized-cookie-token-from-context",
            NewRequestToken = new AntiforgeryToken(),
            NewRequestTokenString = "serialized-form-token-from-context",
        };
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: false,
            isOldCookieValid: false,
            antiforgeryFeature: antiforgeryFeature);
        var testTokenSet = new TestTokenSet
        {
            OldCookieTokenString = null!
        };

        var nullTokenStore = GetTokenStore(context.HttpContext, testTokenSet, false);
        var antiforgery = GetAntiforgery(
            context.HttpContext,
            tokenGenerator: context.TokenGenerator.Object,
            tokenStore: nullTokenStore.Object);

        // Act
        antiforgery.SetCookieTokenAndHeader(context.HttpContext);

        // Assert
        context.TokenSerializer.Verify(s => s.Deserialize(null!), Times.Never);
    }

    [Fact]
    public void SetCookieTokenAndHeader_DoesNotModifyHeadersAfterResponseHasStarted()
    {
        // Arrange
        var antiforgeryFeature = new AntiforgeryFeature
        {
            HaveDeserializedCookieToken = false,
            HaveGeneratedNewCookieToken = false,
            HaveStoredNewCookieToken = true,
            NewCookieToken = new AntiforgeryToken(),
            NewCookieTokenString = "serialized-cookie-token-from-context",
            NewRequestToken = new AntiforgeryToken(),
            NewRequestTokenString = "serialized-form-token-from-context",
        };
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: false,
            isOldCookieValid: false,
            antiforgeryFeature: antiforgeryFeature);
        var testTokenSet = new TestTokenSet
        {
            OldCookieTokenString = null!
        };

        var nullTokenStore = GetTokenStore(context.HttpContext, testTokenSet, false);
        var antiforgery = GetAntiforgery(
            context.HttpContext,
            tokenGenerator: context.TokenGenerator.Object,
            tokenStore: nullTokenStore.Object);

        TestResponseFeature testResponse = new TestResponseFeature();
        context.HttpContext.Features.Set<IHttpResponseFeature>(testResponse);
        context.HttpContext.Response.Headers["Cache-Control"] = "public";
        testResponse.StartResponse();

        // Act
        antiforgery.SetCookieTokenAndHeader(context.HttpContext);

        Assert.Equal("public", context.HttpContext.Response.Headers["Cache-Control"]);
    }

    [Fact]
    public void GetAndStoreTokens_DoesNotLogWarning_IfNoExistingCacheHeadersPresent()
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(lf => lf.CreateLogger(typeof(DefaultAntiforgery).FullName!))
            .Returns(new TestLogger("test logger", testSink, enabled: true));
        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory.Object);
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: false,
            isOldCookieValid: false,
            antiforgeryFeature: antiforgeryFeature);
        context.HttpContext.RequestServices = services.BuildServiceProvider();
        var antiforgery = GetAntiforgery(context);

        // Act
        var tokenSet = antiforgery.GetAndStoreTokens(context.HttpContext);

        // Assert
        var hasWarningMessage = testSink.Writes
            .Where(wc => wc.LogLevel == LogLevel.Warning)
            .Select(wc => wc.State?.ToString())
            .Contains(ResponseCacheHeadersOverrideWarningMessage);
        Assert.False(hasWarningMessage);
    }

    [Theory]
    [InlineData("Cache-Control", "Public")]
    [InlineData("Cache-Control", "PuBlic")]
    [InlineData("Cache-Control", "Private")]
    [InlineData("Cache-Control", "PriVate")]
    [InlineData("Cache-Control", "No-Store")]
    [InlineData("Cache-Control", "No-store")]
    [InlineData("Pragma", "Foo")]
    public void GetAndStoreTokens_LogsWarning_NonNoCacheHeadersAlreadyPresent(string headerName, string headerValue)
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(lf => lf.CreateLogger(typeof(DefaultAntiforgery).FullName!))
            .Returns(new TestLogger("test logger", testSink, enabled: true));
        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory.Object);
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: false,
            isOldCookieValid: false,
            antiforgeryFeature: antiforgeryFeature);
        context.HttpContext.RequestServices = services.BuildServiceProvider();
        var antiforgery = GetAntiforgery(context);
        context.HttpContext.Response.Headers[headerName] = headerValue;

        // Act
        var tokenSet = antiforgery.GetAndStoreTokens(context.HttpContext);

        // Assert
        var hasWarningMessage = testSink.Writes
            .Where(wc => wc.LogLevel == LogLevel.Warning)
            .Select(wc => wc.State?.ToString())
            .Contains(ResponseCacheHeadersOverrideWarningMessage);
        Assert.True(hasWarningMessage);
    }

    [Theory]
    [InlineData("Cache-Control", "no-cache, no-store")]
    [InlineData("Pragma", "no-cache")]
    public void GetAndStoreTokens_DoesNotLogsWarning_ForNoCacheHeaders_AlreadyPresent(string headerName, string headerValue)
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(lf => lf.CreateLogger(typeof(DefaultAntiforgery).FullName!))
            .Returns(new TestLogger("test logger", testSink, enabled: true));
        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory.Object);
        var antiforgeryFeature = new AntiforgeryFeature();
        var context = CreateMockContext(
            new AntiforgeryOptions(),
            useOldCookie: false,
            isOldCookieValid: false,
            antiforgeryFeature: antiforgeryFeature);
        context.HttpContext.RequestServices = services.BuildServiceProvider();
        var antiforgery = GetAntiforgery(context);
        context.HttpContext.Response.Headers[headerName] = headerValue;

        // Act
        var tokenSet = antiforgery.GetAndStoreTokens(context.HttpContext);

        // Assert
        var hasWarningMessage = testSink.Writes
            .Where(wc => wc.LogLevel == LogLevel.Warning)
            .Select(wc => wc.State?.ToString())
            .Contains(ResponseCacheHeadersOverrideWarningMessage);
        Assert.False(hasWarningMessage);
    }

    private DefaultAntiforgery GetAntiforgery(
        HttpContext httpContext,
        AntiforgeryOptions? options = null,
        IAntiforgeryTokenGenerator? tokenGenerator = null,
        IAntiforgeryTokenSerializer? tokenSerializer = null,
        IAntiforgeryTokenStore? tokenStore = null)
    {
        var optionsManager = new TestOptionsManager();
        if (options != null)
        {
            optionsManager.Value = options;
        }

        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        return new DefaultAntiforgery(
            antiforgeryOptionsAccessor: optionsManager,
            tokenGenerator: tokenGenerator!,
            tokenSerializer: tokenSerializer!,
            tokenStore: tokenStore!,
            loggerFactory: loggerFactory);
    }

    private IServiceProvider GetServices()
    {
        var builder = new ServiceCollection();
        builder.AddSingleton<ILoggerFactory>(new LoggerFactory());

        return builder.BuildServiceProvider();
    }

    private HttpContext GetHttpContext(IAntiforgeryFeature? antiforgeryFeature = null)
    {
        var httpContext = new DefaultHttpContext();
        antiforgeryFeature = antiforgeryFeature ?? new AntiforgeryFeature();
        httpContext.Features.Set(antiforgeryFeature);
        httpContext.RequestServices = GetServices();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity("some-auth"));

        return httpContext;
    }

    private DefaultAntiforgery GetAntiforgery(AntiforgeryMockContext context)
    {
        return GetAntiforgery(
            context.HttpContext,
            context.Options,
            context.TokenGenerator?.Object,
            context.TokenSerializer?.Object,
            context.TokenStore?.Object);
    }

    private Mock<IAntiforgeryTokenStore> GetTokenStore(
        HttpContext context,
        TestTokenSet testTokenSet,
        bool saveNewCookie = true)
    {
        var oldCookieToken = testTokenSet.OldCookieTokenString;
        var formToken = testTokenSet.FormTokenString;
        var mockTokenStore = new Mock<IAntiforgeryTokenStore>(MockBehavior.Strict);
        mockTokenStore
            .Setup(o => o.GetCookieToken(context))
            .Returns(oldCookieToken);

        mockTokenStore
            .Setup(o => o.GetRequestTokensAsync(context))
            .Returns(() => Task.FromResult(new AntiforgeryTokenSet(
                formToken,
                oldCookieToken,
                "form",
                "header")));

        if (saveNewCookie)
        {
            var newCookieToken = testTokenSet.NewCookieTokenString;
            mockTokenStore
                .Setup(o => o.SaveCookieToken(context, newCookieToken))
                .Verifiable();
        }

        return mockTokenStore;
    }

    private Mock<IAntiforgeryTokenSerializer> GetTokenSerializer(TestTokenSet testTokenSet)
    {
        var oldCookieToken = testTokenSet.OldCookieToken;
        var newCookieToken = testTokenSet.NewCookieToken;
        var formToken = testTokenSet.RequestToken;
        var mockSerializer = new Mock<IAntiforgeryTokenSerializer>(MockBehavior.Strict);
        mockSerializer.Setup(o => o.Serialize(formToken))
                      .Returns(testTokenSet.FormTokenString);
        mockSerializer.Setup(o => o.Deserialize(testTokenSet.FormTokenString))
                      .Returns(formToken);
        mockSerializer.Setup(o => o.Deserialize(testTokenSet.OldCookieTokenString))
                      .Returns(oldCookieToken);
        mockSerializer.Setup(o => o.Serialize(oldCookieToken))
                      .Returns(testTokenSet.OldCookieTokenString);
        mockSerializer.Setup(o => o.Serialize(newCookieToken))
                      .Returns(testTokenSet.NewCookieTokenString);
        return mockSerializer;
    }

    private AntiforgeryMockContext CreateMockContext(
        AntiforgeryOptions options,
        bool useOldCookie = false,
        bool isOldCookieValid = true,
        IAntiforgeryFeature? antiforgeryFeature = null)
    {
        // Arrange
        var httpContext = GetHttpContext(antiforgeryFeature);
        var testTokenSet = GetTokenSet();

        var mockSerializer = GetTokenSerializer(testTokenSet);

        var mockTokenStore = GetTokenStore(httpContext, testTokenSet, !useOldCookie);

        var mockGenerator = new Mock<IAntiforgeryTokenGenerator>(MockBehavior.Strict);
        mockGenerator
            .Setup(o => o.GenerateRequestToken(
                httpContext,
                useOldCookie ? testTokenSet.OldCookieToken : testTokenSet.NewCookieToken))
            .Returns(testTokenSet.RequestToken);

        mockGenerator
            .Setup(o => o.GenerateCookieToken())
            .Returns(useOldCookie ? testTokenSet.OldCookieToken : testTokenSet.NewCookieToken);
        mockGenerator
            .Setup(o => o.IsCookieTokenValid(null))
            .Returns(false);
        mockGenerator
            .Setup(o => o.IsCookieTokenValid(testTokenSet.OldCookieToken))
            .Returns(isOldCookieValid);

        mockGenerator
            .Setup(o => o.IsCookieTokenValid(testTokenSet.NewCookieToken))
            .Returns(!isOldCookieValid);

        return new AntiforgeryMockContext()
        {
            Options = options,
            HttpContext = httpContext,
            TokenGenerator = mockGenerator,
            TokenSerializer = mockSerializer,
            TokenStore = mockTokenStore,
            TestTokenSet = testTokenSet
        };
    }

    private TestTokenSet GetTokenSet()
    {
        return new TestTokenSet()
        {
            RequestToken = new AntiforgeryToken() { IsCookieToken = false },
            FormTokenString = "serialized-form-token",
            OldCookieToken = new AntiforgeryToken() { IsCookieToken = true },
            OldCookieTokenString = "serialized-old-cookie-token",
            NewCookieToken = new AntiforgeryToken() { IsCookieToken = true },
            NewCookieTokenString = "serialized-new-cookie-token",
        };
    }

    private class TestTokenSet
    {
        public AntiforgeryToken RequestToken { get; set; } = default!;

        public string FormTokenString { get; set; } = default!;

        public AntiforgeryToken OldCookieToken { get; set; } = default!;

        public string OldCookieTokenString { get; set; } = default!;

        public AntiforgeryToken NewCookieToken { get; set; } = default!;

        public string NewCookieTokenString { get; set; } = default!;
    }

    private class AntiforgeryMockContext
    {
        public AntiforgeryOptions Options { get; set; } = default!;

        public TestTokenSet TestTokenSet { get; set; } = default!;

        public HttpContext HttpContext { get; set; } = default!;

        public Mock<IAntiforgeryTokenGenerator> TokenGenerator { get; set; } = default!;

        public Mock<IAntiforgeryTokenStore> TokenStore { get; set; } = default!;

        public Mock<IAntiforgeryTokenSerializer> TokenSerializer { get; set; } = default!;
    }

    private class TestOptionsManager : IOptions<AntiforgeryOptions>
    {
        public AntiforgeryOptions Value { get; set; } = new AntiforgeryOptions();
    }

    private class TestResponseFeature : HttpResponseFeature
    {
        private bool _hasStarted = false;

        public override bool HasStarted { get => _hasStarted; }

        public TestResponseFeature()
        {
        }

        public void StartResponse()
        {
            _hasStarted = true;
        }
    }
}
