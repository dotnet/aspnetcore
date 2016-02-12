// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerAsServicesTest
    {
        [Fact]
        public void AddControllerAsServices_MultipleCalls_RetainsPreviouslyAddedTypes()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act 1
            ControllersAsServices.AddControllersAsServices(services, new Type[] { typeof(ControllerOne) });

            // Assert 1
            var serviceDescriptor = Assert.Single(services, s => s.ServiceType == typeof(IControllerTypeProvider));
            var controllerTypeProvider = Assert.IsType<StaticControllerTypeProvider>(serviceDescriptor.ImplementationInstance);
            var expectedControllerType = Assert.Single(controllerTypeProvider.ControllerTypes);

            // Act 2
            ControllersAsServices.AddControllersAsServices(services, new Type[] { typeof(ControllerTwo) });

            // Assert 2
            serviceDescriptor = Assert.Single(services, s => s.ServiceType == typeof(IControllerTypeProvider));
            controllerTypeProvider = Assert.IsType<StaticControllerTypeProvider>(serviceDescriptor.ImplementationInstance);
            Assert.Equal(2, controllerTypeProvider.ControllerTypes.Count);
            Assert.Same(expectedControllerType, controllerTypeProvider.ControllerTypes[0]);
            Assert.Same(typeof(ControllerTwo), controllerTypeProvider.ControllerTypes[1]);
        }

        private class ControllerOne
        {
        }

        private class ControllerTwo
        {
        }
    }
}
