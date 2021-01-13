namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsTrue_IfModelBinderAttributeIsUsedToRenameParameter
    {
        public string Model { get; set; }

        public void ActionMethod([ModelBinder(Name = "model")] IsProblematicParameter_ReturnsTrue_IfModelBinderAttributeIsUsedToRenameParameter different) { }
    }
}
