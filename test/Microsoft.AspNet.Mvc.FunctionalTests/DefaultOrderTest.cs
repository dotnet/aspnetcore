// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    // Tests that various MVC services have the correct order.
    public class DefaultOrderTest
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices(nameof(BasicWebSite));
        private readonly Action<IApplicationBuilder> _app = new BasicWebSite.Startup().Configure;

        [Theory]
        [InlineData(typeof(INestedProvider<ActionDescriptorProviderContext>), typeof(ControllerActionDescriptorProvider), -1000)]
        [InlineData(typeof(INestedProvider<ActionInvokerProviderContext>), (Type)null, -1000)]
        [InlineData(typeof(INestedProvider<ApiDescriptionProviderContext>), (Type)null, -1000)]
        [InlineData(typeof(INestedProvider<FilterProviderContext>), (Type)null, -1000)]
        [InlineData(typeof(INestedProvider<ViewComponentInvokerProviderContext>), (Type)null, -1000)]
        [InlineData(typeof(IConfigureOptions<RazorViewEngineOptions>), (Type)null, -1000)]
        [InlineData(typeof(IConfigureOptions<MvcOptions>), (Type)null, -1000)]
        public async Task ServiceOrder_GetOrder(Type serviceType, Type actualType, int order)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var url = "http://localhost/Order/GetServiceOrder?serviceType=" + serviceType.AssemblyQualifiedName;

            if (actualType != null)
            {
                url += "&actualType=" + actualType.AssemblyQualifiedName;
            }

            // Act
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(order, int.Parse(content));
        }
    }
}