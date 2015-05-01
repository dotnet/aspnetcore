// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Runtime;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    public class CompilationResultTest
    {
        [Fact]
        public void EnsureSuccessful_ThrowsIfCompilationFailed()
        {
            // Arrange
            var compilationFailure = Mock.Of<ICompilationFailure>();
            var failures = new[] { compilationFailure };
            var result = CompilationResult.Failed(failures);

            // Act and Assert
            Assert.Null(result.CompiledType);
            Assert.Same(failures, result.CompilationFailures);
            var exception = Assert.Throws<CompilationFailedException>(() => result.EnsureSuccessful());
            var failure = Assert.Single(exception.CompilationFailures);
            Assert.Same(compilationFailure, failure);
        }
    }
}