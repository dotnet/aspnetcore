// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class DefaultKeyProviderTests
    {
        private static readonly char KeyDelimiter = '\x1e';

        [Fact]
        public void DefaultKeyProvider_CreateStorageBaseKey_IncludesOnlyNormalizedMethodAndPath()
        {
            var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.Method = "head";
            context.HttpContext.Request.Path = "/path/subpath";
            context.HttpContext.Request.Scheme = "https";
            context.HttpContext.Request.Host = new HostString("example.com", 80);
            context.HttpContext.Request.PathBase = "/pathBase";
            context.HttpContext.Request.QueryString = new QueryString("?query.Key=a&query.Value=b");

            Assert.Equal($"HEAD{KeyDelimiter}/PATH/SUBPATH", cacheKeyProvider.CreateStorageBaseKey(context));
        }

        [Fact]
        public void DefaultKeyProvider_CreateStorageBaseKey_CaseInsensitivePath_NormalizesPath()
        {
            var cacheKeyProvider = TestUtils.CreateTestKeyProvider(new ResponseCachingOptions()
            {
                CaseSensitivePaths = false
            });
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.Method = "GET";
            context.HttpContext.Request.Path = "/Path";

            Assert.Equal($"GET{KeyDelimiter}/PATH", cacheKeyProvider.CreateStorageBaseKey(context));
        }

        [Fact]
        public void DefaultKeyProvider_CreateStorageBaseKey_CaseSensitivePath_PreservesPathCase()
        {
            var cacheKeyProvider = TestUtils.CreateTestKeyProvider(new ResponseCachingOptions()
            {
                CaseSensitivePaths = true
            });
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.Method = "GET";
            context.HttpContext.Request.Path = "/Path";

            Assert.Equal($"GET{KeyDelimiter}/Path", cacheKeyProvider.CreateStorageBaseKey(context));
        }

        [Fact]
        public void DefaultKeyProvider_CreateStorageVaryKey_Throws_IfVaryRulesIsNull()
        {
            var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
            var context = TestUtils.CreateTestContext();

            Assert.Throws<InvalidOperationException>(() => cacheKeyProvider.CreateStorageVaryKey(context));
        }

        [Fact]
        public void DefaultKeyProvider_CreateStorageVaryKey_ReturnsCachedVaryGuid_IfVaryRulesIsEmpty()
        {
            var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
            var context = TestUtils.CreateTestContext();
            context.CachedVaryRules = new CachedVaryRules()
            {
                VaryKeyPrefix = FastGuid.NewGuid().IdString
            };

            Assert.Equal($"{context.CachedVaryRules.VaryKeyPrefix}", cacheKeyProvider.CreateStorageVaryKey(context));
        }

        [Fact]
        public void DefaultKeyProvider_CreateStorageVaryKey_IncludesListedHeadersOnly()
        {
            var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.Headers["HeaderA"] = "ValueA";
            context.HttpContext.Request.Headers["HeaderB"] = "ValueB";
            context.CachedVaryRules = new CachedVaryRules()
            {
                Headers = new string[] { "HeaderA", "HeaderC" }
            };

            Assert.Equal($"{context.CachedVaryRules.VaryKeyPrefix}{KeyDelimiter}H{KeyDelimiter}HeaderA=ValueA{KeyDelimiter}HeaderC=",
                cacheKeyProvider.CreateStorageVaryKey(context));
        }

        [Fact]
        public void DefaultKeyProvider_CreateStorageVaryKey_IncludesListedParamsOnly()
        {
            var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.QueryString = new QueryString("?ParamA=ValueA&ParamB=ValueB");
            context.CachedVaryRules = new CachedVaryRules()
            {
                VaryKeyPrefix = FastGuid.NewGuid().IdString,
                Params = new string[] { "ParamA", "ParamC" }
            };

            Assert.Equal($"{context.CachedVaryRules.VaryKeyPrefix}{KeyDelimiter}Q{KeyDelimiter}ParamA=ValueA{KeyDelimiter}ParamC=",
                cacheKeyProvider.CreateStorageVaryKey(context));
        }

        [Fact]
        public void DefaultKeyProvider_CreateStorageVaryKey_IncludesParams_ParamNameCaseInsensitive_UseParamCasing()
        {
            var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.QueryString = new QueryString("?parama=ValueA&paramB=ValueB");
            context.CachedVaryRules = new CachedVaryRules()
            {
                VaryKeyPrefix = FastGuid.NewGuid().IdString,
                Params = new string[] { "ParamA", "ParamC" }
            };

            Assert.Equal($"{context.CachedVaryRules.VaryKeyPrefix}{KeyDelimiter}Q{KeyDelimiter}ParamA=ValueA{KeyDelimiter}ParamC=",
                cacheKeyProvider.CreateStorageVaryKey(context));
        }

        [Fact]
        public void DefaultKeyProvider_CreateStorageVaryKey_IncludesAllQueryParamsGivenAsterisk()
        {
            var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.QueryString = new QueryString("?ParamA=ValueA&ParamB=ValueB");
            context.CachedVaryRules = new CachedVaryRules()
            {
                VaryKeyPrefix = FastGuid.NewGuid().IdString,
                Params = new string[] { "*" }
            };

            // To support case insensitivity, all param keys are converted to upper case.
            // Explicit params uses the casing specified in the setting.
            Assert.Equal($"{context.CachedVaryRules.VaryKeyPrefix}{KeyDelimiter}Q{KeyDelimiter}PARAMA=ValueA{KeyDelimiter}PARAMB=ValueB",
                cacheKeyProvider.CreateStorageVaryKey(context));
        }

        [Fact]
        public void DefaultKeyProvider_CreateStorageVaryKey_IncludesListedHeadersAndParams()
        {
            var cacheKeyProvider = TestUtils.CreateTestKeyProvider();
            var context = TestUtils.CreateTestContext();
            context.HttpContext.Request.Headers["HeaderA"] = "ValueA";
            context.HttpContext.Request.Headers["HeaderB"] = "ValueB";
            context.HttpContext.Request.QueryString = new QueryString("?ParamA=ValueA&ParamB=ValueB");
            context.CachedVaryRules = new CachedVaryRules()
            {
                VaryKeyPrefix = FastGuid.NewGuid().IdString,
                Headers = new string[] { "HeaderA", "HeaderC" },
                Params = new string[] { "ParamA", "ParamC" }
            };

            Assert.Equal($"{context.CachedVaryRules.VaryKeyPrefix}{KeyDelimiter}H{KeyDelimiter}HeaderA=ValueA{KeyDelimiter}HeaderC={KeyDelimiter}Q{KeyDelimiter}ParamA=ValueA{KeyDelimiter}ParamC=",
                cacheKeyProvider.CreateStorageVaryKey(context));
        }
    }
}
