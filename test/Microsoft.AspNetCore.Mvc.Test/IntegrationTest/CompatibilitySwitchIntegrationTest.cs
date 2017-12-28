// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTest
{
    // Integration tests for compatibility switches. These tests verify which compatibility
    // values apply to each supported version.
    //
    // If you add a new compatibility switch, make sure to update ALL of these tests. Each test
    // here should include verification for all of the switches.
    public class CompatibilitySwitchIntegrationTest
    {
        [Fact(Skip = "#7157 - some settings have the wrong values, this test should pass once #7157 is fixed")]
        public void CompatibilitySwitches_Version_2_0()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            AddHostingServices(serviceCollection);
            serviceCollection.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_0);

            var services = serviceCollection.BuildServiceProvider();

            // Act
            var mvcOptions = services.GetRequiredService<IOptions<MvcOptions>>().Value;

            // Assert
            Assert.False(mvcOptions.AllowBindingUndefinedValueToEnumType);
            Assert.Equal(InputFormatterExceptionModelStatePolicy.AllExceptions, mvcOptions.InputFormatterExceptionModelStatePolicy);
            Assert.False(mvcOptions.SuppressJsonDeserializationExceptionMessagesInModelState); // This name needs to be inverted in #7157
        }

        [Fact(Skip = "#7157 - some settings have the wrong values, this test should pass once #7157 is fixed")]
        public void CompatibilitySwitches_Version_2_1()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            AddHostingServices(serviceCollection);
            serviceCollection.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_0);

            var services = serviceCollection.BuildServiceProvider();

            // Act
            var mvcOptions = services.GetRequiredService<IOptions<MvcOptions>>().Value;

            // Assert
            Assert.True(mvcOptions.AllowBindingUndefinedValueToEnumType);
            Assert.Equal(InputFormatterExceptionModelStatePolicy.MalformedInputExceptions, mvcOptions.InputFormatterExceptionModelStatePolicy);
            Assert.True(mvcOptions.SuppressJsonDeserializationExceptionMessagesInModelState); // This name needs to be inverted in #7157
        }

        [Fact(Skip = "#7157 - some settings have the wrong values, this test should pass once #7157 is fixed")]
        public void CompatibilitySwitches_Version_Latest()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            AddHostingServices(serviceCollection);
            serviceCollection.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_0);

            var services = serviceCollection.BuildServiceProvider();

            // Act
            var mvcOptions = services.GetRequiredService<IOptions<MvcOptions>>().Value;

            // Assert
            Assert.True(mvcOptions.AllowBindingUndefinedValueToEnumType);
            Assert.Equal(InputFormatterExceptionModelStatePolicy.MalformedInputExceptions, mvcOptions.InputFormatterExceptionModelStatePolicy);
            Assert.True(mvcOptions.SuppressJsonDeserializationExceptionMessagesInModelState); // This name needs to be inverted in #7157
        }

        // This just does the minimum needed to be able to resolve these options.
        private static void AddHostingServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        }
    }
}
