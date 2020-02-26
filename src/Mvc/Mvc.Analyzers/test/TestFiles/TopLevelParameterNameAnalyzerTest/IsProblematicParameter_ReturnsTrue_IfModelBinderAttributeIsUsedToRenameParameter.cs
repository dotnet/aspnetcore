// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsTrue_IfModelBinderAttributeIsUsedToRenameParameter
    {
        public string Model { get; set; }

        public void ActionMethod([ModelBinder(Name = "model")] IsProblematicParameter_ReturnsTrue_IfModelBinderAttributeIsUsedToRenameParameter different) { }
    }
}
