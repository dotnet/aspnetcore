// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class DefaultKeyProviderTests
    {
        private static readonly char KeyDelimiter = '\x1e';

        [Fact]
        public void DefaultKeyProvider_CreateBaseKey_IncludesOnlyNormalizedMethodAndPath()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "head";
            httpContext.Request.Path = "/path/subpath";
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("example.com", 80);
            httpContext.Request.PathBase = "/pathBase";
            httpContext.Request.QueryString = new QueryString("?query.Key=a&query.Value=b");
            var keyProvider = CreateTestKeyProvider();

            Assert.Equal($"HEAD{KeyDelimiter}/PATH/SUBPATH", keyProvider.CreateBaseKey(httpContext));
        }

        [Fact]
        public void DefaultKeyProvider_CreateBaseKey_CaseInsensitivePath_NormalizesPath()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/Path";
            var keyProvider = CreateTestKeyProvider(new ResponseCachingOptions()
            {
                CaseSensitivePaths = false
            });

            Assert.Equal($"GET{KeyDelimiter}/PATH", keyProvider.CreateBaseKey(httpContext));
        }

        [Fact]
        public void DefaultKeyProvider_CreateBaseKey_CaseSensitivePath_PreservesPathCase()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/Path";
            var keyProvider = CreateTestKeyProvider(new ResponseCachingOptions()
            {
                CaseSensitivePaths = true
            });

            Assert.Equal($"GET{KeyDelimiter}/Path", keyProvider.CreateBaseKey(httpContext));
        }

        [Fact]
        public void DefaultKeyProvider_CreateVaryKey_IncludesListedHeadersOnly()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/";
            httpContext.Request.Headers["HeaderA"] = "ValueA";
            httpContext.Request.Headers["HeaderB"] = "ValueB";
            var keyProvider = CreateTestKeyProvider();

            Assert.Equal($"GET{KeyDelimiter}/{KeyDelimiter}H{KeyDelimiter}HeaderA=ValueA{KeyDelimiter}HeaderC=null",
                keyProvider.CreateVaryKey(httpContext, new VaryRules()
                {
                    Headers = new string[] { "HeaderA", "HeaderC" }
                }));
        }

        [Fact]
        public void DefaultKeyProvider_CreateVaryKey_IncludesListedParamsOnly()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/";
            httpContext.Request.QueryString = new QueryString("?ParamA=ValueA&ParamB=ValueB");
            var keyProvider = CreateTestKeyProvider();

            Assert.Equal($"GET{KeyDelimiter}/{KeyDelimiter}Q{KeyDelimiter}ParamA=ValueA{KeyDelimiter}ParamC=null",
                keyProvider.CreateVaryKey(httpContext, new VaryRules()
                {
                    Params = new string[] { "ParamA", "ParamC" }
                }));
        }

        [Fact]
        public void DefaultKeyProvider_CreateVaryKey_IncludesParams_ParamNameCaseInsensitive_UseParamCasing()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/";
            httpContext.Request.QueryString = new QueryString("?parama=ValueA&paramB=ValueB");
            var keyProvider = CreateTestKeyProvider();

            Assert.Equal($"GET{KeyDelimiter}/{KeyDelimiter}Q{KeyDelimiter}ParamA=ValueA{KeyDelimiter}ParamC=null",
                keyProvider.CreateVaryKey(httpContext, new VaryRules()
                {
                    Params = new string[] { "ParamA", "ParamC" }
                }));
        }

        [Fact]
        public void DefaultKeyProvider_CreateVaryKey_IncludesAllQueryParamsGivenAsterisk()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/";
            httpContext.Request.QueryString = new QueryString("?ParamA=ValueA&ParamB=ValueB");
            var keyProvider = CreateTestKeyProvider();

            // To support case insensitivity, all param keys are converted to upper case.
            // Explicit params uses the casing specified in the setting.
            Assert.Equal($"GET{KeyDelimiter}/{KeyDelimiter}Q{KeyDelimiter}PARAMA=ValueA{KeyDelimiter}PARAMB=ValueB",
                keyProvider.CreateVaryKey(httpContext, new VaryRules()
                {
                    Params = new string[] { "*" }
                }));
        }

        [Fact]
        public void DefaultKeyProvider_CreateVaryKey_IncludesListedHeadersAndParams()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/";
            httpContext.Request.Headers["HeaderA"] = "ValueA";
            httpContext.Request.Headers["HeaderB"] = "ValueB";
            httpContext.Request.QueryString = new QueryString("?ParamA=ValueA&ParamB=ValueB");
            var keyProvider = CreateTestKeyProvider();

            Assert.Equal($"GET{KeyDelimiter}/{KeyDelimiter}H{KeyDelimiter}HeaderA=ValueA{KeyDelimiter}HeaderC=null{KeyDelimiter}Q{KeyDelimiter}ParamA=ValueA{KeyDelimiter}ParamC=null",
                keyProvider.CreateVaryKey(httpContext, new VaryRules()
                {
                    Headers = new string[] { "HeaderA", "HeaderC" },
                    Params = new string[] { "ParamA", "ParamC" }
                }));
        }

        private static IKeyProvider CreateTestKeyProvider()
        {
            return CreateTestKeyProvider(new ResponseCachingOptions());
        }

        private static IKeyProvider CreateTestKeyProvider(ResponseCachingOptions options)
        {
            return new KeyProvider(new DefaultObjectPoolProvider(), Options.Create(options));
        }
    }
}
