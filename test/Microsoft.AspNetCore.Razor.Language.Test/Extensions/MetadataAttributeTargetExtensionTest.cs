// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class MetadataAttributeTargetExtensionTest
    {
        [Fact]
        public void WriteRazorCompiledItemAttribute_RendersCorrectly()
        {
            // Arrange
            var extension = new MetadataAttributeTargetExtension()
            {
                CompiledItemAttributeName = "global::TestItem",
            };
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new RazorCompiledItemAttributeIntermediateNode()
            {
                TypeName = "Foo.Bar",
                Kind = "test",
                Identifier = "Foo/Bar",
            };

            // Act
            extension.WriteRazorCompiledItemAttribute(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"[assembly: global::TestItem(typeof(Foo.Bar), @""test"", @""Foo/Bar"")]
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteRazorSourceChecksumAttribute_RendersCorrectly()
        {
            // Arrange
            var extension = new MetadataAttributeTargetExtension()
            {
                SourceChecksumAttributeName = "global::TestChecksum",
            };
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new RazorSourceChecksumAttributeIntermediateNode()
            {
                ChecksumAlgorithm = "SHA1",
                Checksum = new byte[] { (byte)'t', (byte)'e', (byte)'s', (byte)'t', },
                Identifier = "Foo/Bar",
            };

            // Act
            extension.WriteRazorSourceChecksumAttribute(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"[global::TestChecksum(@""SHA1"", @""74657374"", @""Foo/Bar"")]
",
                csharp,
                ignoreLineEndingDifferences: true);
        }
    }
}
