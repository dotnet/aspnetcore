namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_IgnoresMethods
    {
        public string Item() => null;

        public void ActionMethod(IsProblematicParameter_IgnoresMethods item) { }
    }
}
