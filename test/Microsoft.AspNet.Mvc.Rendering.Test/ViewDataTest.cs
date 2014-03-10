using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ViewDataDictionaryTest
    {
        [Fact]
        public void ConstructorThrowsIfParameterIsNull()
        {
            // Act & Assert
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new ViewData(source: null));
            Assert.Equal("source", ex.ParamName);
        }
   }
}