namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsTrue_IfParameterNameWithBinderAttributeIsTheSameNameAsModelProperty
    {
        public string Model { get; set; }

        public void ActionMethod([Bind(Prefix = "model")] IsProblematicParameter_ReturnsTrue_IfParameterNameWithBinderAttributeIsTheSameNameAsModelProperty different) { }
    }
}
