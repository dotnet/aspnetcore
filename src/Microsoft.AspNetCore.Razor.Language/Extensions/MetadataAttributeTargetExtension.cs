// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal class MetadataAttributeTargetExtension : IMetadataAttributeTargetExtension
    {
        public string CompiledItemAttributeName { get; set; } = "global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute";

        public string SourceChecksumAttributeName { get; set; } = "global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute";

        public void WriteRazorCompiledItemAttribute(CodeRenderingContext context, RazorCompiledItemAttributeIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // [assembly: global::...RazorCompiledItem(typeof({node.TypeName}), @"{node.Kind}", @"{node.Identifier}")]
            context.CodeWriter.Write("[assembly: ");
            context.CodeWriter.Write(CompiledItemAttributeName);
            context.CodeWriter.Write("(typeof(");
            context.CodeWriter.Write(node.TypeName);
            context.CodeWriter.Write("), @\"");
            context.CodeWriter.Write(node.Kind);
            context.CodeWriter.Write("\", @\"");
            context.CodeWriter.Write(node.Identifier);
            context.CodeWriter.WriteLine("\")]");
        }

        public void WriteRazorSourceChecksumAttribute(CodeRenderingContext context, RazorSourceChecksumAttributeIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // [global::...RazorSourceChecksum(@"{node.ChecksumAlgorithm}", @"{node.Checksum}", @"{node.Identifier}")]
            context.CodeWriter.Write("[");
            context.CodeWriter.Write(SourceChecksumAttributeName);
            context.CodeWriter.Write("(@\"");
            context.CodeWriter.Write(node.ChecksumAlgorithm);
            context.CodeWriter.Write("\", @\"");
            context.CodeWriter.Write(Checksum.BytesToString(node.Checksum));
            context.CodeWriter.Write("\", @\"");
            context.CodeWriter.Write(node.Identifier);
            context.CodeWriter.WriteLine("\")]");
        }
    }
}
