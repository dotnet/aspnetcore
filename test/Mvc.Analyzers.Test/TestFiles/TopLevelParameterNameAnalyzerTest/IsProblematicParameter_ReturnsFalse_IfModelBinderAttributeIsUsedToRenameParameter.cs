namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsFalse_IfModelBinderAttributeIsUsedToRenameParameter
    {
        public string Model { get; set; }

        public void ActionMethod([FromRoute(Name = "id")] IsProblematicParameter_ReturnsFalse_IfModelBinderAttributeIsUsedToRenameParameter model) { }
    }
}
