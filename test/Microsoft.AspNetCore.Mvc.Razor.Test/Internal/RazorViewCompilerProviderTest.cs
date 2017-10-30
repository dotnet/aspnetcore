// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
            var accessor = Mock.Of<IRazorViewEngineFileProviderAccessor>(a => a.FileProvider == fileProvider);

            var partManager = new ApplicationPartManager();
            var options = Options.Create(new RazorViewEngineOptions());

            var referenceManager = new DefaultRazorReferenceManager(partManager, options);

            var provider = new RazorViewCompilerProvider(
                partManager,
                new RazorTemplateEngine(
                    RazorEngine.Create(), 
                    new FileProviderRazorProject(accessor)),
                accessor,
                new CSharpCompiler(referenceManager, Mock.Of<IHostingEnvironment>()),
                options,
                NullLoggerFactory.Instance);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => provider.GetCompiler());
            Assert.Equal(expected, exception.Message);
        }
    }
}
