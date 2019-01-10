// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Core
{
    public class JsonResultTest
    {
        [Fact]
        public async Task ExecuteResultAsync_ThrowsIfExecutorIsNotAvailableInServices()
        {
            // Arrange
            var jsonResult = new JsonResult("Hello");
            var message = "'JsonResult.ExecuteResultAsync' requires a reference to 'Microsoft.AspNetCore.Mvc.NewtonsoftJson'. " +
                "Configure your application by adding a reference to the 'Microsoft.AspNetCore.Mvc.NewtonsoftJson' package and calling 'IMvcBuilder.AddNewtonsoftJson' " +
                "inside the call to 'ConfigureServices(...)' in the application startup code.";
            var actionContext = new ActionContext
            {
                HttpContext = new DefaultHttpContext {  RequestServices = Mock.Of<IServiceProvider>() }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => jsonResult.ExecuteResultAsync(actionContext));
            Assert.Equal(message, ex.Message);
        }
    }
}
