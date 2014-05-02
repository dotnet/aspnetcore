// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

#if NET45
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class DictionaryModelBinderTest
    {
        [Fact]
        public async Task BindModel()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            ModelBindingContext bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(null, typeof(IDictionary<int, string>)),
                ModelName = "someName",
                ValueProvider = new SimpleHttpValueProvider
                {
                    { "someName[0]", new KeyValuePair<int, string>(42, "forty-two") },
                    { "someName[1]", new KeyValuePair<int, string>(84, "eighty-four") }
                },
                ModelBinder = CreateKvpBinder(),
                MetadataProvider = metadataProvider
            };            
            var binder = new DictionaryModelBinder<int, string>();
            
            // Act
            bool retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(retVal);

            var dictionary = Assert.IsAssignableFrom<IDictionary<int, string>>(bindingContext.Model);
            Assert.NotNull(dictionary);
            Assert.Equal(2, dictionary.Count);
            Assert.Equal("forty-two", dictionary[42]);
            Assert.Equal("eighty-four", dictionary[84]);
        }

        private static IModelBinder CreateKvpBinder()
        {
            Mock<IModelBinder> mockKvpBinder = new Mock<IModelBinder>();
            mockKvpBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(async (ModelBindingContext mbc) =>
                {
                    var value = await mbc.ValueProvider.GetValueAsync(mbc.ModelName);
                    if (value != null)
                    {
                        mbc.Model = value.ConvertTo(mbc.ModelType);
                        return true;
                    }
                    return false;
                });
            return mockKvpBinder.Object;
        }
    }
}
#endif
