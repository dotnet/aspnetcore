namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_IgnoresFields
    {
        public string model;

        public void ActionMethod(IsProblematicParameter_IgnoresFields model) { }
    }
}
