// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class DirectiveChunkGenerator : ParentChunkGenerator
    {
        private static readonly Type Type = typeof(DirectiveChunkGenerator);

        public DirectiveChunkGenerator(DirectiveDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public DirectiveDescriptor Descriptor { get; }

        public override void Accept(ParserVisitor visitor, Block block)
        {
            visitor.VisitDirectiveBlock(this, block);
        }

        public override bool Equals(object obj)
        {
            var other = obj as DirectiveChunkGenerator;
            return base.Equals(other) &&
                DirectiveDescriptorComparer.Default.Equals(Descriptor, other.Descriptor);
        }

        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner.Start();
            combiner.Add(base.GetHashCode());
            combiner.Add(Type);

            return combiner.CombinedHash;
        }
    }
}
