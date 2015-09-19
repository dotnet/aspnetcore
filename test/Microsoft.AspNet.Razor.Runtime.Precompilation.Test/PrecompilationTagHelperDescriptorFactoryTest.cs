// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Test.Internal;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    public class PrecompilationTagHelperDescriptorFactoryTest : TagHelperDescriptorFactoryTest
    {
        public override ITypeInfo GetTypeInfo(Type tagHelperType)
        {
            var paths = new[]
            {
                $"TagHelperDescriptorFactoryTagHelpers",
                $"CommonTagHelpers",
            };

            var compilation = CompilationUtility.GetCompilation(paths);
            var typeResolver = new PrecompilationTagHelperTypeResolver(compilation);

            return Assert.Single(typeResolver.GetExportedTypes(CompilationUtility.GeneratedAssemblyName),
                generatedType => string.Equals(generatedType.FullName, tagHelperType.FullName, StringComparison.Ordinal));
        }

        [Theory]
        [MemberData(nameof(TagHelperWithPrefixData))]
        public void CreateDescriptors_WithPrefixes_ReturnsExpectedAttributeDescriptors(
            Type tagHelperType,
            IEnumerable<TagHelperAttributeDescriptor> expectedAttributeDescriptors,
            string[] expectedErrorMessages)
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                GetTypeInfo(tagHelperType),
                designTime: false,
                errorSink: errorSink);

            // Assert
            var errors = errorSink.Errors.ToArray();
            Assert.Equal(expectedErrorMessages.Length, errors.Length);

            for (var i = 0; i < errors.Length; i++)
            {
                Assert.Equal(0, errors[i].Length);
                Assert.Equal(SourceLocation.Zero, errors[i].Location);
                Assert.Equal(expectedErrorMessages[i], errors[i].Message, StringComparer.Ordinal);
            }

            var descriptor = Assert.Single(descriptors);
#if DNXCORE50
            // In CoreCLR type forwarding of System.Runtime types causes issues with comparing FullNames for generic types.
            // We'll work around this by sanitizing the type names so that the assembly qualification is removed.
            foreach (var attributeDescriptor in expectedAttributeDescriptors)
            {
                attributeDescriptor.TypeName = RuntimeTypeInfo.SanitizeFullName(attributeDescriptor.TypeName);
            }

            foreach (var attributeDescriptor in descriptor.Attributes)
            {
                attributeDescriptor.TypeName = RuntimeTypeInfo.SanitizeFullName(attributeDescriptor.TypeName);
            }
#endif

            Assert.Equal(
                expectedAttributeDescriptors,
                descriptor.Attributes,
                TagHelperAttributeDescriptorComparer.Default);
        }
    }
}
