// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Tests
{
    public class ConfigureBuilderTests
    {
        [Fact]
        public void CapturesServiceExceptionDetails()
        {
            var methodInfo = GetType().GetMethod(nameof(InjectedMethod), BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(methodInfo);

            var services = new ServiceCollection()
                .AddSingleton<CrasherService>()
                .BuildServiceProvider();

            var applicationBuilder = new ApplicationBuilder(services);

            var builder = new ConfigureBuilder(methodInfo);
            Action<IApplicationBuilder> action = builder.Build(instance:null);
            var ex = Assert.Throws<Exception>(() => action.Invoke(applicationBuilder));

            Assert.NotNull(ex);
            Assert.Equal($"Could not resolve a service of type '{typeof(CrasherService).FullName}' for the parameter"
                + $" 'service' of method '{methodInfo.Name}' on type '{methodInfo.DeclaringType.FullName}'.", ex.Message);

            // the inner exception contains the root cause
            Assert.NotNull(ex.InnerException);
            Assert.Equal("Service instantiation failed", ex.InnerException.Message);
            Assert.Contains(nameof(CrasherService), ex.InnerException.StackTrace);
        }

        private static void InjectedMethod(CrasherService service)
        {
            Assert.NotNull(service);
        }

        private class CrasherService
        {
            public CrasherService()
            {
                throw new Exception("Service instantiation failed");
            }
        }
    }
}
