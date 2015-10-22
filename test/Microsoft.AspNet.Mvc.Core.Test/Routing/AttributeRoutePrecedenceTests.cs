// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Extensions.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Routing
{
    public class AttributeRoutePrecedenceTests
    {
        [Theory]
        [InlineData("Employees/{id}", "Employees/{employeeId}")]
        [InlineData("abc", "def")]
        [InlineData("{x:alpha}", "{x:int}")]
        public void ComputeMatched_IsEqual(string xTemplate, string yTemplate)
        {
            // Arrange & Act
            var xPrededence = ComputeMatched(xTemplate);
            var yPrededence = ComputeMatched(yTemplate);

            // Assert
            Assert.Equal(xPrededence, yPrededence);
        }

        [Theory]
        [InlineData("Employees/{id}", "Employees/{employeeId}")]
        [InlineData("abc", "def")]
        [InlineData("{x:alpha}", "{x:int}")]
        public void ComputeGenerated_IsEqual(string xTemplate, string yTemplate)
        {
            // Arrange & Act
            var xPrededence = ComputeGenerated(xTemplate);
            var yPrededence = ComputeGenerated(yTemplate);

            // Assert
            Assert.Equal(xPrededence, yPrededence);
        }

        [Theory]
        [InlineData("abc", "a{x}")]
        [InlineData("abc", "{x}c")]
        [InlineData("abc", "{x:int}")]
        [InlineData("abc", "{x}")]
        [InlineData("abc", "{*x}")]
        [InlineData("{x:int}", "{x}")]
        [InlineData("{x:int}", "{*x}")]
        [InlineData("a{x}", "{x}")]
        [InlineData("{x}c", "{x}")]
        [InlineData("a{x}", "{*x}")]
        [InlineData("{x}c", "{*x}")]
        [InlineData("{x}", "{*x}")]
        [InlineData("{*x:maxlength(10)}", "{*x}")]
        [InlineData("abc/def", "abc/{x:int}")]
        [InlineData("abc/def", "abc/{x}")]
        [InlineData("abc/def", "abc/{*x}")]
        [InlineData("abc/{x:int}", "abc/{x}")]
        [InlineData("abc/{x:int}", "abc/{*x}")]
        [InlineData("abc/{x}", "abc/{*x}")]
        [InlineData("{x}/{y:int}", "{x}/{y}")]
        public void ComputeMatched_IsLessThan(string xTemplate, string yTemplate)
        {
            // Arrange & Act
            var xPrededence = ComputeMatched(xTemplate);
            var yPrededence = ComputeMatched(yTemplate);

            // Assert
            Assert.True(xPrededence < yPrededence);
        }

        [Theory]
        [InlineData("abc", "a{x}")]
        [InlineData("abc", "{x}c")]
        [InlineData("abc", "{x:int}")]
        [InlineData("abc", "{x}")]
        [InlineData("abc", "{*x}")]
        [InlineData("{x:int}", "{x}")]
        [InlineData("{x:int}", "{*x}")]
        [InlineData("a{x}", "{x}")]
        [InlineData("{x}c", "{x}")]
        [InlineData("a{x}", "{*x}")]
        [InlineData("{x}c", "{*x}")]
        [InlineData("{x}", "{*x}")]
        [InlineData("{*x:maxlength(10)}", "{*x}")]
        [InlineData("abc/def", "abc/{x:int}")]
        [InlineData("abc/def", "abc/{x}")]
        [InlineData("abc/def", "abc/{*x}")]
        [InlineData("abc/{x:int}", "abc/{x}")]
        [InlineData("abc/{x:int}", "abc/{*x}")]
        [InlineData("abc/{x}", "abc/{*x}")]
        [InlineData("{x}/{y:int}", "{x}/{y}")]
        public void ComputeGenerated_IsGreaterThan(string xTemplate, string yTemplate)
        {
            // Arrange & Act
            var xPrecedence = ComputeGenerated(xTemplate);
            var yPrecedence = ComputeGenerated(yTemplate);

            // Assert
            Assert.True(xPrecedence > yPrecedence);
        }

        private static decimal ComputeMatched(string template)
        {
            return Compute(template, AttributeRoutePrecedence.ComputeMatched);
        }
        private static decimal ComputeGenerated(string template)
        {
            return Compute(template, AttributeRoutePrecedence.ComputeGenerated);
        }

        private static decimal Compute(string template, Func<RouteTemplate, decimal> func)
        {
            var options = new Mock<IOptions<RouteOptions>>();
            options.SetupGet(o => o.Value).Returns(new RouteOptions());

            var parsed = TemplateParser.Parse(template);
            return func(parsed);
        }
    }
}
#endif
