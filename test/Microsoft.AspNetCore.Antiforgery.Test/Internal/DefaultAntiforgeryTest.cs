// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public class DefaultAntiforgeryTest
    {
        public static TheoryData<string> SafeHttpMethods => new TheoryData<string>()
        {
            "GeT",
            "HEAD",
            "options",
            "TrAcE",
        };

        public static TheoryData<string> UnsafeHttpMethods => new TheoryData<string>()
        {
            "PUT",
            "post",
            "Delete",
            "Custom",
        };

        [Fact]
        public async Task ChecksSSL_ValidateRequestAsync_Throws()
        {
            // Arrange
            var httpContext = GetHttpContext();
            var options = new AntiforgeryOptions()
            {
                RequireSsl = true
            };
            var antiforgery = GetAntiforgery(httpContext, options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => antiforgery.ValidateRequestAsync(httpContext));
            Assert.Equal(
                @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
                "but the current request is not an SSL request.",
                exception.Message);
        }

        [Fact]
        public async Task ChecksSSL_ValidateRequestAsync_WithPrincipal_Throws()
        {
            // Arrange
            var httpContext = GetHttpContext();
            var options = new AntiforgeryOptions()
            {
                RequireSsl = true
            };
            var antiforgery = GetAntiforgery(httpContext, options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => antiforgery.ValidateRequestAsync(httpContext, new ClaimsPrincipal()));
            Assert.Equal(
                @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
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
                RequireSsl = true
            };

            var antiforgery = GetAntiforgery(httpContext, options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => antiforgery.IsRequestValidAsync(httpContext));
            Assert.Equal(
                @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
                "but the current request is not an SSL request.",
                exception.Message);
        }

        [Fact]
        public async Task ChecksSSL_IsRequestValidAsync_WithPrincipal_Throws()
        {
            // Arrange
            var httpContext = GetHttpContext();
            var options = new AntiforgeryOptions()
            {
                RequireSsl = true
            };

            var antiforgery = GetAntiforgery(httpContext, options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => antiforgery.IsRequestValidAsync(httpContext, new ClaimsPrincipal()));
            Assert.Equal(
                @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
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
                RequireSsl = true
            };

            var antiforgery = GetAntiforgery(httpContext, options);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => antiforgery.GetAndStoreTokens(httpContext));
            Assert.Equal(
                 @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
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
                RequireSsl = true
            };

            var antiforgery = GetAntiforgery(httpContext, options);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => antiforgery.GetTokens(httpContext));
            Assert.Equal(
                 @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
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
                RequireSsl = true
            };

            var antiforgery = GetAntiforgery(httpContext, options);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => antiforgery.SetCookieTokenAndHeader(httpContext));
            Assert.Equal(
                 @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
                 "but the current request is not an SSL request.",
                 exception.Message);
        }

        [Fact]
        public void GetTokens_ExistingInvalidCookieToken_GeneratesANewCookieTokenAndANewFormToken()
        {
            // Arrange
            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            // Generate a new cookie.
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: false,
                isOldCookieValid: false,
                contextAccessor: contextAccessor);
            var antiforgery = GetAntiforgery(context);

            // Act
            var tokenset = antiforgery.GetTokens(context.HttpContext);

            // Assert
            Assert.Equal(context.TestTokenSet.NewCookieTokenString, tokenset.CookieToken);
            Assert.Equal(context.TestTokenSet.FormTokenString, tokenset.RequestToken);

            Assert.NotNull(contextAccessor.Value);
            Assert.True(contextAccessor.Value.HaveDeserializedCookieToken);
            Assert.Equal(context.TestTokenSet.OldCookieToken, contextAccessor.Value.CookieToken);
            Assert.True(contextAccessor.Value.HaveGeneratedNewCookieToken);
            Assert.Equal(context.TestTokenSet.NewCookieToken, contextAccessor.Value.NewCookieToken);
            Assert.Equal(context.TestTokenSet.NewCookieTokenString, contextAccessor.Value.NewCookieTokenString);
            Assert.Equal(context.TestTokenSet.RequestToken, contextAccessor.Value.NewRequestToken);
            Assert.Equal(context.TestTokenSet.FormTokenString, contextAccessor.Value.NewRequestTokenString);
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
            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: true,
                isOldCookieValid: true,
                contextAccessor: contextAccessor);
            var antiforgery = GetAntiforgery(context);

            // Act
            var tokenset = antiforgery.GetTokens(context.HttpContext);

            // Assert
            Assert.Null(tokenset.CookieToken);
            Assert.Equal(context.TestTokenSet.FormTokenString, tokenset.RequestToken);

            Assert.NotNull(contextAccessor.Value);
            Assert.True(contextAccessor.Value.HaveDeserializedCookieToken);
            Assert.Equal(context.TestTokenSet.OldCookieToken, contextAccessor.Value.CookieToken);
            Assert.True(contextAccessor.Value.HaveGeneratedNewCookieToken);
            Assert.Null(contextAccessor.Value.NewCookieToken);
            Assert.Equal(context.TestTokenSet.RequestToken, contextAccessor.Value.NewRequestToken);
            Assert.Equal(context.TestTokenSet.FormTokenString, contextAccessor.Value.NewRequestTokenString);
        }

        [Fact]
        public void GetTokens_DoesNotSerializeTwice()
        {
            // Arrange
            var contextAccessor = new DefaultAntiforgeryContextAccessor
            {
                Value = new AntiforgeryContext
                {
                    HaveDeserializedCookieToken = true,
                    HaveGeneratedNewCookieToken = true,
                    NewRequestToken = new AntiforgeryToken(),
                    NewRequestTokenString = "serialized-form-token-from-context",
                },
            };
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: true,
                isOldCookieValid: true,
                contextAccessor: contextAccessor);

            var antiforgery = GetAntiforgery(context);

            // Act
            var tokenset = antiforgery.GetTokens(context.HttpContext);

            // Assert
            Assert.Null(tokenset.CookieToken);
            Assert.Equal("serialized-form-token-from-context", tokenset.RequestToken);

            Assert.Null(contextAccessor.Value.NewCookieToken);

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
            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: true,
                isOldCookieValid: true,
                contextAccessor: contextAccessor);
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

            Assert.NotNull(contextAccessor.Value);
            Assert.True(contextAccessor.Value.HaveDeserializedCookieToken);
            Assert.Equal(context.TestTokenSet.OldCookieToken, contextAccessor.Value.CookieToken);
            Assert.True(contextAccessor.Value.HaveGeneratedNewCookieToken);
            Assert.Null(contextAccessor.Value.NewCookieToken);
            Assert.Equal(context.TestTokenSet.RequestToken, contextAccessor.Value.NewRequestToken);
            Assert.Equal(context.TestTokenSet.FormTokenString, contextAccessor.Value.NewRequestTokenString);
        }

        [Fact]
        public void GetAndStoreTokens_NoExistingCookieToken_Saved()
        {
            // Arrange
            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: false,
                isOldCookieValid: false,
                contextAccessor: contextAccessor);
            var antiforgery = GetAntiforgery(context);

            // Act
            var tokenSet = antiforgery.GetAndStoreTokens(context.HttpContext);

            // Assert
            context.TokenStore.Verify(
                t => t.SaveCookieToken(It.IsAny<HttpContext>(), context.TestTokenSet.NewCookieTokenString),
                Times.Once);

            Assert.Equal(context.TestTokenSet.NewCookieTokenString, tokenSet.CookieToken);
            Assert.Equal(context.TestTokenSet.FormTokenString, tokenSet.RequestToken);

            Assert.NotNull(contextAccessor.Value);
            Assert.True(contextAccessor.Value.HaveDeserializedCookieToken);
            Assert.Equal(context.TestTokenSet.OldCookieToken, contextAccessor.Value.CookieToken);
            Assert.True(contextAccessor.Value.HaveGeneratedNewCookieToken);
            Assert.Equal(context.TestTokenSet.NewCookieToken, contextAccessor.Value.NewCookieToken);
            Assert.Equal(context.TestTokenSet.NewCookieTokenString, contextAccessor.Value.NewCookieTokenString);
            Assert.Equal(context.TestTokenSet.RequestToken, contextAccessor.Value.NewRequestToken);
            Assert.Equal(context.TestTokenSet.FormTokenString, contextAccessor.Value.NewRequestTokenString);
            Assert.True(contextAccessor.Value.HaveStoredNewCookieToken);
        }

        [Fact]
        public void GetAndStoreTokens_DoesNotSerializeTwice()
        {
            // Arrange
            var contextAccessor = new DefaultAntiforgeryContextAccessor
            {
                Value = new AntiforgeryContext
                {
                    HaveDeserializedCookieToken = true,
                    HaveGeneratedNewCookieToken = true,
                    NewCookieToken = new AntiforgeryToken(),
                    NewCookieTokenString = "serialized-cookie-token-from-context",
                    NewRequestToken = new AntiforgeryToken(),
                    NewRequestTokenString = "serialized-form-token-from-context",
                },
            };
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: true,
                isOldCookieValid: true,
                contextAccessor: contextAccessor);
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

            Assert.True(contextAccessor.Value.HaveStoredNewCookieToken);
        }

        [Fact]
        public void GetAndStoreTokens_DoesNotStoreTwice()
        {
            // Arrange
            var contextAccessor = new DefaultAntiforgeryContextAccessor
            {
                Value = new AntiforgeryContext
                {
                    HaveDeserializedCookieToken = true,
                    HaveGeneratedNewCookieToken = true,
                    HaveStoredNewCookieToken = true,
                    NewCookieToken = new AntiforgeryToken(),
                    NewCookieTokenString = "serialized-cookie-token-from-context",
                    NewRequestToken = new AntiforgeryToken(),
                    NewRequestTokenString = "serialized-form-token-from-context",
                },
            };
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: true,
                isOldCookieValid: true,
                contextAccessor: contextAccessor);
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
            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            var context = CreateMockContext(new AntiforgeryOptions(), contextAccessor: contextAccessor);

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    It.IsAny<ClaimsPrincipal>(),
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
            Assert.NotNull(contextAccessor.Value);
            Assert.True(contextAccessor.Value.HaveDeserializedCookieToken);
            Assert.Equal(context.TestTokenSet.OldCookieToken, contextAccessor.Value.CookieToken);
            Assert.True(contextAccessor.Value.HaveDeserializedRequestToken);
            Assert.Equal(context.TestTokenSet.RequestToken, contextAccessor.Value.RequestToken);
        }

        [Fact]
        public async Task IsRequestValidAsync_FromStore_Success()
        {
            // Arrange
            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            var context = CreateMockContext(new AntiforgeryOptions(), contextAccessor: contextAccessor);
            context.HttpContext.Request.Method = "POST";

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    It.IsAny<ClaimsPrincipal>(),
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

            Assert.NotNull(contextAccessor.Value);
            Assert.True(contextAccessor.Value.HaveDeserializedCookieToken);
            Assert.Equal(context.TestTokenSet.OldCookieToken, contextAccessor.Value.CookieToken);
            Assert.True(contextAccessor.Value.HaveDeserializedRequestToken);
            Assert.Equal(context.TestTokenSet.RequestToken, contextAccessor.Value.RequestToken);
        }

        [Fact]
        public async Task IsRequestValidAsync_DoesNotDeserializeTwice()
        {
            // Arrange
            var contextAccessor = new DefaultAntiforgeryContextAccessor
            {
                Value = new AntiforgeryContext
                {
                    HaveDeserializedCookieToken = true,
                    CookieToken = new AntiforgeryToken(),
                    HaveDeserializedRequestToken = true,
                    RequestToken = new AntiforgeryToken(),
                },
            };
            var context = CreateMockContext(new AntiforgeryOptions(), contextAccessor: contextAccessor);
            context.HttpContext.Request.Method = "POST";

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    It.IsAny<ClaimsPrincipal>(),
                    contextAccessor.Value.CookieToken,
                    contextAccessor.Value.RequestToken,
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
        [MemberData(nameof(SafeHttpMethods))]
        public async Task IsRequestValidAsync_SkipsAntiforgery_ForSafeHttpMethods(string httpMethod)
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());
            context.HttpContext.Request.Method = httpMethod;

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    It.IsAny<ClaimsPrincipal>(),
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
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AntiforgeryToken>(),
                    It.IsAny<AntiforgeryToken>(),
                    out message),
                    Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnsafeHttpMethods))]
        public async Task IsRequestValidAsync_ValidatesAntiforgery_ForNonSafeHttpMethods(string httpMethod)
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());
            context.HttpContext.Request.Method = httpMethod;

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    It.IsAny<ClaimsPrincipal>(),
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
        public async Task IsRequestValidAsync_UsesPrincipalFromHttpContext()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());
            context.HttpContext.Request.Method = "POST";

            var principal = new ClaimsPrincipal();
            context.HttpContext.User = principal;

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    principal,
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
        public async Task IsRequestValidAsync_UsesPassedInPrincipal()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());
            context.HttpContext.Request.Method = "POST";

            var principal = new ClaimsPrincipal();
            context.HttpContext.User = new ClaimsPrincipal(); // This should be ignored.

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    principal,
                    It.IsAny<AntiforgeryToken>(),
                    It.IsAny<AntiforgeryToken>(),
                    out message))
                .Returns(true)
                .Verifiable();

            var antiforgery = GetAntiforgery(context);

            // Act
            var result = await antiforgery.IsRequestValidAsync(context.HttpContext, principal);

            // Assert
            Assert.True(result);
            context.TokenGenerator.Verify();
        }

        [Fact]
        public async Task ValidateRequestAsync_FromStore_Failure()
        {
            // Arrange
            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            var context = CreateMockContext(new AntiforgeryOptions(), contextAccessor: contextAccessor);

            var message = "my-message";
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    It.IsAny<ClaimsPrincipal>(),
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
            Assert.NotNull(contextAccessor.Value);
            Assert.True(contextAccessor.Value.HaveDeserializedCookieToken);
            Assert.Equal(context.TestTokenSet.OldCookieToken, contextAccessor.Value.CookieToken);
            Assert.True(contextAccessor.Value.HaveDeserializedRequestToken);
            Assert.Equal(context.TestTokenSet.RequestToken, contextAccessor.Value.RequestToken);
        }

        [Fact]
        public async Task ValidateRequestAsync_FromStore_Success()
        {
            // Arrange
            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            var context = CreateMockContext(new AntiforgeryOptions(), contextAccessor: contextAccessor);

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    It.IsAny<ClaimsPrincipal>(),
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

            Assert.NotNull(contextAccessor.Value);
            Assert.True(contextAccessor.Value.HaveDeserializedCookieToken);
            Assert.Equal(context.TestTokenSet.OldCookieToken, contextAccessor.Value.CookieToken);
            Assert.True(contextAccessor.Value.HaveDeserializedRequestToken);
            Assert.Equal(context.TestTokenSet.RequestToken, contextAccessor.Value.RequestToken);
        }

        [Fact]
        public async Task ValidateRequestAsync_NoCookieToken_Throws()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
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
                CookieName = "cookie-name",
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
                CookieName = "cookie-name",
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
                CookieName = "cookie-name",
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
            var contextAccessor = new DefaultAntiforgeryContextAccessor
            {
                Value = new AntiforgeryContext
                {
                    HaveDeserializedCookieToken = true,
                    CookieToken = new AntiforgeryToken(),
                    HaveDeserializedRequestToken = true,
                    RequestToken = new AntiforgeryToken(),
                },
            };
            var context = CreateMockContext(new AntiforgeryOptions(), contextAccessor: contextAccessor);

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    It.IsAny<ClaimsPrincipal>(),
                    contextAccessor.Value.CookieToken,
                    contextAccessor.Value.RequestToken,
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

        [Theory]
        [MemberData(nameof(SafeHttpMethods))]
        public async Task ValidateRequestAsync_SkipsAntiforgery_ForSafeHttpMethods(string httpMethod)
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());
            context.HttpContext.Request.Method = httpMethod;

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AntiforgeryToken>(),
                    It.IsAny<AntiforgeryToken>(),
                    out message))
                .Returns(false)
                .Verifiable();

            var antiforgery = GetAntiforgery(context);

            // Act
            await antiforgery.ValidateRequestAsync(context.HttpContext);

            // Assert
            context.TokenGenerator
                .Verify(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AntiforgeryToken>(),
                    It.IsAny<AntiforgeryToken>(),
                    out message),
                    Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnsafeHttpMethods))]
        public async Task ValidateRequestAsync_ValidatesAntiforgery_ForNonSafeHttpMethods(string httpMethod)
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());
            context.HttpContext.Request.Method = httpMethod;

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AntiforgeryToken>(),
                    It.IsAny<AntiforgeryToken>(),
                    out message))
                .Returns(true)
                .Verifiable();

            var antiforgery = GetAntiforgery(context);

            // Act
            await antiforgery.ValidateRequestAsync(context.HttpContext);

            // Assert
            context.TokenGenerator.Verify();
        }

        [Fact]
        public async Task ValidateRequestAsync_UsesPrincipalFromHttpContext()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());
            context.HttpContext.Request.Method = "POST";

            var principal = new ClaimsPrincipal();
            context.HttpContext.User = principal;

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    principal,
                    It.IsAny<AntiforgeryToken>(),
                    It.IsAny<AntiforgeryToken>(),
                    out message))
                .Returns(true)
                .Verifiable();

            var antiforgery = GetAntiforgery(context);

            // Act
            await antiforgery.ValidateRequestAsync(context.HttpContext);

            // Assert
            context.TokenGenerator.Verify();
        }

        [Fact]
        public async Task ValidateRequestAsync_UsesPassedInPrincipal()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());
            context.HttpContext.Request.Method = "POST";

            var principal = new ClaimsPrincipal();
            context.HttpContext.User = new ClaimsPrincipal(); // This should be ignored.

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    principal,
                    It.IsAny<AntiforgeryToken>(),
                    It.IsAny<AntiforgeryToken>(),
                    out message))
                .Returns(true)
                .Verifiable();

            var antiforgery = GetAntiforgery(context);

            // Act
            await antiforgery.ValidateRequestAsync(context.HttpContext, principal);

            // Assert
            context.TokenGenerator.Verify();
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
            var contextAccessor = new DefaultAntiforgeryContextAccessor();

            // Generate a new cookie.
            var context = CreateMockContext(
                options,
                useOldCookie: false,
                isOldCookieValid: false,
                contextAccessor: contextAccessor);
            var antiforgery = GetAntiforgery(context);

            // Act
            antiforgery.SetCookieTokenAndHeader(context.HttpContext);

            // Assert
            var xFrameOptions = context.HttpContext.Response.Headers["X-Frame-Options"];
            Assert.Equal(expectedHeaderValue, xFrameOptions);

            Assert.NotNull(contextAccessor.Value);
            Assert.True(contextAccessor.Value.HaveDeserializedCookieToken);
            Assert.Equal(context.TestTokenSet.OldCookieToken, contextAccessor.Value.CookieToken);
            Assert.True(contextAccessor.Value.HaveGeneratedNewCookieToken);
            Assert.Equal(context.TestTokenSet.NewCookieToken, contextAccessor.Value.NewCookieToken);
            Assert.Equal(context.TestTokenSet.NewCookieTokenString, contextAccessor.Value.NewCookieTokenString);
            Assert.True(contextAccessor.Value.HaveStoredNewCookieToken);
        }

        [Fact]
        public void SetCookieTokenAndHeader_DoesNotDeserializeTwice()
        {
            // Arrange
            var contextAccessor = new DefaultAntiforgeryContextAccessor
            {
                Value = new AntiforgeryContext
                {
                    HaveDeserializedCookieToken = true,
                    HaveGeneratedNewCookieToken = true,
                    NewCookieToken = new AntiforgeryToken(),
                    NewCookieTokenString = "serialized-cookie-token-from-context",
                    NewRequestToken = new AntiforgeryToken(),
                    NewRequestTokenString = "serialized-form-token-from-context",
                },
            };
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: true,
                isOldCookieValid: true,
                contextAccessor: contextAccessor);
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
            var contextAccessor = new DefaultAntiforgeryContextAccessor
            {
                Value = new AntiforgeryContext
                {
                    HaveDeserializedCookieToken = true,
                    HaveGeneratedNewCookieToken = true,
                    HaveStoredNewCookieToken = true,
                    NewCookieToken = new AntiforgeryToken(),
                    NewCookieTokenString = "serialized-cookie-token-from-context",
                    NewRequestToken = new AntiforgeryToken(),
                    NewRequestTokenString = "serialized-form-token-from-context",
                },
            };
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: true,
                isOldCookieValid: true,
                contextAccessor: contextAccessor);
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

        private DefaultAntiforgery GetAntiforgery(
            HttpContext httpContext,
            AntiforgeryOptions options = null,
            IAntiforgeryTokenGenerator tokenGenerator = null,
            IAntiforgeryTokenSerializer tokenSerializer = null,
            IAntiforgeryTokenStore tokenStore = null)
        {
            var optionsManager = new TestOptionsManager();
            if (options != null)
            {
                optionsManager.Value = options;
            }

            var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            return new DefaultAntiforgery(
                antiforgeryOptionsAccessor: optionsManager,
                tokenGenerator: tokenGenerator,
                tokenSerializer: tokenSerializer,
                tokenStore: tokenStore,
                loggerFactory: loggerFactory);
        }

        private IServiceProvider GetServices(IAntiforgeryContextAccessor contextAccessor)
        {
            var builder = new ServiceCollection();
            builder.AddSingleton<IAntiforgeryContextAccessor>(contextAccessor);
            builder.AddSingleton<ILoggerFactory>(new LoggerFactory());

            return builder.BuildServiceProvider();
        }

        private HttpContext GetHttpContext(IAntiforgeryContextAccessor contextAccessor = null)
        {
            var httpContext = new DefaultHttpContext();
            contextAccessor = contextAccessor ?? new DefaultAntiforgeryContextAccessor();
            httpContext.RequestServices = GetServices(contextAccessor);

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
            IAntiforgeryContextAccessor contextAccessor = null)
        {
            // Arrange
            var httpContext = GetHttpContext(contextAccessor);
            var testTokenSet = GetTokenSet();

            var mockSerializer = GetTokenSerializer(testTokenSet);

            var mockTokenStore = GetTokenStore(httpContext, testTokenSet, !useOldCookie);

            var mockGenerator = new Mock<IAntiforgeryTokenGenerator>(MockBehavior.Strict);
            mockGenerator
                .Setup(o => o.GenerateRequestToken(
                    httpContext,
                    It.IsAny<ClaimsPrincipal>(),
                    useOldCookie ? testTokenSet.OldCookieToken : testTokenSet.NewCookieToken))
                .Returns(testTokenSet.RequestToken);

            mockGenerator
                .Setup(o => o.GenerateCookieToken())
                .Returns(useOldCookie ? testTokenSet.OldCookieToken : testTokenSet.NewCookieToken);

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
            public AntiforgeryToken RequestToken { get; set; }

            public string FormTokenString { get; set; }

            public AntiforgeryToken OldCookieToken { get; set; }

            public string OldCookieTokenString { get; set; }

            public AntiforgeryToken NewCookieToken { get; set; }

            public string NewCookieTokenString { get; set; }
        }

        private class AntiforgeryMockContext
        {
            public AntiforgeryOptions Options { get; set; }

            public TestTokenSet TestTokenSet { get; set; }

            public HttpContext HttpContext { get; set; }

            public Mock<IAntiforgeryTokenGenerator> TokenGenerator { get; set; }

            public Mock<IAntiforgeryTokenStore> TokenStore { get; set; }

            public Mock<IAntiforgeryTokenSerializer> TokenSerializer { get; set; }
        }

        private class TestOptionsManager : IOptions<AntiforgeryOptions>
        {
            public AntiforgeryOptions Value { get; set; } = new AntiforgeryOptions();
        }
    }
}
