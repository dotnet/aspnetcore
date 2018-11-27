// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class DirectiveChunkGenerator : ParentChunkGenerator
    {
        private static readonly Type Type = typeof(DirectiveChunkGenerator);
        private List<RazorDiagnostic> _diagnostics;

        public DirectiveChunkGenerator(DirectiveDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public DirectiveDescriptor Descriptor { get; }

        public List<RazorDiagnostic> Diagnostics
        {
            get
            {
                if (_diagnostics == null)
                {
                    _diagnostics = new List<RazorDiagnostic>();
                }

                return _diagnostics;
            }
        }

        public override void Accept(ParserVisitor visitor, Block block)
        {
            visitor.VisitDirectiveBlock(this, block);
        }

        public override bool Equals(object obj)
        {
            var other = obj as DirectiveChunkGenerator;
            return base.Equals(other) &&
                Enumerable.SequenceEqual(Diagnostics, other.Diagnostics) &&
                DirectiveDescriptorComparer.Default.Equals(Descriptor, other.Descriptor);
        }

        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner.Start();
            combiner.Add(base.GetHashCode());
            combiner.Add(Type);

            return combiner.CombinedHash;
        }

        public override string ToString()
        {
            // This is used primarily at test time to show an identifiable representation of the chunk generator.

            var builder = new StringBuilder("Directive {");
            builder.Append(Descriptor.Directive);
            builder.Append("}");

            if (Diagnostics.Count > 0)
            {
                builder.Append(" [");
                var ids = string.Join(", ", Diagnostics.Select(diagnostic => diagnostic.Id));
                builder.Append(ids);
                builder.Append("]");
            }

            return builder.ToString();
        }
    }
}
