using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class ControllerTests
    {
        [Fact]
        public void SettingViewData_AlsoUpdatesViewBag()
        {
            // Arrange
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var controller = new Controller();
            var originalViewData = controller.ViewData = new ViewDataDictionary<object>(metadataProvider);
            var replacementViewData = new ViewDataDictionary<object>(metadataProvider);

            // Act
            controller.ViewBag.Hello = "goodbye";
            controller.ViewData = replacementViewData;
            controller.ViewBag.Another = "property";

            // Assert
            Assert.NotSame(originalViewData, controller.ViewData);
            Assert.Same(replacementViewData, controller.ViewData);
            Assert.Null(controller.ViewBag.Hello);
            Assert.Equal("property", controller.ViewBag.Another);
            Assert.Equal("property", controller.ViewData["Another"]);
        }

        [Fact]
        public void Redirect_Temporary_SetsSameUrl()
        {
            // Arrange
            var controller = new Controller();

            // Act
            var result = controller.Redirect("sample\\url");

            // Assert
            Assert.False(result.Permanent);
            Assert.Equal("sample\\url", result.Url);
        }

        [Fact]
        public void Redirect_Permanent_SetsSameUrl()
        {
            // Arrange
            var controller = new Controller();

            // Act
            var result = controller.RedirectPermanent("sample\\url");

            // Assert
            Assert.True(result.Permanent);
            Assert.Equal("sample\\url", result.Url);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Redirect_NullOrEmptyUrl_Throws(string url)
        {
            // Arrange
            var controller = new Controller();

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => controller.Redirect(url: url), "url", "The value cannot be null or empty");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RedirectPermanent_NullOrEmptyUrl_Throws(string url)
        {
            // Arrange
            var controller = new Controller();

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => controller.RedirectPermanent(url: url), "url", "The value cannot be null or empty");
        }
    }
}
