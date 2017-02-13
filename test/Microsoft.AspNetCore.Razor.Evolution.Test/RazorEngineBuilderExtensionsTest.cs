// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.CodeGeneration;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class RazorEngineBuilderExtensionsTest
    {
        [Fact]
        public void AddDirective_ExistingFeature_UsesFeature()
        {
            // Arrange
            var expected = new DefaultRazorDirectiveFeature();
            var engine = RazorEngine.CreateEmpty(b =>
            {
                b.Features.Add(expected);

                // Act
                b.AddDirective(DirectiveDescriptorBuilder.Create("test_directive").Build());
            });

            // Assert
            var actual = Assert.Single(engine.Features.OfType<IRazorDirectiveFeature>());
            Assert.Same(expected, actual);

            var directive = Assert.Single(actual.Directives);
            Assert.Equal("test_directive", directive.Name);
        }

        [Fact]
        public void AddDirective_NoFeature_CreatesFeature()
        {
            // Arrange
            var engine = RazorEngine.CreateEmpty(b =>
            {
                // Act
                b.AddDirective(DirectiveDescriptorBuilder.Create("test_directive").Build());
            });

            // Assert
            var actual = Assert.Single(engine.Features.OfType<IRazorDirectiveFeature>());
            Assert.IsType<DefaultRazorDirectiveFeature>(actual);

            var directive = Assert.Single(actual.Directives);
            Assert.Equal("test_directive", directive.Name);
        }

        [Fact]
        public void AddTargetExtensions_ExistingFeature_UsesFeature()
        {
            // Arrange
            var extension = new MyTargetExtension();

            var expected = new DefaultRazorTargetExtensionFeature();
            var engine = RazorEngine.CreateEmpty(b =>
            {
                b.Features.Add(expected);

                // Act
                b.AddTargetExtension(extension);
            });

            // Assert
            var actual = Assert.Single(engine.Features.OfType<IRazorTargetExtensionFeature>());
            Assert.Same(expected, actual);

            Assert.Same(extension, Assert.Single(actual.TargetExtensions));
        }

        [Fact]
        public void AddTargetExtensions_NoFeature_CreatesFeature()
        {
            // Arrange
            var extension = new MyTargetExtension();

            var engine = RazorEngine.CreateEmpty(b =>
            {
                // Act
                b.AddTargetExtension(extension);
            });

            // Assert
            var actual = Assert.Single(engine.Features.OfType<IRazorTargetExtensionFeature>());
            Assert.IsType<DefaultRazorTargetExtensionFeature>(actual);

            Assert.Same(extension, Assert.Single(actual.TargetExtensions));
        }

        private class MyTargetExtension : IRuntimeTargetExtension
        {
        }
    }
}
