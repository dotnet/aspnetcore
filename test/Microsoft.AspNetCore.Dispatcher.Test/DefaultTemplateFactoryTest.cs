// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DefaultTemplateFactoryTest
    {
        [Fact]
        public void GetTemplateFromKey_UsesMatchingComponent_SelectsTemplate()
        {
            // Arrange
            var expected = Mock.Of<Template>();
            var factory = new DefaultTemplateFactory(new ITemplateFactoryComponent[]
            {
                Mock.Of<TemplateFactory<string>>(f => f.GetTemplate("foo") == expected),
            });

            // Act
            var template = factory.GetTemplateFromKey("foo");

            // Assert
            Assert.Same(expected, template);
        }

        [Fact]
        public void GetTemplateFromKey_UsesMatchingComponent_IgnoresOtherComponents()
        {
            // Arrange
            var expected = Mock.Of<Template>();
            var factory = new DefaultTemplateFactory(new ITemplateFactoryComponent[]
            {
                Mock.Of<TemplateFactory<int>>(f => f.GetTemplate(17) == Mock.Of<Template>()),
                Mock.Of<TemplateFactory<string>>(f => f.GetTemplate("foo") == expected),
            });

            // Act
            var template = factory.GetTemplateFromKey("foo");

            // Assert
            Assert.Same(expected, template);
        }

        [Fact]
        public void GetTemplateFromKey_UsesMatchingComponent_ReturnsFirstMatch()
        {
            // Arrange
            var expected = Mock.Of<Template>();
            var factory = new DefaultTemplateFactory(new ITemplateFactoryComponent[]
            {
                Mock.Of<TemplateFactory<string>>(), // Will return null
                Mock.Of<TemplateFactory<string>>(f => f.GetTemplate("foo") == expected),
            });

            // Act
            var template = factory.GetTemplateFromKey("foo");

            // Assert
            Assert.Same(expected, template);
        }
    }
}
