// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.HeaderPropagation.Tests
{
    public class HeaderPropagationLoggerScopeBuilderTest
    {
        public HeaderPropagationLoggerScopeBuilderTest()
        {
            Options = new HeaderPropagationOptions();
            Values = new HeaderPropagationValues()
            {
                Headers = new Dictionary<string, StringValues>()
            };
        }

        public HeaderPropagationOptions Options { get; set; }
        public HeaderPropagationValues Values { get; set; }

        [Fact]
        public void NoPropagatedHeaders_EmptyScope()
        {
            // Arrange
            IHeaderPropagationLoggerScopeBuilder builder = CreateBuilder();

            // Act
            var scope = builder.Build();

            // Assert
            Assert.Empty(scope);
            Assert.Empty(scope.ToString());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void PropagatedHeaderHasValue_AddValueInScope_WithoutDuplicates(int times)
        {
            // Arrange
            for (var i = 0; i < times; i++)
            {
                Options.Headers.Add("foo");
            }
            Values.Headers.Add("foo", "bar");
            IHeaderPropagationLoggerScopeBuilder builder = new HeaderPropagationLoggerScopeBuilder(
                new OptionsWrapper<HeaderPropagationOptions>(Options),
                Values);

            // Act
            var scope = builder.Build();

            // Assert
            Assert.Single(scope);
            var entry = scope[0];
            Assert.Equal("foo", entry.Key);
            Assert.IsType<StringValues>(entry.Value);
            Assert.Equal("bar", (StringValues)entry.Value);
            Assert.Equal("foo:bar", scope.ToString());
        }

        private IHeaderPropagationLoggerScopeBuilder CreateBuilder() =>
            new HeaderPropagationLoggerScopeBuilder(new OptionsWrapper<HeaderPropagationOptions>(Options), Values);

    }
}
