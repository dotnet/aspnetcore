// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
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
    }
}
