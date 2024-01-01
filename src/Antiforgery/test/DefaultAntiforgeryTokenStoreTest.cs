// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

public class DefaultAntiforgeryTokenStoreTest
{
    private readonly string _cookieName = "cookie-name";

    [Fact]
    public void GetCookieToken_CookieDoesNotExist_ReturnsNull()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var options = new AntiforgeryOptions
        {
            Cookie = { Name = _cookieName }
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        var token = tokenStore.GetCookieToken(httpContext);

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public void GetCookieToken_CookieIsEmpty_ReturnsNull()
    {
        // Arrange
        var httpContext = GetHttpContext(_cookieName, string.Empty);
        var options = new AntiforgeryOptions
        {
            Cookie = { Name = _cookieName }
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        var token = tokenStore.GetCookieToken(httpContext);

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public void GetCookieToken_CookieIsNotEmpty_ReturnsToken()
    {
        // Arrange
        var expectedToken = "valid-value";
        var httpContext = GetHttpContext(_cookieName, expectedToken);

        var options = new AntiforgeryOptions
        {
            Cookie = { Name = _cookieName }
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        var token = tokenStore.GetCookieToken(httpContext);

        // Assert
        Assert.Equal(expectedToken, token);
    }

    [Fact]
    public async Task GetRequestTokens_CookieIsEmpty_ReturnsNullTokens()
    {
        // Arrange
        var httpContext = GetHttpContext();
        httpContext.Request.Form = FormCollection.Empty;

        var options = new AntiforgeryOptions
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        var tokenSet = await tokenStore.GetRequestTokensAsync(httpContext);

        // Assert
        Assert.Null(tokenSet.CookieToken);
        Assert.Null(tokenSet.RequestToken);
    }

    [Fact]
    public async Task GetRequestTokens_HeaderTokenTakensPriority_OverFormToken()
    {
        // Arrange
        var httpContext = GetHttpContext("cookie-name", "cookie-value");
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                { "form-field-name", "form-value" },
            }); // header value has priority.
        httpContext.Request.Headers.Add("header-name", "header-value");

        var options = new AntiforgeryOptions
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = "header-name",
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        var tokens = await tokenStore.GetRequestTokensAsync(httpContext);

        // Assert
        Assert.Equal("cookie-value", tokens.CookieToken);
        Assert.Equal("header-value", tokens.RequestToken);
    }

    [Fact]
    public async Task GetRequestTokens_FormTokenDisabled_ReturnsNullToken()
    {
        // Arrange
        var httpContext = GetHttpContext("cookie-name", "cookie-value");
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                { "form-field-name", "form-value" },
            });

        var options = new AntiforgeryOptions
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = "header-name",
            SuppressReadingTokenFromFormBody = true
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        var tokens = await tokenStore.GetRequestTokensAsync(httpContext);

        // Assert
        Assert.Equal("cookie-value", tokens.CookieToken);
        Assert.Null(tokens.RequestToken);
    }

