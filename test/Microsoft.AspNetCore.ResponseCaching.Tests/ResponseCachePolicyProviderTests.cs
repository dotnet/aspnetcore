// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class ResponseCachePolicyProviderTests
    {
        public static TheoryData<string> CacheableMethods
        {
            get
            {
                return new TheoryData<string>
                {
                    HttpMethods.Get,
                    HttpMethods.Head
                };
            }
        }

        [Theory]
        [MemberData(nameof(CacheableMethods))]
        public void IsRequestCacheable_CacheableMethods_Allowed(string method)
        {
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.Method = method;

            Assert.True(new ResponseCachePolicyProvider().IsRequestCacheable(context));
        }
        public static TheoryData<string> NonCacheableMethods
        {
            get
            {
                return new TheoryData<string>
                {
                    HttpMethods.Post,
                    HttpMethods.Put,
                    HttpMethods.Delete,
                    HttpMethods.Trace,
                    HttpMethods.Connect,
                    HttpMethods.Options,
                    "",
                    null
                };
            }
        }

        [Theory]
        [MemberData(nameof(NonCacheableMethods))]
        public void IsRequestCacheable_UncacheableMethods_NotAllowed(string method)
        {
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.Method = method;

            Assert.False(new ResponseCachePolicyProvider().IsRequestCacheable(context));
        }

        [Fact]
        public void IsRequestCacheable_AuthorizationHeaders_NotAllowed()
        {
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.Method = HttpMethods.Get;
            context.HttpContext.Request.Headers[HeaderNames.Authorization] = "Basic plaintextUN:plaintextPW";

            Assert.False(new ResponseCachePolicyProvider().IsRequestCacheable(context));
        }

        [Fact]
        public void IsRequestCacheable_NoCache_NotAllowed()
        {
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.Method = HttpMethods.Get;
            context.TypedRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                NoCache = true
            };

            Assert.False(new ResponseCachePolicyProvider().IsRequestCacheable(context));
        }

        [Fact]
        public void IsRequestCacheable_NoStore_Allowed()
        {
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.Method = HttpMethods.Get;
            context.TypedRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                NoStore = true
            };

            Assert.True(new ResponseCachePolicyProvider().IsRequestCacheable(context));
        }

        [Fact]
        public void IsRequestCacheable_LegacyDirectives_NotAllowed()
        {
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.Method = HttpMethods.Get;
            context.HttpContext.Request.Headers[HeaderNames.Pragma] = "no-cache";

            Assert.False(new ResponseCachePolicyProvider().IsRequestCacheable(context));
        }

        [Fact]
        public void IsRequestCacheable_LegacyDirectives_OverridenByCacheControl()
        {
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.Method = HttpMethods.Get;
            context.HttpContext.Request.Headers[HeaderNames.Pragma] = "no-cache";
            context.HttpContext.Request.Headers[HeaderNames.CacheControl] = "max-age=10";

            Assert.True(new ResponseCachePolicyProvider().IsRequestCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_NoPublic_NotAllowed()
        {
            var context = TestUtils.CreateTestContext();

            Assert.False(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_Public_Allowed()
        {
            var context = TestUtils.CreateTestContext();
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };

            Assert.True(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_NoCache_NotAllowed()
        {
            var context = TestUtils.CreateTestContext();
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                NoCache = true
            };

            Assert.False(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_RequestNoStore_NotAllowed()
        {
            var context = TestUtils.CreateTestContext();
            context.TypedRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                NoStore = true
            };
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };

            Assert.False(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_ResponseNoStore_NotAllowed()
        {
            var context = TestUtils.CreateTestContext();
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                NoStore = true
            };

            Assert.False(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_SetCookieHeader_NotAllowed()
        {
            var context = TestUtils.CreateTestContext();
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };
            context.HttpContext.Response.Headers[HeaderNames.SetCookie] = "cookieName=cookieValue";

            Assert.False(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_VaryHeaderByStar_NotAllowed()
        {
            var context = TestUtils.CreateTestContext();
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };
            context.HttpContext.Response.Headers[HeaderNames.Vary] = "*";

            Assert.False(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_Private_NotAllowed()
        {
            var context = TestUtils.CreateTestContext();
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                Private = true
            };

            Assert.False(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Theory]
        [InlineData(StatusCodes.Status200OK)]
        public void IsResponseCacheable_SuccessStatusCodes_Allowed(int statusCode)
        {
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Response.StatusCode = statusCode;
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };

            Assert.True(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Theory]
        [InlineData(StatusCodes.Status201Created)]
        [InlineData(StatusCodes.Status202Accepted)]
        [InlineData(StatusCodes.Status203NonAuthoritative)]
        [InlineData(StatusCodes.Status204NoContent)]
        [InlineData(StatusCodes.Status205ResetContent)]
        [InlineData(StatusCodes.Status206PartialContent)]
        [InlineData(StatusCodes.Status207MultiStatus)]
        [InlineData(StatusCodes.Status300MultipleChoices)]
        [InlineData(StatusCodes.Status301MovedPermanently)]
        [InlineData(StatusCodes.Status302Found)]
        [InlineData(StatusCodes.Status303SeeOther)]
        [InlineData(StatusCodes.Status304NotModified)]
        [InlineData(StatusCodes.Status305UseProxy)]
        [InlineData(StatusCodes.Status306SwitchProxy)]
        [InlineData(StatusCodes.Status307TemporaryRedirect)]
        [InlineData(StatusCodes.Status308PermanentRedirect)]
        [InlineData(StatusCodes.Status400BadRequest)]
        [InlineData(StatusCodes.Status401Unauthorized)]
        [InlineData(StatusCodes.Status402PaymentRequired)]
        [InlineData(StatusCodes.Status403Forbidden)]
        [InlineData(StatusCodes.Status404NotFound)]
        [InlineData(StatusCodes.Status405MethodNotAllowed)]
        [InlineData(StatusCodes.Status406NotAcceptable)]
        [InlineData(StatusCodes.Status407ProxyAuthenticationRequired)]
        [InlineData(StatusCodes.Status408RequestTimeout)]
        [InlineData(StatusCodes.Status409Conflict)]
        [InlineData(StatusCodes.Status410Gone)]
        [InlineData(StatusCodes.Status411LengthRequired)]
        [InlineData(StatusCodes.Status412PreconditionFailed)]
        [InlineData(StatusCodes.Status413RequestEntityTooLarge)]
        [InlineData(StatusCodes.Status414RequestUriTooLong)]
        [InlineData(StatusCodes.Status415UnsupportedMediaType)]
        [InlineData(StatusCodes.Status416RequestedRangeNotSatisfiable)]
        [InlineData(StatusCodes.Status417ExpectationFailed)]
        [InlineData(StatusCodes.Status418ImATeapot)]
        [InlineData(StatusCodes.Status419AuthenticationTimeout)]
        [InlineData(StatusCodes.Status422UnprocessableEntity)]
        [InlineData(StatusCodes.Status423Locked)]
        [InlineData(StatusCodes.Status424FailedDependency)]
        [InlineData(StatusCodes.Status451UnavailableForLegalReasons)]
        [InlineData(StatusCodes.Status500InternalServerError)]
        [InlineData(StatusCodes.Status501NotImplemented)]
        [InlineData(StatusCodes.Status502BadGateway)]
        [InlineData(StatusCodes.Status503ServiceUnavailable)]
        [InlineData(StatusCodes.Status504GatewayTimeout)]
        [InlineData(StatusCodes.Status505HttpVersionNotsupported)]
        [InlineData(StatusCodes.Status506VariantAlsoNegotiates)]
        [InlineData(StatusCodes.Status507InsufficientStorage)]
        public void IsResponseCacheable_NonSuccessStatusCodes_NotAllowed(int statusCode)
        {
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Response.StatusCode = statusCode;
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };

            Assert.False(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_NoExpiryRequirements_IsAllowed()
        {
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };

            var utcNow = DateTimeOffset.UtcNow;
            context.TypedResponseHeaders.Date = utcNow;
            context.ResponseTime = DateTimeOffset.MaxValue;

            Assert.True(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_AtExpiry_NotAllowed()
        {
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };
            var utcNow = DateTimeOffset.UtcNow;
            context.TypedResponseHeaders.Expires = utcNow;

            context.TypedResponseHeaders.Date = utcNow;
            context.ResponseTime = utcNow;

            Assert.False(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_MaxAgeOverridesExpiry_ToAllowed()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10)
            };
            context.TypedResponseHeaders.Expires = utcNow;
            context.TypedResponseHeaders.Date = utcNow;
            context.ResponseTime = utcNow + TimeSpan.FromSeconds(9);

            Assert.True(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_MaxAgeOverridesExpiry_ToNotAllowed()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10)
            };
            context.TypedResponseHeaders.Expires = utcNow;
            context.TypedResponseHeaders.Date = utcNow;
            context.ResponseTime = utcNow + TimeSpan.FromSeconds(10);

            Assert.False(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_SharedMaxAgeOverridesMaxAge_ToAllowed()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10),
                SharedMaxAge = TimeSpan.FromSeconds(15)
            };
            context.TypedResponseHeaders.Date = utcNow;
            context.ResponseTime = utcNow + TimeSpan.FromSeconds(11);

            Assert.True(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsResponseCacheable_SharedMaxAgeOverridesMaxAge_ToNotAllowed()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10),
                SharedMaxAge = TimeSpan.FromSeconds(5)
            };
            context.TypedResponseHeaders.Date = utcNow;
            context.ResponseTime = utcNow + TimeSpan.FromSeconds(5);

            Assert.False(new ResponseCachePolicyProvider().IsResponseCacheable(context));
        }

        [Fact]
        public void IsCachedEntryFresh_NoCachedCacheControl_FallsbackToEmptyCacheControl()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.ResponseTime = DateTimeOffset.MaxValue;
            context.CachedEntryAge = TimeSpan.MaxValue;
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary());

            Assert.True(new ResponseCachePolicyProvider().IsCachedEntryFresh(context));
        }

        [Fact]
        public void IsCachedEntryFresh_NoExpiryRequirements_IsFresh()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.ResponseTime = DateTimeOffset.MaxValue;
            context.CachedEntryAge = TimeSpan.MaxValue;
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                CacheControl = new CacheControlHeaderValue()
                {
                    Public = true
                }
            };

            Assert.True(new ResponseCachePolicyProvider().IsCachedEntryFresh(context));
        }

        [Fact]
        public void IsCachedEntryFresh_AtExpiry_IsNotFresh()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.ResponseTime = utcNow;
            context.CachedEntryAge = TimeSpan.Zero;
            context.CachedResponseHeaders =  new ResponseHeaders(new HeaderDictionary())
            {
                CacheControl = new CacheControlHeaderValue()
                {
                    Public = true
                },
                Expires = utcNow
            };

            Assert.False(new ResponseCachePolicyProvider().IsCachedEntryFresh(context));
        }

        [Fact]
        public void IsCachedEntryFresh_MaxAgeOverridesExpiry_ToFresh()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.CachedEntryAge = TimeSpan.FromSeconds(9);
            context.ResponseTime = utcNow + context.CachedEntryAge;
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10)
                },
                Expires = utcNow
            };

            Assert.True(new ResponseCachePolicyProvider().IsCachedEntryFresh(context));
        }

        [Fact]
        public void IsCachedEntryFresh_MaxAgeOverridesExpiry_ToNotFresh()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.CachedEntryAge = TimeSpan.FromSeconds(10);
            context.ResponseTime = utcNow + context.CachedEntryAge;
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10)
                },
                Expires = utcNow
            };

            Assert.False(new ResponseCachePolicyProvider().IsCachedEntryFresh(context));
        }

        [Fact]
        public void IsCachedEntryFresh_SharedMaxAgeOverridesMaxAge_ToFresh()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.CachedEntryAge = TimeSpan.FromSeconds(11);
            context.ResponseTime = utcNow + context.CachedEntryAge;
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10),
                    SharedMaxAge = TimeSpan.FromSeconds(15)
                },
                Expires = utcNow
            };

            Assert.True(new ResponseCachePolicyProvider().IsCachedEntryFresh(context));
        }

        [Fact]
        public void IsCachedEntryFresh_SharedMaxAgeOverridesMaxAge_ToNotFresh()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.CachedEntryAge = TimeSpan.FromSeconds(5);
            context.ResponseTime = utcNow + context.CachedEntryAge;
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10),
                    SharedMaxAge = TimeSpan.FromSeconds(5)
                },
                Expires = utcNow
            };

            Assert.False(new ResponseCachePolicyProvider().IsCachedEntryFresh(context));
        }

        [Fact]
        public void IsCachedEntryFresh_MinFreshReducesFreshness_ToNotFresh()
        {
            var context = TestUtils.CreateTestContext();
            context.TypedRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                MinFresh = TimeSpan.FromSeconds(2)
            };
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromSeconds(10),
                    SharedMaxAge = TimeSpan.FromSeconds(5)
                }
            };
            context.CachedEntryAge = TimeSpan.FromSeconds(3);

            Assert.False(new ResponseCachePolicyProvider().IsCachedEntryFresh(context));
        }

        [Fact]
        public void IsCachedEntryFresh_RequestMaxAgeRestrictAge_ToNotFresh()
        {
            var context = TestUtils.CreateTestContext();
            context.TypedRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5)
            };
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromSeconds(10),
                }
            };
            context.CachedEntryAge = TimeSpan.FromSeconds(5);

            Assert.False(new ResponseCachePolicyProvider().IsCachedEntryFresh(context));
        }

        [Fact]
        public void IsCachedEntryFresh_MaxStaleOverridesFreshness_ToFresh()
        {
            var context = TestUtils.CreateTestContext();
            context.TypedRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
                MaxStale = true, // This value must be set to true in order to specify MaxStaleLimit
                MaxStaleLimit = TimeSpan.FromSeconds(10)
            };
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromSeconds(5),
                }
            };
            context.CachedEntryAge = TimeSpan.FromSeconds(6);

            Assert.True(new ResponseCachePolicyProvider().IsCachedEntryFresh(context));
        }

        [Fact]
        public void IsCachedEntryFresh_MaxStaleOverridesFreshness_ButStillNotFresh()
        {
            var context = TestUtils.CreateTestContext();
            context.TypedRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
                MaxStale = true, // This value must be set to true in order to specify MaxStaleLimit
                MaxStaleLimit = TimeSpan.FromSeconds(6)
            };
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromSeconds(5),
                }
            };
            context.CachedEntryAge = TimeSpan.FromSeconds(6);

            Assert.False(new ResponseCachePolicyProvider().IsCachedEntryFresh(context));
        }

        [Fact]
        public void IsCachedEntryFresh_MustRevalidateOverridesRequestMaxStale_ToNotFresh()
        {
            var context = TestUtils.CreateTestContext();
            context.TypedRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
                MaxStale = true, // This value must be set to true in order to specify MaxStaleLimit
                MaxStaleLimit = TimeSpan.FromSeconds(10)
            };
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromSeconds(5),
                    MustRevalidate = true
                }
            };
            context.CachedEntryAge = TimeSpan.FromSeconds(6);

            Assert.False(new ResponseCachePolicyProvider().IsCachedEntryFresh(context));
        }
    }
}
