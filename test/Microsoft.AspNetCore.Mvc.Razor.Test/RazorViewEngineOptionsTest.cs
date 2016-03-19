// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class RazorViewEngineOptionsTest
    {
        [Fact]
        public void AddRazorOptions_ConfiguresOptionsAsExpected()
        {
            // Arrange
            var services = new ServiceCollection().AddOptions();
            var fileProvider = new TestFileProvider();

            // Act
            var builder = new MvcBuilder(services, new ApplicationPartManager());
            builder.AddRazorOptions(options =>
            {
                options.FileProviders.Add(fileProvider);
            });
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var accessor = serviceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>();
            Assert.Same(fileProvider, accessor.Value.FileProviders[0]);
        }
    }
}