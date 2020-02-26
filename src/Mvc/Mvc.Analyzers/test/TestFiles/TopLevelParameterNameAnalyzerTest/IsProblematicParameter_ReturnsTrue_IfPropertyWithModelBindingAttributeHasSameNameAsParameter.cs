// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsTrue_IfPropertyWithModelBindingAttributeHasSameNameAsParameter
    {
        [ModelBinder(typeof(ComplexTypeModelBinder), Name = "model")]
        public string Different { get; set; }

        public void ActionMethod(
            IsProblematicParameter_ReturnsTrue_IfPropertyWithModelBindingAttributeHasSameNameAsParameter model) { }
    }
}
