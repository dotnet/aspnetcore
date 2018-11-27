namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class GetNameTests
    {
        public void NoAttribute(int param) { }

        public void SingleAttribute([ModelBinder(Name = "testModelName")] int param) { }

        public void SingleAttributeWithoutName([ModelBinder] int param) { }

        public void MultipleAttributes([ModelBinder(Name = "name1")][Bind(Prefix = "name2")] int param) { }
    }
}
