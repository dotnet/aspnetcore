// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public class ChecksumIRNode : RazorIRNode
    {
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

        public override TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor)
        {
            return visitor.VisitChecksum(this);
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

            var charBuffer = new char[sourceDocument.Length];
            sourceDocument.CopyTo(0, charBuffer, 0, sourceDocument.Length);

            var encoder = sourceDocument.Encoding.GetEncoder();
            var byteCount = encoder.GetByteCount(charBuffer, 0, charBuffer.Length, flush: true);
            var checksumBytes = new byte[byteCount];
            encoder.GetBytes(charBuffer, 0, charBuffer.Length, checksumBytes, 0, flush: true);

            using (var hashAlgorithm = SHA1.Create())
            {
                var hashedBytes = hashAlgorithm.ComputeHash(checksumBytes);
                var fileHashBuilder = new StringBuilder(hashedBytes.Length * 2);
                foreach (var value in hashedBytes)
                {
                    fileHashBuilder.Append(value.ToString("x2"));
                }

                node.Bytes = fileHashBuilder.ToString();
            }

            return node;
        }
    }
}