    [Fact]
    public async Task GetRequestTokens_FormTokenDisabled_ReturnsHeaderToken()
    {
        // Arrange
        var httpContext = GetHttpContext("cookie-name", "cookie-value");
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                { "form-field-name", "form-value" },
            });
        httpContext.Request.Headers.Add("header-name", "header-value");

        var options = new AntiforgeryOptions
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = "header-name",
            SuppressReadingTokenFromFormBody = true
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        var tokens = await tokenStore.GetRequestTokensAsync(httpContext);

        // Assert
        Assert.Equal("cookie-value", tokens.CookieToken);
        Assert.Equal("header-value", tokens.RequestToken);
    }

    [Fact]
    public async Task GetRequestTokens_NoHeaderToken_FallsBackToFormToken()
    {
        // Arrange
        var httpContext = GetHttpContext("cookie-name", "cookie-value");
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                { "form-field-name", "form-value" },
            });

        var options = new AntiforgeryOptions
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = "header-name",
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        var tokens = await tokenStore.GetRequestTokensAsync(httpContext);

        // Assert
        Assert.Equal("cookie-value", tokens.CookieToken);
        Assert.Equal("form-value", tokens.RequestToken);
    }

    [Fact]
    public async Task GetRequestTokens_NonFormContentType_UsesHeaderToken()
    {
        // Arrange
        var httpContext = GetHttpContext("cookie-name", "cookie-value");
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.Headers.Add("header-name", "header-value");

        // Will not be accessed
        httpContext.Request.Form = null!;

        var options = new AntiforgeryOptions
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = "header-name",
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        var tokens = await tokenStore.GetRequestTokensAsync(httpContext);

        // Assert
        Assert.Equal("cookie-value", tokens.CookieToken);
        Assert.Equal("header-value", tokens.RequestToken);
    }

    [Fact]
    public async Task GetRequestTokens_NoHeaderToken_NonFormContentType_ReturnsNullToken()
    {
        // Arrange
        var httpContext = GetHttpContext("cookie-name", "cookie-value");
        httpContext.Request.ContentType = "application/json";

        // Will not be accessed
        httpContext.Request.Form = null!;

        var options = new AntiforgeryOptions
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = "header-name",
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        var tokenSet = await tokenStore.GetRequestTokensAsync(httpContext);

        // Assert
        Assert.Equal("cookie-value", tokenSet.CookieToken);
        Assert.Null(tokenSet.RequestToken);
    }

    [Fact]
    public async Task GetRequestTokens_BothHeaderValueAndFormFieldsEmpty_ReturnsNullTokens()
    {
        // Arrange
        var httpContext = GetHttpContext("cookie-name", "cookie-value");
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = FormCollection.Empty;

        var options = new AntiforgeryOptions
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = "header-name",
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        var tokenSet = await tokenStore.GetRequestTokensAsync(httpContext);

        // Assert
        Assert.Equal("cookie-value", tokenSet.CookieToken);
        Assert.Null(tokenSet.RequestToken);
    }

    [Fact]
    public async Task GetRequestTokens_ReadFormAsyncThrowsIOException_ThrowsAntiforgeryValidationException()
    {
        // Arrange
        var ioException = new IOException();
        var httpContext = new Mock<HttpContext>();

        httpContext.Setup(r => r.Request.Cookies).Returns(Mock.Of<IRequestCookieCollection>());
        httpContext.SetupGet(r => r.Request.HasFormContentType).Returns(true);
        httpContext.Setup(r => r.Request.ReadFormAsync(It.IsAny<CancellationToken>())).Throws(ioException);

        var options = new AntiforgeryOptions
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = null,
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AntiforgeryValidationException>(() => tokenStore.GetRequestTokensAsync(httpContext.Object));
        Assert.Same(ioException, ex.InnerException);
    }

    [Fact]
    public async Task GetRequestTokens_ReadFormAsyncThrowsInvalidDataException_ThrowsAntiforgeryValidationException()
    {
        // Arrange
        var exception = new InvalidDataException();
        var httpContext = new Mock<HttpContext>();

        httpContext.Setup(r => r.Request.Cookies).Returns(Mock.Of<IRequestCookieCollection>());
        httpContext.SetupGet(r => r.Request.HasFormContentType).Returns(true);
        httpContext.Setup(r => r.Request.ReadFormAsync(It.IsAny<CancellationToken>())).Throws(exception);

        var options = new AntiforgeryOptions
        {
            Cookie = { Name = "cookie-name" },
            FormFieldName = "form-field-name",
            HeaderName = null,
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AntiforgeryValidationException>(() => tokenStore.GetRequestTokensAsync(httpContext.Object));
        Assert.Same(exception, ex.InnerException);
    }

    [Theory]
    [InlineData(false, CookieSecurePolicy.SameAsRequest, null)]
    [InlineData(true, CookieSecurePolicy.SameAsRequest, true)]
    [InlineData(false, CookieSecurePolicy.Always, true)]
    [InlineData(true, CookieSecurePolicy.Always, true)]
    [InlineData(false, CookieSecurePolicy.None, null)]
    [InlineData(true, CookieSecurePolicy.None, null)]
    public void SaveCookieToken_HonorsCookieSecurePolicy_OnOptions(
        bool isRequestSecure,
        CookieSecurePolicy policy,
        bool? expectedCookieSecureFlag)
    {
        // Arrange
        var token = "serialized-value";
        bool defaultCookieSecureValue = expectedCookieSecureFlag ?? false; // pulled from config; set by ctor
        var cookies = new MockResponseCookieCollection();

        var httpContext = new Mock<HttpContext>();
        httpContext
            .Setup(hc => hc.Request.IsHttps)
            .Returns(isRequestSecure);
        httpContext
            .Setup(o => o.Response.Cookies)
            .Returns(cookies);
        httpContext
            .SetupGet(hc => hc.Request.PathBase)
            .Returns("/");

        var options = new AntiforgeryOptions()
        {
            Cookie =
                {
                    Name = _cookieName,
                    SecurePolicy = policy
                },
        };

        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        tokenStore.SaveCookieToken(httpContext.Object, token);

        // Assert
        Assert.Equal(1, cookies.Count);
        Assert.NotNull(cookies);
        Assert.Equal(_cookieName, cookies.Key);
        Assert.Equal("serialized-value", cookies.Value);
        Assert.True(cookies.Options!.HttpOnly);
        Assert.Equal(defaultCookieSecureValue, cookies.Options.Secure);
    }

    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("/", "/")]
    [InlineData("/vdir1", "/vdir1")]
    [InlineData("/vdir1/vdir2", "/vdir1/vdir2")]
    public void SaveCookieToken_SetsCookieWithApproriatePathBase(string requestPathBase, string expectedCookiePath)
    {
        // Arrange
        var token = "serialized-value";
        var cookies = new MockResponseCookieCollection();
        var httpContext = new Mock<HttpContext>();
        httpContext
            .Setup(hc => hc.Response.Cookies)
            .Returns(cookies);
        httpContext
            .SetupGet(hc => hc.Request.PathBase)
            .Returns(requestPathBase);
        httpContext
            .SetupGet(hc => hc.Request.Path)
            .Returns("/index.html");
        var options = new AntiforgeryOptions
        {
            Cookie = { Name = _cookieName }
        };
        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        tokenStore.SaveCookieToken(httpContext.Object, token);

        // Assert
        Assert.Equal(1, cookies.Count);
        Assert.NotNull(cookies);
        Assert.Equal(_cookieName, cookies.Key);
        Assert.Equal("serialized-value", cookies.Value);
        Assert.True(cookies.Options!.HttpOnly);
        Assert.Equal(expectedCookiePath, cookies.Options.Path);
    }

    [Fact]
    public void SaveCookieToken_NonNullAntiforgeryOptionsConfigureCookieOptionsPath_UsesCookieOptionsPath()
    {
        // Arrange
        var expectedCookiePath = "/";
        var requestPathBase = "/vdir1";
        var token = "serialized-value";
        var cookies = new MockResponseCookieCollection();
        var httpContext = new Mock<HttpContext>();
        httpContext
            .Setup(hc => hc.Response.Cookies)
            .Returns(cookies);
        httpContext
            .SetupGet(hc => hc.Request.PathBase)
            .Returns(requestPathBase);
        httpContext
            .SetupGet(hc => hc.Request.Path)
            .Returns("/index.html");
        var options = new AntiforgeryOptions
        {
            Cookie =
                {
                    Name = _cookieName,
                    Path = expectedCookiePath
                }
        };
        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        tokenStore.SaveCookieToken(httpContext.Object, token);

        // Assert
        Assert.Equal(1, cookies.Count);
        Assert.NotNull(cookies);
        Assert.Equal(_cookieName, cookies.Key);
        Assert.Equal("serialized-value", cookies.Value);
        Assert.True(cookies.Options!.HttpOnly);
        Assert.Equal(expectedCookiePath, cookies.Options.Path);
    }

    [Fact]
    public void SaveCookieToken_NonNullAntiforgeryOptionsConfigureCookieOptionsDomain_UsesCookieOptionsDomain()
    {
        // Arrange
        var expectedCookieDomain = "microsoft.com";
        var token = "serialized-value";
        var cookies = new MockResponseCookieCollection();
        var httpContext = new Mock<HttpContext>();
        httpContext
            .Setup(hc => hc.Response.Cookies)
            .Returns(cookies);
        httpContext
            .SetupGet(hc => hc.Request.PathBase)
            .Returns("/vdir1");
        httpContext
            .SetupGet(hc => hc.Request.Path)
            .Returns("/index.html");
        var options = new AntiforgeryOptions
        {
            Cookie =
                {
                    Name = _cookieName,
                    Domain = expectedCookieDomain
                }
        };
        var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

        // Act
        tokenStore.SaveCookieToken(httpContext.Object, token);

        // Assert
        Assert.Equal(1, cookies.Count);
        Assert.NotNull(cookies);
        Assert.Equal(_cookieName, cookies.Key);
        Assert.Equal("serialized-value", cookies.Value);
        Assert.True(cookies.Options!.HttpOnly);
        Assert.Equal("/vdir1", cookies.Options.Path);
        Assert.Equal(expectedCookieDomain, cookies.Options.Domain);
    }

    private HttpContext GetHttpContext(string cookieName, string cookieValue)
    {
        var context = GetHttpContext();
        context.Request.Headers.Cookie = $"{cookieName}={cookieValue}";
        return context;
    }

    private HttpContext GetHttpContext()
    {
        var httpContext = new DefaultHttpContext();

        return httpContext;
    }

    private class MockResponseCookieCollection : IResponseCookies
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
        public CookieOptions? Options { get; set; }
        public int Count { get; set; }

        public void Append(string key, string value, CookieOptions options)
        {
            Key = key;
            Value = value;
            Options = options;
            Count++;
        }

        public void Append(string key, string value)
        {
            throw new NotImplementedException();
        }

        public void Delete(string key, CookieOptions options)
        {
            throw new NotImplementedException();
        }

        public void Delete(string key)
        {
            throw new NotImplementedException();
        }
    }
}
