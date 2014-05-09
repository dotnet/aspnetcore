// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public interface IBlockCodeGenerator
    {
        void GenerateStartBlockCode(Block target, CodeGeneratorContext context);
        void GenerateEndBlockCode(Block target, CodeGeneratorContext context);
    }
}
