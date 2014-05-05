// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Abstractions.Extensions
{
    public class MapPathMiddlewareTests
    {
        private static readonly Action<IBuilder> ActionNotImplemented = new Action<IBuilder>(_ => { throw new NotImplementedException(); });

        private static Task Success(HttpContext context)
        {
            context.Response.StatusCode = 200;
            context.Items["test.PathBase"] = context.Request.PathBase.Value;
            context.Items["test.Path"] = context.Request.Path.Value;
            return Task.FromResult<object>(null);
        }

        private static void UseSuccess(IBuilder app)
        {
            app.Run(Success);
        }

        private static Task NotImplemented(HttpContext context)
        {
            throw new NotImplementedException();
        }

        private static void UseNotImplemented(IBuilder app)
        {
            app.Run(NotImplemented);
        }

        [Fact]
        public void NullArguments_ArgumentNullException()
        {
            var builder = new Builder(serviceProvider: null);
            var noMiddleware = new Builder(serviceProvider: null).Build();
            var noOptions = new MapOptions();
            // TODO: [NotNull] Assert.Throws<ArgumentNullException>(() => builder.Map(null, ActionNotImplemented));
            // TODO: [NotNull] Assert.Throws<ArgumentNullException>(() => builder.Map("/foo", (Action<IBuilder>)null));
            // TODO: [NotNull] Assert.Throws<ArgumentNullException>(() => new MapMiddleware(null, noOptions));
            // TODO: [NotNull] Assert.Throws<ArgumentNullException>(() => new MapMiddleware(noMiddleware, null));
        }

        [Theory]
        [InlineData("/foo", "", "/foo")]
        [InlineData("/foo", "", "/foo/")]
        [InlineData("/foo", "/Bar", "/foo")]
        [InlineData("/foo", "/Bar", "/foo/cho")]
        [InlineData("/foo", "/Bar", "/foo/cho/")]
        [InlineData("/foo/cho", "/Bar", "/foo/cho")]
        [InlineData("/foo/cho", "/Bar", "/foo/cho/do")]
        public void PathMatchFunc_BranchTaken(string matchPath, string basePath, string requestPath)
        {
            HttpContext context = CreateRequest(basePath, requestPath);
            IBuilder builder = new Builder(serviceProvider: null);
            builder.Map(matchPath, UseSuccess);
            var app = builder.Build();
            app.Invoke(context).Wait();

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(basePath, context.Request.PathBase.Value);
            Assert.Equal(requestPath, context.Request.Path.Value);
        }

        [Theory]
        [InlineData("/foo", "", "/foo")]
        [InlineData("/foo", "", "/foo/")]
        [InlineData("/foo", "/Bar", "/foo")]
        [InlineData("/foo", "/Bar", "/foo/cho")]
        [InlineData("/foo", "/Bar", "/foo/cho/")]
        [InlineData("/foo/cho", "/Bar", "/foo/cho")]
        [InlineData("/foo/cho", "/Bar", "/foo/cho/do")]
        public void PathMatchAction_BranchTaken(string matchPath, string basePath, string requestPath)
        {
            HttpContext context = CreateRequest(basePath, requestPath);
            IBuilder builder = new Builder(serviceProvider: null);
            builder.Map(matchPath, subBuilder => subBuilder.Run(Success));
            var app = builder.Build();
            app.Invoke(context).Wait();

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(basePath + matchPath, context.Items["test.PathBase"]);
            Assert.Equal(requestPath.Substring(matchPath.Length), context.Items["test.Path"]);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/foo/")]
        [InlineData("/foo/cho/")]
        public void MatchPathWithTrailingSlashThrowsException(string matchPath)
        {
            Should.Throw<ArgumentException>(() => new Builder(serviceProvider: null).Map(matchPath, map => { }).Build());
        }

        [Theory]
        [InlineData("/foo", "", "")]
        [InlineData("/foo", "/bar", "")]
        [InlineData("/foo", "", "/bar")]
        [InlineData("/foo", "/foo", "")]
        [InlineData("/foo", "/foo", "/bar")]
        [InlineData("/foo", "", "/bar/foo")]
        [InlineData("/foo/bar", "/foo", "/bar")]
        public void PathMismatchFunc_PassedThrough(string matchPath, string basePath, string requestPath)
        {
            HttpContext context = CreateRequest(basePath, requestPath);
            IBuilder builder = new Builder(serviceProvider: null);
            builder.Map(matchPath, UseNotImplemented);
            builder.Run(Success);
            var app = builder.Build();
            app.Invoke(context).Wait();

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(basePath, context.Request.PathBase.Value);
            Assert.Equal(requestPath, context.Request.Path.Value);
        }

        [Theory]
        [InlineData("/foo", "", "")]
        [InlineData("/foo", "/bar", "")]
        [InlineData("/foo", "", "/bar")]
        [InlineData("/foo", "/foo", "")]
        [InlineData("/foo", "/foo", "/bar")]
        [InlineData("/foo", "", "/bar/foo")]
        [InlineData("/foo/bar", "/foo", "/bar")]
        public void PathMismatchAction_PassedThrough(string matchPath, string basePath, string requestPath)
        {
            HttpContext context = CreateRequest(basePath, requestPath);
            IBuilder builder = new Builder(serviceProvider: null);
            builder.Map(matchPath, UseNotImplemented);
            builder.Run(Success);
            var app = builder.Build();
            app.Invoke(context).Wait();

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(basePath, context.Request.PathBase.Value);
            Assert.Equal(requestPath, context.Request.Path.Value);
        }

        [Fact]
        public void ChainedRoutes_Success()
        {
            IBuilder builder = new Builder(serviceProvider: null);
            builder.Map("/route1", map =>
            {
                map.Map((string)"/subroute1", UseSuccess);
                map.Run(NotImplemented);
            });
            builder.Map("/route2/subroute2", UseSuccess);
            var app = builder.Build();

            HttpContext context = CreateRequest(string.Empty, "/route1");
            Assert.Throws<AggregateException>(() => app.Invoke(context).Wait());

            context = CreateRequest(string.Empty, "/route1/subroute1");
            app.Invoke(context).Wait();
            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(string.Empty, context.Request.PathBase.Value);
            Assert.Equal("/route1/subroute1", context.Request.Path.Value);

            context = CreateRequest(string.Empty, "/route2");
            app.Invoke(context).Wait();
            Assert.Equal(404, context.Response.StatusCode);
            Assert.Equal(string.Empty, context.Request.PathBase.Value);
            Assert.Equal("/route2", context.Request.Path.Value);

            context = CreateRequest(string.Empty, "/route2/subroute2");
            app.Invoke(context).Wait();
            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(string.Empty, context.Request.PathBase.Value);
            Assert.Equal("/route2/subroute2", context.Request.Path.Value);

            context = CreateRequest(string.Empty, "/route2/subroute2/subsub2");
            app.Invoke(context).Wait();
            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(string.Empty, context.Request.PathBase.Value);
            Assert.Equal("/route2/subroute2/subsub2", context.Request.Path.Value);
        }

        private HttpContext CreateRequest(string basePath, string requestPath)
        {
            HttpContext context = new DefaultHttpContext(new FeatureModel.FeatureCollection());
            context.SetFeature<IHttpRequestFeature>(new FakeHttpRequestFeature());
            context.SetFeature<IHttpResponseFeature>(new FakeHttpResponseFeature());
            context.Request.PathBase = new PathString(basePath);
            context.Request.Path = new PathString(requestPath);
            return context;
        }
    }
}
