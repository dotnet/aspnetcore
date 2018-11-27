namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_IgnoresStaticProperties
    {
        public static string Model { get; set; }

        public void ActionMethod(IsProblematicParameter_IgnoresStaticProperties model) { }
    }
}
