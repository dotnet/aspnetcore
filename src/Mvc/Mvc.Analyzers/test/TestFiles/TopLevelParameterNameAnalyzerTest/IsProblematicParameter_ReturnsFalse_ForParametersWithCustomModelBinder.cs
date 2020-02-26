// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsFalse_ForParametersWithCustomModelBinder
    {
        public string Model { get; set; }

        public void ActionMethod(
            [ModelBinder(typeof(SimpleTypeModelBinder))] IsProblematicParameter_ReturnsFalse_ForParametersWithCustomModelBinder model) { }
    }
}
