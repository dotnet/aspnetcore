// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewEngineOptionsTest
    {
        [Fact]
        public void FileProviderThrows_IfNullIsAsseigned()
        {
            // Arrange
            var options = new RazorViewEngineOptions();

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => options.FileProvider = null);
            Assert.Equal("value", ex.ParamName);
        }

        [Fact]
        public void ConfigureRazorViewEngineOptions_ConfiguresOptionsProperly()
        {
            // Arrange
            var services = new ServiceCollection().AddOptions();
            var timeSpan = new TimeSpan(400);

            // Act
            services.ConfigureRazorViewEngineOptions(options => {
                options.ExpirationBeforeCheckingFilesOnDisk = timeSpan;
            });
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var accessor = serviceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>();
            var expiration = Assert.IsType<TimeSpan>(accessor.Options.ExpirationBeforeCheckingFilesOnDisk);
            Assert.Equal(timeSpan, expiration);
        }
    }
}