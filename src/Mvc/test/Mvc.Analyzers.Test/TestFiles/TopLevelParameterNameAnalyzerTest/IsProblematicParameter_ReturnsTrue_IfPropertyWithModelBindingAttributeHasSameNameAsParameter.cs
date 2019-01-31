namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsTrue_IfPropertyWithModelBindingAttributeHasSameNameAsParameter
    {
        [ModelBinder(typeof(object), Name = "model")]
        public string Different { get; set; }

        public void ActionMethod(IsProblematicParameter_ReturnsTrue_IfPropertyWithModelBindingAttributeHasSameNameAsParameter model) { }
    }
}
