// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class RazorViewCompilerProviderTest
    {
        [Fact]
        public void GetCompiler_ThrowsIfNullFileProvider()
        {
            // Arrange
            var expected =
                $"'{typeof(RazorViewEngineOptions).FullName}.{nameof(RazorViewEngineOptions.FileProviders)}' must " +
                $"not be empty. At least one '{typeof(IFileProvider).FullName}' is required to locate a view for " +
                "rendering.";
            var fileProvider = new NullFileProvider();
            var accessor = new Mock<IRazorViewEngineFileProviderAccessor>();
            var applicationManager = new ApplicationPartManager();
            var options = new TestOptionsManager<RazorViewEngineOptions>();
            var referenceManager = new DefaultRazorReferenceManager(applicationManager, options);
            accessor.Setup(a => a.FileProvider).Returns(fileProvider);
            var provider = new RazorViewCompilerProvider(
                applicationManager,
                new RazorTemplateEngine(RazorEngine.Create(), new FileProviderRazorProject(fileProvider)),
                accessor.Object,
                new CSharpCompiler(referenceManager, options),
                options,
                NullLoggerFactory.Instance);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => provider.GetCompiler());
            Assert.Equal(expected, exception.Message);
        }
    }
}
