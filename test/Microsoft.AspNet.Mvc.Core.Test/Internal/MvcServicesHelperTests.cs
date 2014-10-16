// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class MvcServicesHelperTests
    {
        [Fact]
        public void MvcServicesHelperThrowsIfServiceIsAbsent()
        {
            // Arrange
            var services = new Mock<IServiceProvider>();
            services.Setup(o => o.GetService(typeof(IEnumerable<MvcMarkerService>)))
                .Returns(new List<MvcMarkerService>());
            var expectedMessage = "Unable to find the required services. Please add all the required " +
                "services by calling 'IServiceCollection.AddMvc()' inside the call to 'IApplicationBuilder.UseServices(...)' " +
                "or 'IApplicationBuilder.UseMvc(...)' in the application startup code.";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => MvcServicesHelper.ThrowIfMvcNotRegistered(services.Object));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void MvcServicesHelperDoesNotThrowIfServiceExists()
        {
            // Arrange
            var services = new Mock<IServiceProvider>();
            var expectedOutput = new MvcMarkerService();
            services.Setup(o => o.GetService(typeof(MvcMarkerService)))
                .Returns(expectedOutput);

            // Act & Assert
            Assert.DoesNotThrow(() => MvcServicesHelper.ThrowIfMvcNotRegistered(services.Object));
        }
    }
}