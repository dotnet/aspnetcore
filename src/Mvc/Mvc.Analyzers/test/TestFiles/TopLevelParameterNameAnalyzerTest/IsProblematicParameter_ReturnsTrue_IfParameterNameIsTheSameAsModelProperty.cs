namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsTrue_IfParameterNameIsTheSameAsModelProperty
    {
        public string Model { get; set; }

        public void ActionMethod(IsProblematicParameter_ReturnsTrue_IfParameterNameIsTheSameAsModelProperty model) { }
    }
}
