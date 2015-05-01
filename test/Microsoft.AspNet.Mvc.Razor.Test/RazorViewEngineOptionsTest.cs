// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewEngineOptionsTest
    {
        [Fact]
        public void FileProviderThrows_IfNullIsAssigned()
        {
            // Arrange
            var options = new RazorViewEngineOptions();

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => options.FileProvider = null);
            Assert.Equal("value", ex.ParamName);
        }

        [Fact]
        public void ConfigureRazorViewEngine_ConfiguresOptionsProperly()
        {
            // Arrange
            var services = new ServiceCollection().AddOptions();
            var fileProvider = new TestFileProvider();

            // Act
            services.ConfigureRazorViewEngine(options =>
            {
                options.FileProvider = fileProvider;
            });
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var accessor = serviceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>();
            Assert.Same(fileProvider, accessor.Options.FileProvider);
        }
    }
}