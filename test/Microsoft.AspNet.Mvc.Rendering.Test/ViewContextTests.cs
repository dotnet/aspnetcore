using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class ViewContextTests
    {
        [Fact]
        public void SettingViewData_AlsoUpdatesViewBag()
        {
            // Arrange (eventually passing null to these consturctors will throw)
            var context = new ViewContext(serviceProvider: null, httpContext: null, viewEngineContext: null);
            var originalViewData = context.ViewData = new ViewDataDictionary(metadataProvider: null);
            var replacementViewData = new ViewDataDictionary(metadataProvider: null);

            // Act
            context.ViewBag.Hello = "goodbye";
            context.ViewData = replacementViewData;
            context.ViewBag.Another = "property";

            // Assert
            Assert.NotSame(originalViewData, context.ViewData);
            Assert.Same(replacementViewData, context.ViewData);
            Assert.Null(context.ViewBag.Hello);
            Assert.Equal("property", context.ViewBag.Another);
            Assert.Equal("property", context.ViewData["Another"]);
        }
    }
}
