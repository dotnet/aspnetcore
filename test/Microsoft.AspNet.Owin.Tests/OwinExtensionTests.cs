// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Owin
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using CreateMiddleware = Func<
          Func<IDictionary<string, object>, Task>,
          Func<IDictionary<string, object>, Task>
        >;
    using AddMiddleware = Action<Func<
          Func<IDictionary<string, object>, Task>,
          Func<IDictionary<string, object>, Task>
        >>;

    public class OwinExtensionTests
    {
        static AppFunc notFound = env => new Task(() => { env["owin.ResponseStatusCode"] = 404; });

        [Fact]
        public void OwinConfigureServiceProviderAddsServices()
        {
            var list = new List<CreateMiddleware>();
            AddMiddleware build = list.Add;
            IServiceProvider serviceProvider = null;
            FakeService fakeService = null;

            var builder = build.UseBuilder(applicationBuilder =>
            {
                serviceProvider = applicationBuilder.ApplicationServices;
                applicationBuilder.Run(async context =>
                {
                    fakeService = context.RequestServices.GetService<FakeService>();
                });
            }, new ServiceCollection().AddSingleton(new FakeService()).BuildServiceProvider());

            list.Reverse();
            list.Aggregate(notFound, (next, middleware) => middleware(next)).Invoke(new Dictionary<string, object>());

            Assert.NotNull(fakeService);
            Assert.NotNull(serviceProvider.GetService<FakeService>());
        }

        [Fact]
        public void OwinDefaultNoServices()
        {
            var list = new List<CreateMiddleware>();
            AddMiddleware build = list.Add;
            IServiceProvider serviceProvider = null;
            FakeService fakeService = null;
            bool builderExecuted = false;
            bool applicationExecuted = false;

            var builder = build.UseBuilder(applicationBuilder =>
            {
                builderExecuted = true;
                serviceProvider = applicationBuilder.ApplicationServices;
                applicationBuilder.Run(async context =>
                {
                    applicationExecuted = true;
                    fakeService = context.RequestServices.GetService<FakeService>();
                });
            });

            list.Reverse();
            list.Aggregate(notFound, (next, middleware) => middleware(next)).Invoke(new Dictionary<string, object>());

            Assert.True(builderExecuted);
            Assert.Null(fakeService);
            Assert.True(applicationExecuted);
            Assert.Null(serviceProvider);
        }

        private class FakeService
        {

        }
    }
}
