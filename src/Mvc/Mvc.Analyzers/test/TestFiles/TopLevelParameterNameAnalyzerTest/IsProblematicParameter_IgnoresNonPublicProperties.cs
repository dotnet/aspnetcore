namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_IgnoresNonPublicProperties
    {
        protected string Model { get; set; }

        public void ActionMethod(IsProblematicParameter_IgnoresNonPublicProperties model) { }
    }
}
