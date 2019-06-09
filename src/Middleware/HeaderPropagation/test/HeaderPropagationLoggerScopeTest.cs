// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.HeaderPropagation.Tests
{
    public class HeaderPropagationLoggerScopeTest
    {
        [Fact]
        public void NoPropagatedHeaders_EmptyScope()
        {
            // Arrange
            var headerNames = new List<string>();
            var headerValues = new Dictionary<string, StringValues>();

            // Act
            var scope = new HeaderPropagationLoggerScope(headerNames, headerValues);

            // Assert
            Assert.Empty(scope);
            Assert.Empty(scope.ToString());
        }

        [Fact]
        public void PropagatedHeaderHasValue_AddsValueInScope()
        {
            // Arrange
            var headerNames = new List<string> { "foo" };
            var headerValues = new Dictionary<string, StringValues> { ["foo"] = "bar" };

            // Act
            var scope = new HeaderPropagationLoggerScope(headerNames, headerValues);

            // Assert
            Assert.Single(scope);
            var entry = scope[0];
            Assert.Equal("foo", entry.Key);
            Assert.IsType<StringValues>(entry.Value);
            Assert.Equal("bar", (StringValues)entry.Value);
            Assert.Equal("foo:bar", scope.ToString());
        }

        [Fact]
        public void PropagatedHeaderHasNoValue_AddsValueInScopeWithoutValue()
        {
            // Arrange
            var headerNames = new List<string> { "foo" };
            var headerValues = new Dictionary<string, StringValues>();

            // Act
            var scope = new HeaderPropagationLoggerScope(headerNames, headerValues);

            // Assert
            Assert.Single(scope);
            var entry = scope[0];
            Assert.Equal("foo", entry.Key);
            Assert.IsType<StringValues>(entry.Value);
            Assert.Empty((StringValues)entry.Value);
            Assert.Equal("foo:", scope.ToString());
        }

        [Fact]
        public void MultiplePropagatedHeadersHaveValue_AddsAllInScopeInOrder()
        {
            // Arrange
            var headerNames = new List<string> { "foo", "answer" };
            var headerValues = new Dictionary<string, StringValues> {
                ["foo"] = "bar",
                ["answer"] = "42"
            };

            // Act
            var scope = new HeaderPropagationLoggerScope(headerNames, headerValues);

            // Assert
            Assert.Equal(2, scope.Count);
            var entry = scope[0];
            Assert.Equal("foo", entry.Key);
            Assert.IsType<StringValues>(entry.Value);
            Assert.Equal("bar", (StringValues)entry.Value);
            entry = scope[1];
            Assert.Equal("answer", entry.Key);
            Assert.IsType<StringValues>(entry.Value);
            Assert.Equal("42", (StringValues)entry.Value);
            Assert.Equal("foo:bar answer:42", scope.ToString());
        }
    }
}
