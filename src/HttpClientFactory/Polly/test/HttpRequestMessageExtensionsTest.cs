// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Xunit;

namespace Polly
{ 
    public class HttpRequestMessageExtensionsTest
    {
        [Fact]
        public void GetPolicyExecutionContext_Found_SetsContext()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var expected = new Context(Guid.NewGuid().ToString());
            request.Properties[HttpRequestMessageExtensions.PolicyExecutionContextKey] = expected;

            // Act
            var actual = request.GetPolicyExecutionContext();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void GetPolicyExecutionContext_NotFound_ReturnsNull()
        {
            // Arrange
            var request = new HttpRequestMessage();

            // Act
            var actual = request.GetPolicyExecutionContext();

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void GetPolicyExecutionContext_Null_ReturnsNull()
        {
            // Arrange
            var request = new HttpRequestMessage();
            request.Properties[HttpRequestMessageExtensions.PolicyExecutionContextKey] = null;

            // Act
            var actual = request.GetPolicyExecutionContext();

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void SetPolicyExecutionContext_WithValue_SetsContext()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var expected = new Context(Guid.NewGuid().ToString());

            // Act
            request.SetPolicyExecutionContext(expected);

            // Assert
            var actual = request.Properties[HttpRequestMessageExtensions.PolicyExecutionContextKey];
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetPolicyExecutionContext_WithNull_SetsNull()
        {
            // Arrange
            var request = new HttpRequestMessage();
            request.Properties[HttpRequestMessageExtensions.PolicyExecutionContextKey] = new Context(Guid.NewGuid().ToString());

            // Act
            request.SetPolicyExecutionContext(null);

            // Assert
            var actual = request.Properties[HttpRequestMessageExtensions.PolicyExecutionContextKey];
            Assert.Null(actual);
        }
    }
}
