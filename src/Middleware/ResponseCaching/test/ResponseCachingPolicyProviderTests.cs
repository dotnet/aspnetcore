// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class ResponseCachingPolicyProviderTests
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
        public void AttemptResponseCaching_CacheableMethods_Allowed(string method)
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Method = method;

            Assert.True(new ResponseCachingPolicyProvider().AttemptResponseCaching(context));
            Assert.Empty(sink.Writes);
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
        public void AttemptResponseCaching_UncacheableMethods_NotAllowed(string method)
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Method = method;

            Assert.False(new ResponseCachingPolicyProvider().AttemptResponseCaching(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.RequestMethodNotCacheable);
        }

        [Fact]
        public void AttemptResponseCaching_AuthorizationHeaders_NotAllowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Method = HttpMethods.Get;
            context.HttpContext.Request.Headers[HeaderNames.Authorization] = "Basic plaintextUN:plaintextPW";

            Assert.False(new ResponseCachingPolicyProvider().AttemptResponseCaching(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.RequestWithAuthorizationNotCacheable);
        }

        [Fact]
        public void AllowCacheStorage_NoStore_Allowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Method = HttpMethods.Get;
            context.HttpContext.Request.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                NoStore = true
            }.ToString();

            Assert.True(new ResponseCachingPolicyProvider().AllowCacheLookup(context));
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void AllowCacheLookup_NoCache_NotAllowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Method = HttpMethods.Get;
            context.HttpContext.Request.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                NoCache = true
            }.ToString();

            Assert.False(new ResponseCachingPolicyProvider().AllowCacheLookup(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.RequestWithNoCacheNotCacheable);
        }

        [Fact]
        public void AllowCacheLookup_LegacyDirectives_NotAllowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Method = HttpMethods.Get;
            context.HttpContext.Request.Headers[HeaderNames.Pragma] = "no-cache";

            Assert.False(new ResponseCachingPolicyProvider().AllowCacheLookup(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.RequestWithPragmaNoCacheNotCacheable);
        }

        [Fact]
        public void AllowCacheLookup_LegacyDirectives_OverridenByCacheControl()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Method = HttpMethods.Get;
            context.HttpContext.Request.Headers[HeaderNames.Pragma] = "no-cache";
            context.HttpContext.Request.Headers[HeaderNames.CacheControl] = "max-age=10";

            Assert.True(new ResponseCachingPolicyProvider().AllowCacheLookup(context));
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void AllowCacheStorage_NoStore_NotAllowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Method = HttpMethods.Get;
            context.HttpContext.Request.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                NoStore = true
            }.ToString();

            Assert.False(new ResponseCachingPolicyProvider().AllowCacheStorage(context));
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void IsResponseCacheable_NoPublic_NotAllowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);

            Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ResponseWithoutPublicNotCacheable);
        }

        [Fact]
        public void IsResponseCacheable_Public_Allowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true
            }.ToString();

            Assert.True(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void IsResponseCacheable_NoCache_NotAllowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true,
                NoCache = true
            }.ToString();

            Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ResponseWithNoCacheNotCacheable);
        }

        [Fact]
        public void IsResponseCacheable_ResponseNoStore_NotAllowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true,
                NoStore = true
            }.ToString();

            Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ResponseWithNoStoreNotCacheable);
        }

        [Fact]
        public void IsResponseCacheable_SetCookieHeader_NotAllowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true
            }.ToString();
            context.HttpContext.Response.Headers[HeaderNames.SetCookie] = "cookieName=cookieValue";

            Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ResponseWithSetCookieNotCacheable);
        }

        [Fact]
        public void IsResponseCacheable_VaryHeaderByStar_NotAllowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true
            }.ToString();
            context.HttpContext.Response.Headers[HeaderNames.Vary] = "*";

            Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ResponseWithVaryStarNotCacheable);
        }

        [Fact]
        public void IsResponseCacheable_Private_NotAllowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true,
                Private = true
            }.ToString();

            Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ResponseWithPrivateNotCacheable);
        }

        [Theory]
        [InlineData(StatusCodes.Status200OK)]
        public void IsResponseCacheable_SuccessStatusCodes_Allowed(int statusCode)
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.StatusCode = statusCode;
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true
            }.ToString();

            Assert.True(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            Assert.Empty(sink.Writes);
        }

        [Theory]
        [InlineData(StatusCodes.Status100Continue)]
        [InlineData(StatusCodes.Status101SwitchingProtocols)]
        [InlineData(StatusCodes.Status102Processing)]
        [InlineData(StatusCodes.Status201Created)]
        [InlineData(StatusCodes.Status202Accepted)]
        [InlineData(StatusCodes.Status203NonAuthoritative)]
        [InlineData(StatusCodes.Status204NoContent)]
        [InlineData(StatusCodes.Status205ResetContent)]
        [InlineData(StatusCodes.Status206PartialContent)]
        [InlineData(StatusCodes.Status207MultiStatus)]
        [InlineData(StatusCodes.Status208AlreadyReported)]
        [InlineData(StatusCodes.Status226IMUsed)]
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
        [InlineData(StatusCodes.Status421MisdirectedRequest)]
        [InlineData(StatusCodes.Status422UnprocessableEntity)]
        [InlineData(StatusCodes.Status423Locked)]
        [InlineData(StatusCodes.Status424FailedDependency)]
        [InlineData(StatusCodes.Status426UpgradeRequired)]
        [InlineData(StatusCodes.Status428PreconditionRequired)]
        [InlineData(StatusCodes.Status429TooManyRequests)]
        [InlineData(StatusCodes.Status431RequestHeaderFieldsTooLarge)]
        [InlineData(StatusCodes.Status451UnavailableForLegalReasons)]
        [InlineData(StatusCodes.Status500InternalServerError)]
        [InlineData(StatusCodes.Status501NotImplemented)]
        [InlineData(StatusCodes.Status502BadGateway)]
        [InlineData(StatusCodes.Status503ServiceUnavailable)]
        [InlineData(StatusCodes.Status504GatewayTimeout)]
        [InlineData(StatusCodes.Status505HttpVersionNotsupported)]
        [InlineData(StatusCodes.Status506VariantAlsoNegotiates)]
        [InlineData(StatusCodes.Status507InsufficientStorage)]
        [InlineData(StatusCodes.Status508LoopDetected)]
        [InlineData(StatusCodes.Status510NotExtended)]
        [InlineData(StatusCodes.Status511NetworkAuthenticationRequired)]
        public void IsResponseCacheable_NonSuccessStatusCodes_NotAllowed(int statusCode)
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.StatusCode = statusCode;
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true
            }.ToString();

            Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ResponseWithUnsuccessfulStatusCodeNotCacheable);
        }

        [Fact]
        public void IsResponseCacheable_NoExpiryRequirements_IsAllowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true
            }.ToString();

            var utcNow = DateTimeOffset.UtcNow;
            context.HttpContext.Response.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow);
            context.ResponseTime = DateTimeOffset.MaxValue;

            Assert.True(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void IsResponseCacheable_AtExpiry_NotAllowed()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true
            }.ToString();
            var utcNow = DateTimeOffset.UtcNow;
            context.HttpContext.Response.Headers[HeaderNames.Expires] = HeaderUtilities.FormatDate(utcNow);

            context.HttpContext.Response.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow);
            context.ResponseTime = utcNow;

            Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationExpiresExceeded);
        }

        [Fact]
        public void IsResponseCacheable_MaxAgeOverridesExpiry_ToAllowed()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10)
            }.ToString();
            context.HttpContext.Response.Headers[HeaderNames.Expires] = HeaderUtilities.FormatDate(utcNow);
            context.HttpContext.Response.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow);
            context.ResponseTime = utcNow + TimeSpan.FromSeconds(9);

            Assert.True(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void IsResponseCacheable_MaxAgeOverridesExpiry_ToNotAllowed()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10)
            }.ToString();
            context.HttpContext.Response.Headers[HeaderNames.Expires] = HeaderUtilities.FormatDate(utcNow);
            context.HttpContext.Response.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow);
            context.ResponseTime = utcNow + TimeSpan.FromSeconds(10);

            Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationMaxAgeExceeded);
        }

        [Fact]
        public void IsResponseCacheable_SharedMaxAgeOverridesMaxAge_ToAllowed()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10),
                SharedMaxAge = TimeSpan.FromSeconds(15)
            }.ToString();
            context.HttpContext.Response.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow);
            context.ResponseTime = utcNow + TimeSpan.FromSeconds(11);

            Assert.True(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void IsResponseCacheable_SharedMaxAgeOverridesMaxAge_ToNotAllowed()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.HttpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10),
                SharedMaxAge = TimeSpan.FromSeconds(5)
            }.ToString();
            context.HttpContext.Response.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow);
            context.ResponseTime = utcNow + TimeSpan.FromSeconds(5);

            Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationSharedMaxAgeExceeded);
        }

        [Fact]
        public void IsCachedEntryFresh_NoCachedCacheControl_FallsbackToEmptyCacheControl()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.ResponseTime = DateTimeOffset.MaxValue;
            context.CachedEntryAge = TimeSpan.MaxValue;
            context.CachedResponseHeaders = new HeaderDictionary();

            Assert.True(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void IsCachedEntryFresh_NoExpiryRequirements_IsFresh()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.ResponseTime = DateTimeOffset.MaxValue;
            context.CachedEntryAge = TimeSpan.MaxValue;
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true
            }.ToString();

            Assert.True(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void IsCachedEntryFresh_AtExpiry_IsNotFresh()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.ResponseTime = utcNow;
            context.CachedEntryAge = TimeSpan.Zero;
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true
            }.ToString();
            context.CachedResponseHeaders[HeaderNames.Expires] = HeaderUtilities.FormatDate(utcNow);

            Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationExpiresExceeded);
        }

        [Fact]
        public void IsCachedEntryFresh_MaxAgeOverridesExpiry_ToFresh()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.CachedEntryAge = TimeSpan.FromSeconds(9);
            context.ResponseTime = utcNow + context.CachedEntryAge;
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10)
            }.ToString();
            context.HttpContext.Response.Headers[HeaderNames.Expires] = HeaderUtilities.FormatDate(utcNow);

            Assert.True(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void IsCachedEntryFresh_MaxAgeOverridesExpiry_ToNotFresh()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.CachedEntryAge = TimeSpan.FromSeconds(10);
            context.ResponseTime = utcNow + context.CachedEntryAge;
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10)
            }.ToString();
            context.HttpContext.Response.Headers[HeaderNames.Expires] = HeaderUtilities.FormatDate(utcNow);

            Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationMaxAgeExceeded);
        }

        [Fact]
        public void IsCachedEntryFresh_SharedMaxAgeOverridesMaxAge_ToFresh()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.CachedEntryAge = TimeSpan.FromSeconds(11);
            context.ResponseTime = utcNow + context.CachedEntryAge;
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10),
                SharedMaxAge = TimeSpan.FromSeconds(15)
            }.ToString();
            context.CachedResponseHeaders[HeaderNames.Expires] = HeaderUtilities.FormatDate(utcNow);

            Assert.True(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void IsCachedEntryFresh_SharedMaxAgeOverridesMaxAge_ToNotFresh()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.CachedEntryAge = TimeSpan.FromSeconds(5);
            context.ResponseTime = utcNow + context.CachedEntryAge;
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10),
                SharedMaxAge = TimeSpan.FromSeconds(5)
            }.ToString();
            context.CachedResponseHeaders[HeaderNames.Expires] = HeaderUtilities.FormatDate(utcNow);

            Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationSharedMaxAgeExceeded);
        }

        [Fact]
        public void IsCachedEntryFresh_MinFreshReducesFreshness_ToNotFresh()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MinFresh = TimeSpan.FromSeconds(2)
            }.ToString();
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(10),
                SharedMaxAge = TimeSpan.FromSeconds(5)
            }.ToString();
            context.CachedEntryAge = TimeSpan.FromSeconds(3);

            Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationMinFreshAdded,
                LoggedMessage.ExpirationSharedMaxAgeExceeded);
        }

        [Fact]
        public void IsCachedEntryFresh_RequestMaxAgeRestrictAge_ToNotFresh()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5)
            }.ToString();
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(10),
            }.ToString();
            context.CachedEntryAge = TimeSpan.FromSeconds(5);

            Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationMaxAgeExceeded);
        }

        [Fact]
        public void IsCachedEntryFresh_MaxStaleOverridesFreshness_ToFresh()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
                MaxStale = true, // This value must be set to true in order to specify MaxStaleLimit
                MaxStaleLimit = TimeSpan.FromSeconds(2)
            }.ToString();
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
            }.ToString();
            context.CachedEntryAge = TimeSpan.FromSeconds(6);

            Assert.True(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationMaxStaleSatisfied);
        }

        [Fact]
        public void IsCachedEntryFresh_MaxStaleInfiniteOverridesFreshness_ToFresh()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
                MaxStale = true // No value specified means a MaxStaleLimit of infinity
            }.ToString();
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
            }.ToString();
            context.CachedEntryAge = TimeSpan.FromSeconds(6);

            Assert.True(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationInfiniteMaxStaleSatisfied);
        }

        [Fact]
        public void IsCachedEntryFresh_MaxStaleOverridesFreshness_ButStillNotFresh()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
                MaxStale = true, // This value must be set to true in order to specify MaxStaleLimit
                MaxStaleLimit = TimeSpan.FromSeconds(1)
            }.ToString();
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
            }.ToString();
            context.CachedEntryAge = TimeSpan.FromSeconds(6);

            Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationMaxAgeExceeded);
        }

        [Fact]
        public void IsCachedEntryFresh_MustRevalidateOverridesRequestMaxStale_ToNotFresh()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
                MaxStale = true, // This value must be set to true in order to specify MaxStaleLimit
                MaxStaleLimit = TimeSpan.FromSeconds(2)
            }.ToString();
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
                MustRevalidate = true
            }.ToString();
            context.CachedEntryAge = TimeSpan.FromSeconds(6);

            Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationMustRevalidate);
        }

        [Fact]
        public void IsCachedEntryFresh_ProxyRevalidateOverridesRequestMaxStale_ToNotFresh()
        {
            var sink = new TestSink();
            var context = TestUtils.CreateTestContext(sink);
            context.HttpContext.Request.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
                MaxStale = true, // This value must be set to true in order to specify MaxStaleLimit
                MaxStaleLimit = TimeSpan.FromSeconds(2)
            }.ToString();
            context.CachedResponseHeaders = new HeaderDictionary();
            context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(5),
                MustRevalidate = true
            }.ToString();
            context.CachedEntryAge = TimeSpan.FromSeconds(6);

            Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
            TestUtils.AssertLoggedMessages(
                sink.Writes,
                LoggedMessage.ExpirationMustRevalidate);
        }
    }
}
