// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Runtime;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Test
{
    public class CompilationResultTest
    {
        [Fact]
        public void EnsureSuccessful_ThrowsIfCompilationFailed()
        {
            // Arrange
            var compilationFailure = Mock.Of<ICompilationFailure>();
            var result = CompilationResult.Failed(compilationFailure);

            // Act and Assert
            Assert.Null(result.CompiledType);
            Assert.Same(compilationFailure, result.CompilationFailure);
            var exception = Assert.Throws<CompilationFailedException>(() => result.EnsureSuccessful());
            Assert.Collection(exception.CompilationFailures,
                failure => Assert.Same(compilationFailure, failure));
        }
    }
}