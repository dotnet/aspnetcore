namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsFalse_IfModelBinderAttributeIsUsedToRenameProperty
    {
        [FromQuery(Name = "different")]
        public string Model { get; set; }

        public void ActionMethod([Bind(Prefix = nameof(Model))] IsProblematicParameter_ReturnsFalse_IfModelBinderAttributeIsUsedToRenameProperty different) { }
    }
}
