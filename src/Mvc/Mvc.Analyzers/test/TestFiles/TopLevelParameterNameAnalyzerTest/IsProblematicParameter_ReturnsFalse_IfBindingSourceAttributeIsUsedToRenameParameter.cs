namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsFalse_IfBindingSourceAttributeIsUsedToRenameParameter
    {
        public string Model { get; set; }

        public void ActionMethod([FromRoute(Name = "id")] IsProblematicParameter_ReturnsFalse_IfBindingSourceAttributeIsUsedToRenameParameter model) { }
    }
}
