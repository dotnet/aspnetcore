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
