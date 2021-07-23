// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsTrue_IfPropertyWithModelBindingAttributeHasSameNameAsParameter
    {
        [ModelBinder(typeof(ComplexObjectModelBinder), Name = "model")]
        public string Different { get; set; }

        public void ActionMethod(
            IsProblematicParameter_ReturnsTrue_IfPropertyWithModelBindingAttributeHasSameNameAsParameter model) { }
    }
}
