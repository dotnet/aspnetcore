namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsFalse_IfBindingSourceAttributeIsUsedToRenameProperty
    {
        [FromQuery(Name = "different")]
        public string Model { get; set; }

        public void ActionMethod(IsProblematicParameter_ReturnsFalse_IfBindingSourceAttributeIsUsedToRenameProperty model) { }
    }
}
