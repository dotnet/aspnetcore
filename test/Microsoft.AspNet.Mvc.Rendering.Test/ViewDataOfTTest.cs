using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class ViewDataOfTTest
    {
        [Fact]
        public void SettingModelThrowsIfTheModelIsNull()
        {
            // Arrange
            var viewDataOfT = new ViewDataDictionary<int>(new DataAnnotationsModelMetadataProvider());
            ViewDataDictionary viewData = viewDataOfT;

            // Act and Assert
            Exception ex = Assert.Throws<InvalidOperationException>(() => viewData.Model = null);
            Assert.Equal("The model item passed is null, but this ViewDataDictionary instance requires a non-null model item of type 'System.Int32'.", ex.Message);
        }

        [Fact]
        public void SettingModelThrowsIfTheModelIsIncompatible()
        {
            // Arrange
            var viewDataOfT = new ViewDataDictionary<string>(new DataAnnotationsModelMetadataProvider());
            ViewDataDictionary viewData = viewDataOfT;

            // Act and Assert
            Exception ex = Assert.Throws<InvalidOperationException>(() => viewData.Model = DateTime.UtcNow);
            Assert.Equal("The model item passed into the ViewDataDictionary is of type 'System.DateTime', but this ViewDataDictionary instance requires a model item of type 'System.String'.", ex.Message);
        }

        [Fact]
        public void SettingModelWorksForCompatibleTypes()
        {
            // Arrange
            string value = "some value";
            var viewDataOfT = new ViewDataDictionary<object>(new DataAnnotationsModelMetadataProvider());
            ViewDataDictionary viewData = viewDataOfT;

            // Act
            viewData.Model = value;

            // Assert
            Assert.Same(value, viewDataOfT.Model);
        }
    }
}
