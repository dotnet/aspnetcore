// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.OptionsModel;
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
        public void Compute_IsEqual(string xTemplate, string yTemplate)
        {
            // Arrange & Act
            var xPrededence = Compute(xTemplate);
            var yPrededence = Compute(yTemplate);

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
        public void Compute_IsLessThan(string xTemplate, string yTemplate)
        {
            // Arrange & Act
            var xPrededence = Compute(xTemplate);
            var yPrededence = Compute(yTemplate);

            // Assert
            Assert.True(xPrededence < yPrededence);
        }

        private static decimal Compute(string template)
        {
            var options = new Mock<IOptions<RouteOptions>>();
            options.SetupGet(o => o.Value).Returns(new RouteOptions());

            var parsed = TemplateParser.Parse(template);
            return AttributeRoutePrecedence.Compute(parsed);
        }
    }
}
#endif
