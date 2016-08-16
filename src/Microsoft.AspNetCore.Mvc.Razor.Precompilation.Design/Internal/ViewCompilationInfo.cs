// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.CodeGenerators;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation.Design.Internal
{
    public class ViewCompilationInfo
    {
        public ViewCompilationInfo(
            RelativeFileInfo relativeFileInfo,
            GeneratorResults generatorResults)
        {
            RelativeFileInfo = relativeFileInfo;
            GeneratorResults = generatorResults;
        }

        public RelativeFileInfo RelativeFileInfo { get; }

        public GeneratorResults GeneratorResults { get; }

        public string TypeName { get; set; }
    }
}
