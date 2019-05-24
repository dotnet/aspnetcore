// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    public class RuntimeCompilationFileProviderTest
    {
        [Fact]
        public void GetFileProvider_ThrowsIfNoConfiguredFileProviders()
        {
            // Arrange
            var expected =
                $"'{typeof(MvcRazorRuntimeCompilationOptions).FullName}.{nameof(MvcRazorRuntimeCompilationOptions.FileProviders)}' must " +
                $"not be empty. At least one '{typeof(IFileProvider).FullName}' is required to locate a view for " +
                "rendering.";
            var options = Options.Create(new MvcRazorRuntimeCompilationOptions());

            var fileProvider = new RuntimeCompilationFileProvider(options);
            
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => fileProvider.FileProvider);
            Assert.Equal(expected, exception.Message);
        }
    }
}
