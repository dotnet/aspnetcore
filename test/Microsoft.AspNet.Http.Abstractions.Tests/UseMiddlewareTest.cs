// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Builder.Internal;
using Microsoft.AspNet.Http.Abstractions;
using Microsoft.AspNet.Http.Internal;
using Xunit;

namespace Microsoft.AspNet.Http
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

        private class DummyServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IServiceProvider))
                {
                    return this;
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