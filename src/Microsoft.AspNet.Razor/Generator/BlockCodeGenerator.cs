// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public abstract class BlockCodeGenerator : IBlockCodeGenerator
    {
        private static readonly int TypeHashCode = typeof(BlockCodeGenerator).GetHashCode();

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
            return obj != null &&
                GetType() == obj.GetType();
        }

        public override int GetHashCode()
        {
            return TypeHashCode;
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
