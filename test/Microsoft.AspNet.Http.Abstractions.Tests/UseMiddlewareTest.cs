// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Builder.Internal;
using Microsoft.AspNet.Http.Abstractions;
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

            Assert.Equal(Resources.FormatException_UseMiddlewareNoParameters("Invoke",nameof(HttpContext)), exception.Message); 
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

        private class DummyServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                return null;
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