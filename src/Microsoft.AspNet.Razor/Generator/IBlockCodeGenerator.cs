// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public interface IBlockCodeGenerator
    {
        void GenerateStartBlockCode(Block target, CodeGeneratorContext context);
        void GenerateEndBlockCode(Block target, CodeGeneratorContext context);
    }
}
