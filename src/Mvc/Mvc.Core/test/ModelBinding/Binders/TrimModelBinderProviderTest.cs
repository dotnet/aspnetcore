using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Xunit;
using static Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelMetadataBindingDetailsProviderTest;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class TrimModelBinderProviderTest
    {
        [Fact]
        public void Create_ForCanTrim_ReturnsBinder()
        {
            // Arrange
            var provider = new TrimModelBinderProvider();
            var context = new TestModelBinderProviderContext(typeof(string), ModelMetadata(true));

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<TrimModelBinder>(result);
        }

        [Fact]
        public void Create_ForCannotTrim_ReturnsNull()
        {
            // Arrange
            var provider = new TrimModelBinderProvider();
            var context = new TestModelBinderProviderContext(typeof(string), ModelMetadata(false));

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        public static ModelMetadata ModelMetadata(bool canTrim, TrimType trimType = TrimType.Trim, bool convertEmptyStringToNull = true)
        {
            var trimAttribute = new object[] { new TrimAttribute() };
            var providerMetadata = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForProperty(ParameterInfos.StringPropertyInfo, typeof(string), typeof(ParameterInfos));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], trimAttribute, null))
            {
                BindingMetadata = new BindingMetadata()
                {
                    CanTrim = canTrim,
                    TrimType = trimType
                },
                DisplayMetadata = new DisplayMetadata { ConvertEmptyStringToNull = convertEmptyStringToNull }
            };

            return new DefaultModelMetadata(providerMetadata, detailsProvider, cache);
        }
    }
}
