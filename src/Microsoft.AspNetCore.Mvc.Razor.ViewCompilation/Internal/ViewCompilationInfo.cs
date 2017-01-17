// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.CodeGenerators;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal
{
    public class ViewCompilationInfo
    {
        public ViewCompilationInfo(
            ViewFileInfo viewFileInfo,
            GeneratorResults generatorResults)
        {
            ViewFileInfo = viewFileInfo;
            GeneratorResults = generatorResults;
        }

        public ViewFileInfo ViewFileInfo { get; }

        public GeneratorResults GeneratorResults { get; }

        public string TypeName { get; set; }
    }
}
