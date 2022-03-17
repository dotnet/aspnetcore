// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

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
