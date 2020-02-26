// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsFalse_IfBindingSourceAttributeIsUsedToRenameParameter
    {
        public string Model { get; set; }

        public void ActionMethod([FromRoute(Name = "id")] IsProblematicParameter_ReturnsFalse_IfBindingSourceAttributeIsUsedToRenameParameter model) { }
    }
}
