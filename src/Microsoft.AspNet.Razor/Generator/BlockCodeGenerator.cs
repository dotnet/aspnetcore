// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public abstract class BlockCodeGenerator : IBlockCodeGenerator
    {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "This class has no instance state")]
        public static readonly IBlockCodeGenerator Null = new NullBlockCodeGenerator();

        public virtual void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
        }

        public virtual void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
        }

        public override bool Equals(object obj)
        {
            return (obj as IBlockCodeGenerator) != null;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private class NullBlockCodeGenerator : IBlockCodeGenerator
        {
            public void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
            {
            }

            public void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
            {
            }

            public override string ToString()
            {
                return "None";
            }
        }
    }
}
