using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ViewDataOfTTest
    {
        [Fact]
        public void SettingModelThrowsIfTheModelIsNull()
        {
            // Arrange
            var viewDataOfT = new ViewData<int>();
            ViewData viewData = viewDataOfT;

            // Act and Assert
            Exception ex = Assert.Throws<InvalidOperationException>(() => viewData.Model = null);
            Assert.Equal("The model item passed is null, but this ViewData instance requires a non-null model item of type 'System.Int32'.", ex.Message);
        }

        [Fact]
        public void SettingModelThrowsIfTheModelIsIncompatible()
        {
            // Arrange
            var viewDataOfT = new ViewData<string>();
            ViewData viewData = viewDataOfT;

            // Act and Assert
            Exception ex = Assert.Throws<InvalidOperationException>(() => viewData.Model = DateTime.UtcNow);
            Assert.Equal("The model item passed into the ViewData is of type 'System.DateTime', but this ViewData instance requires a model item of type 'System.String'.", ex.Message);
        }

        [Fact]
        public void SettingModelWorksForCompatibleTypes()
        {
            // Arrange
            string value = "some value";
            var viewDataOfT = new ViewData<object>();
            ViewData viewData = viewDataOfT;

            // Act
            viewData.Model = value;

            // Assert
            Assert.Same(value, viewDataOfT.Model);
        }
    }
}
