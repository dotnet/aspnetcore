// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.AspNet.PipelineCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Http.Extensions.Tests
{
    public class UseWithServicesTests
    {
        [Fact]
        public async Task CallingUseThatAlsoTakesServices()
        {
            var builder = new ApplicationBuilder(new ServiceCollection()
                .AddScoped<ITestService, TestService>()
                .BuildServiceProvider());

            ITestService theService = null;
            builder.Use<ITestService>(async (ctx, next, testService) =>
            {
                theService = testService;
                await next();
            });

            var app = builder.Build();
            await app(new DefaultHttpContext());

            Assert.IsType<TestService>(theService);
        }

        [Fact]
        public async Task ServicesArePerRequest()
        {
            var services = new ServiceCollection()
                .AddScoped<ITestService, TestService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();
            var builder = new ApplicationBuilder(services);

            builder.Use(async (ctx, next) =>
            {
                var serviceScopeFactory = services.GetRequiredService<IServiceScopeFactory>();
                using (var serviceScope = serviceScopeFactory.CreateScope())
                {
                    var priorApplicationServices = ctx.ApplicationServices;
                    var priorRequestServices = ctx.ApplicationServices;
                    ctx.ApplicationServices = services;
                    ctx.RequestServices = serviceScope.ServiceProvider;
                    try
                    {
                        await next();
                    }
                    finally
                    {
                        ctx.ApplicationServices = priorApplicationServices;
                        ctx.RequestServices = priorRequestServices;
                    }
                }
            });

            var testServicesA = new List<ITestService>();
            builder.Use(async (HttpContext ctx, Func<Task> next, ITestService testService) =>
            {
                testServicesA.Add(testService);
                await next();
            });

            var testServicesB = new List<ITestService>();
            builder.Use<ITestService>(async (ctx, next, testService) =>
            {
                testServicesB.Add(testService);
                await next();
            });

            var app = builder.Build();
            await app(new DefaultHttpContext());
            await app(new DefaultHttpContext());

            Assert.Equal(2, testServicesA.Count);
            Assert.IsType<TestService>(testServicesA[0]);
            Assert.IsType<TestService>(testServicesA[1]);

            Assert.Equal(2, testServicesB.Count);
            Assert.IsType<TestService>(testServicesB[0]);
            Assert.IsType<TestService>(testServicesB[1]);

            Assert.Same(testServicesA[0], testServicesB[0]);
            Assert.Same(testServicesA[1], testServicesB[1]);

            Assert.NotSame(testServicesA[0], testServicesA[1]);
            Assert.NotSame(testServicesB[0], testServicesB[1]);
        }

        [Fact]
        public async Task InvokeMethodWillAllowPerRequestServices()
        {
            var services = new ServiceCollection()
                .AddScoped<ITestService, TestService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();
            var builder = new ApplicationBuilder(services);
            builder.UseMiddleware<TestMiddleware>();
            var app = builder.Build();

            var ctx1 = new DefaultHttpContext();
            await app(ctx1);

            var testService = ctx1.Items[typeof(ITestService)];
            Assert.IsType<TestService>(testService);
        }
    }

    public interface ITestService
    {
    }

    public class TestService : ITestService
    {
    }

    public class TestMiddleware 
    {
        RequestDelegate _next;

        public TestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ITestService testService)
        {
            context.Items[typeof(ITestService)] = testService;
        }
    }
}