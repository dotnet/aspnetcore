// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.DependencyInjection
{
    public class MvcCoreBuilderExtensionsTest
    {
        [Fact]
        public void AddApplicationPart_AddsAnApplicationPart_ToTheListOfPartsOnTheBuilder()
        {
            // Arrange
            var manager = new ApplicationPartManager();
            var builder = new MvcCoreBuilder(Mock.Of<IServiceCollection>(), manager);
            var assembly = typeof(MvcCoreBuilder).GetTypeInfo().Assembly;

            // Act
            var result = builder.AddApplicationPart(assembly);

            // Assert
            Assert.Same(result, builder);
            var part = Assert.Single(builder.PartManager.ApplicationParts);
            var assemblyPart = Assert.IsType<AssemblyPart>(part);
            Assert.Equal(assembly, assemblyPart.Assembly);
        }

        [Fact]
        public void ConfigureApplicationParts_InvokesSetupAction()
        {
            // Arrange
            var builder = new MvcCoreBuilder(
                Mock.Of<IServiceCollection>(),
                new ApplicationPartManager());

            var part = new TestApplicationPart();

            // Act
            var result = builder.ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Add(part);
            });

            // Assert
            Assert.Same(result, builder);
            Assert.Equal(new ApplicationPart[] { part }, builder.PartManager.ApplicationParts.ToArray());
        }
    }
}
