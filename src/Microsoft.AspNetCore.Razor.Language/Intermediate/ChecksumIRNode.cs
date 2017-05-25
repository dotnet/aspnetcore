// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class ChecksumIRNode : RazorIRNode
    {
        public override ItemCollection Annotations => ReadOnlyItemCollection.Empty;

        public override IList<RazorIRNode> Children => EmptyArray;

        public override RazorIRNode Parent { get; set; }

        public override SourceSpan? Source { get; set; }

        public string Bytes { get; set; }

        public string FileName { get; set; }

        public string Guid { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            visitor.VisitChecksum(this);
        }

        public static ChecksumIRNode Create(RazorSourceDocument sourceDocument)
        {
            // See http://msdn.microsoft.com/en-us/library/system.codedom.codechecksumpragma.checksumalgorithmid.aspx
            const string Sha1AlgorithmId = "{ff1816ec-aa5e-4d10-87f7-6f4963833460}";

            var node = new ChecksumIRNode()
            {
                FileName = sourceDocument.FileName,
                Guid = Sha1AlgorithmId
            };

            var checksum = sourceDocument.GetChecksum();
            var fileHashBuilder = new StringBuilder(checksum.Length * 2);
            foreach (var value in checksum)
            {
                fileHashBuilder.Append(value.ToString("x2"));
            }

            node.Bytes = fileHashBuilder.ToString();

            return node;
        }
    }
}
