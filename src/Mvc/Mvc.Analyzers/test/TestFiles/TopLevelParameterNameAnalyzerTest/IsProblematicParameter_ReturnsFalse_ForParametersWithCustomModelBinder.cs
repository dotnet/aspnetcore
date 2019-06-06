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
