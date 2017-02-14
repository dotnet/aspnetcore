// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Http
{
    public class UseMiddlewareTest
    {
        [Fact]
        public void UseMiddleware_WithNoParameters_ThrowsException()
        {
            var mockServiceProvider = new DummyServiceProvider();
            var builder = new ApplicationBuilder(mockServiceProvider);
            builder.UseMiddleware(typeof(MiddlewareNoParametersStub));
            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

            Assert.Equal(Resources.FormatException_UseMiddlewareNoParameters("Invoke", nameof(HttpContext)), exception.Message);
        }

        [Fact]
        public void UseMiddleware_NonTaskReturnType_ThrowsException()
        {
            var mockServiceProvider = new DummyServiceProvider();
            var builder = new ApplicationBuilder(mockServiceProvider);
            builder.UseMiddleware(typeof(MiddlewareNonTaskReturnStub));
            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Equal(Resources.FormatException_UseMiddlewareNonTaskReturnType("Invoke", nameof(Task)), exception.Message);
        }

        [Fact]
        public void UseMiddleware_NoInvokeMethod_ThrowsException()
        {
            var mockServiceProvider = new DummyServiceProvider();
            var builder = new ApplicationBuilder(mockServiceProvider);
            builder.UseMiddleware(typeof(MiddlewareNoInvokeStub));
            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Equal(Resources.FormatException_UseMiddlewareNoInvokeMethod("Invoke"), exception.Message);
        }

        [Fact]
        public void UseMiddleware_MutlipleInvokeMethods_ThrowsException()
        {
            var mockServiceProvider = new DummyServiceProvider();
            var builder = new ApplicationBuilder(mockServiceProvider);
            builder.UseMiddleware(typeof(MiddlewareMultipleInvokesStub));
            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Equal(Resources.FormatException_UseMiddleMutlipleInvokes("Invoke"), exception.Message);
        }

        [Fact]
        public async Task UseMiddleware_ThrowsIfArgCantBeResolvedFromContainer()
        {
            var mockServiceProvider = new DummyServiceProvider();
            var builder = new ApplicationBuilder(mockServiceProvider);
            builder.UseMiddleware(typeof(MiddlewareInjectInvokeNoService));
            var app = builder.Build();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app(new DefaultHttpContext()));
            Assert.Equal(Resources.FormatException_InvokeMiddlewareNoService(typeof(object), typeof(MiddlewareInjectInvokeNoService)), exception.Message);
        }

        [Fact]
        public void UseMiddlewareWithInvokeArg()
        {
            var mockServiceProvider = new DummyServiceProvider();
            var builder = new ApplicationBuilder(mockServiceProvider);
            builder.UseMiddleware(typeof(MiddlewareInjectInvoke));
            var app = builder.Build();
            app(new DefaultHttpContext());
        }

        [Fact]
        public void UseMiddlewareWithIvokeWithOutAndRefThrows()
        {
            var mockServiceProvider = new DummyServiceProvider();
            var builder = new ApplicationBuilder(mockServiceProvider);
            builder.UseMiddleware(typeof(MiddlewareInjectWithOutAndRefParams));
            var exception = Assert.Throws<NotSupportedException>(() => builder.Build());
        }

        [Fact]
        public void UseMiddlewareWithIMiddlewareThrowsIfParametersSpecified()
        {
            var mockServiceProvider = new DummyServiceProvider();
            var builder = new ApplicationBuilder(mockServiceProvider);
            var exception = Assert.Throws<NotSupportedException>(() => builder.UseMiddleware(typeof(Middleware), "arg"));
            Assert.Equal(Resources.FormatException_UseMiddlewareExplicitArgumentsNotSupported(typeof(IMiddleware)), exception.Message);
        }

        [Fact]
        public async Task UseMiddlewareWithIMiddlewareThrowsIfNoIMiddlewareFactoryRegistered()
        {
            var mockServiceProvider = new DummyServiceProvider();
            var builder = new ApplicationBuilder(mockServiceProvider);
            builder.UseMiddleware(typeof(Middleware));
            var app = builder.Build();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var context = new DefaultHttpContext();
                var sp = new DummyServiceProvider();
                context.RequestServices = sp;
                await app(context);
            });
            Assert.Equal(Resources.FormatException_UseMiddlewareNoMiddlewareFactory(typeof(IMiddlewareFactory)), exception.Message);
        }

        [Fact]
        public async Task UseMiddlewareWithIMiddlewareThrowsIfMiddlewareFactoryCreateReturnsNull()
        {
            var mockServiceProvider = new DummyServiceProvider();
            var builder = new ApplicationBuilder(mockServiceProvider);
            builder.UseMiddleware(typeof(Middleware));
            var app = builder.Build();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var context = new DefaultHttpContext();
                var sp = new DummyServiceProvider();
                sp.AddService(typeof(IMiddlewareFactory), new BadMiddlewareFactory());
                context.RequestServices = sp;
                await app(context);
            });

            Assert.Equal(Resources.FormatException_UseMiddlewareUnableToCreateMiddleware(typeof(BadMiddlewareFactory), typeof(Middleware)), exception.Message);
        }

        [Fact]
        public async Task UseMiddlewareWithIMiddlewareWorks()
        {
            var mockServiceProvider = new DummyServiceProvider();
            var builder = new ApplicationBuilder(mockServiceProvider);
            builder.UseMiddleware(typeof(Middleware));
            var app = builder.Build();
            var context = new DefaultHttpContext();
            var sp = new DummyServiceProvider();
            var middlewareFactory = new BasicMiddlewareFactory();
            sp.AddService(typeof(IMiddlewareFactory), middlewareFactory);
            context.RequestServices = sp;
            await app(context);
            Assert.Equal(true, context.Items["before"]);
            Assert.Equal(true, context.Items["after"]);
            Assert.NotNull(middlewareFactory.Created);
            Assert.NotNull(middlewareFactory.Released);
            Assert.IsType(typeof(Middleware), middlewareFactory.Created);
            Assert.IsType(typeof(Middleware), middlewareFactory.Released);
            Assert.Same(middlewareFactory.Created, middlewareFactory.Released);
        }

        public class Middleware : IMiddleware
        {
            public async Task Invoke(HttpContext context, RequestDelegate next)
            {
                context.Items["before"] = true;
                await next(context);
                context.Items["after"] = true;
            }
        }

        public class BasicMiddlewareFactory : IMiddlewareFactory
        {
            public IMiddleware Created { get; private set; }
            public IMiddleware Released { get; private set; }

            public IMiddleware Create(Type middlewareType)
            {
                Created = Activator.CreateInstance(middlewareType) as IMiddleware;
                return Created;
            }

            public void Release(IMiddleware middleware)
            {
                Released = middleware;
            }
        }

        public class BadMiddlewareFactory : IMiddlewareFactory
        {
            public IMiddleware Create(Type middlewareType)
            {
                return null;
            }

            public void Release(IMiddleware middleware)
            {

            }
        }

        private class DummyServiceProvider : IServiceProvider
        {
            private Dictionary<Type, object> _services = new Dictionary<Type, object>();

            public void AddService(Type type, object value) => _services[type] = value;

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IServiceProvider))
                {
                    return this;
                }

                if (_services.TryGetValue(serviceType, out object value))
                {
                    return value;
                }
                return null;
            }
        }

        public class MiddlewareInjectWithOutAndRefParams
        {
            public MiddlewareInjectWithOutAndRefParams(RequestDelegate next)
            {
            }

            public Task Invoke(HttpContext context, ref IServiceProvider sp1, out IServiceProvider sp2)
            {
                sp1 = null;
                sp2 = null;
                return Task.FromResult(0);
            }
        }

        private class MiddlewareInjectInvokeNoService
        {
            public MiddlewareInjectInvokeNoService(RequestDelegate next)
            {
            }

            public Task Invoke(HttpContext context, object value)
            {
                return Task.FromResult(0);
            }
        }

        private class MiddlewareInjectInvoke
        {
            public MiddlewareInjectInvoke(RequestDelegate next)
            {
            }

            public Task Invoke(HttpContext context, IServiceProvider provider)
            {
                return Task.FromResult(0);
            }
        }

        private class MiddlewareNoParametersStub
        {
            public MiddlewareNoParametersStub(RequestDelegate next)
            {
            }

            public Task Invoke()
            {
                return Task.FromResult(0);
            }
        }

        private class MiddlewareNonTaskReturnStub
        {
            public MiddlewareNonTaskReturnStub(RequestDelegate next)
            {
            }

            public int Invoke()
            {
                return 0;
            }
        }

        private class MiddlewareNoInvokeStub
        {
            public MiddlewareNoInvokeStub(RequestDelegate next)
            {
            }
        }

        private class MiddlewareMultipleInvokesStub
        {
            public MiddlewareMultipleInvokesStub(RequestDelegate next)
            {
            }

            public Task Invoke(HttpContext context)
            {
                return Task.FromResult(0);
            }

            public Task Invoke(HttpContext context, int i)
            {
                return Task.FromResult(0);
            }
        }
    }
}