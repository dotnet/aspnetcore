using System.Globalization;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ValueProviderResultTest
    {
        [Fact]
        public void ConvertTo_ReturnsNullForReferenceTypes_WhenValueIsNull()
        {
            var valueProviderResult = new ValueProviderResult(null, null, CultureInfo.InvariantCulture);

            var convertedValue = valueProviderResult.ConvertTo(typeof(string));

            Assert.Equal(null, convertedValue);
        }

        [Fact]
        public void ConvertTo_ReturnsDefaultForValueTypes_WhenValueIsNull()
        {
            var valueProviderResult = new ValueProviderResult(null, null, CultureInfo.InvariantCulture);

            var convertedValue = valueProviderResult.ConvertTo(typeof(int));

            Assert.Equal(0, convertedValue);
        }
    }
}