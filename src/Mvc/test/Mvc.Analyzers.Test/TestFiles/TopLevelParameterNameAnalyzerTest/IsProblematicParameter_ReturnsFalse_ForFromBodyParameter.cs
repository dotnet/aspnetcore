namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsFalse_ForFromBodyParameter
    {
        public string Model { get; set; }

        public void ActionMethod([FromBody] IsProblematicParameter_ReturnsFalse_ForFromBodyParameter model) { }
    }
}
