namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class SpecifiesModelTypeTests
    {
        public void SpecifiesModelType_ReturnsFalse_IfModelBinderDoesNotSpecifyType([ModelBinder(Name = "Name")] object model) { }

        public void SpecifiesModelType_ReturnsTrue_IfModelBinderSpecifiesTypeFromConstructor([ModelBinder(typeof(object))] object model) { }

        public void SpecifiesModelType_ReturnsTrue_IfModelBinderSpecifiesTypeFromProperty([ModelBinder(BinderType = typeof(object))] object model) { }
    }
}
