// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection
{
    public class RC1ForwardingActivatorTests
    {
        [Fact]
        public void CreateInstance_ForwardsToNewNamespaceIfExists()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection();
            var services = serviceCollection.BuildServiceProvider();
            var activator = services.GetActivator();
            
            // Act
            var name = "Microsoft.AspNet.DataProtection.RC1ForwardingActivatorTests+ClassWithParameterlessCtor, Microsoft.AspNet.DataProtection.Test";
            var instance = activator.CreateInstance<object>(name);

            // Assert
            Assert.IsType<ClassWithParameterlessCtor>(instance);
        }

        [Fact]
        public void CreateInstance_DoesNotForwardIfClassDoesNotExist()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection();
            var services = serviceCollection.BuildServiceProvider();
            var activator = services.GetActivator();

            // Act & Assert
            var name = "Microsoft.AspNet.DataProtection.RC1ForwardingActivatorTests+NonExistentClassWithParameterlessCtor, Microsoft.AspNet.DataProtection.Test";
            var exception = Assert.ThrowsAny<Exception>(()=> activator.CreateInstance<object>(name));

            Assert.Contains("Microsoft.AspNet.DataProtection.Test", exception.Message);
        }

        private class ClassWithParameterlessCtor
        {
        }
    }
}