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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    //public class ModelBindingContextTest
    //{
    //    [Fact]
    //    public void CopyConstructor()
    //    {
    //        // Arrange
    //        ModelBindingContext originalBindingContext = new ModelBindingContext
    //        {
    //            ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(object)),
    //            ModelName = "theName",
    //            ModelState = new ModelStateDictionary(),
    //            ValueProvider = new SimpleHttpValueProvider()
    //        };

    //        // Act
    //        ModelBindingContext newBindingContext = new ModelBindingContext(originalBindingContext);

    //        // Assert
    //        Assert.Null(newBindingContext.ModelMetadata);
    //        Assert.Equal("", newBindingContext.ModelName);
    //        Assert.Equal(originalBindingContext.ModelState, newBindingContext.ModelState);
    //        Assert.Equal(originalBindingContext.ValueProvider, newBindingContext.ValueProvider);
    //    }

    //    [Fact]
    //    public void ModelProperty()
    //    {
    //        // Arrange
    //        ModelBindingContext bindingContext = new ModelBindingContext
    //        {
    //            ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(int))
    //        };

    //        // Act & assert
    //        MemberHelper.TestPropertyValue(bindingContext, "Model", 42);
    //    }

    //    [Fact]
    //    public void ModelProperty_ThrowsIfModelMetadataDoesNotExist()
    //    {
    //        // Arrange
    //        ModelBindingContext bindingContext = new ModelBindingContext();

    //        // Act & assert
    //        Assert.Throws<InvalidOperationException>(
    //            () => bindingContext.Model = null,
    //            "The ModelMetadata property must be set before accessing this property.");
    //    }

    //    [Fact]
    //    public void ModelNameProperty()
    //    {
    //        // Arrange
    //        ModelBindingContext bindingContext = new ModelBindingContext();

    //        // Act & assert
    //        Assert.Reflection.StringProperty(bindingContext, bc => bc.ModelName, String.Empty);
    //    }

    //    [Fact]
    //    public void ModelStateProperty()
    //    {
    //        // Arrange
    //        ModelBindingContext bindingContext = new ModelBindingContext();
    //        ModelStateDictionary modelState = new ModelStateDictionary();

    //        // Act & assert
    //        MemberHelper.TestPropertyWithDefaultInstance(bindingContext, "ModelState", modelState);
    //    }

    //    [Fact]
    //    public void ModelAndModelTypeAreFedFromModelMetadata()
    //    {
    //        // Act
    //        ModelBindingContext bindingContext = new ModelBindingContext
    //        {
    //            ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(() => 42, typeof(int))
    //        };

    //        // Assert
    //        Assert.Equal(42, bindingContext.Model);
    //        Assert.Equal(typeof(int), bindingContext.ModelType);
    //    }

    //    [Fact]
    //    public void ValidationNodeProperty()
    //    {
    //        // Act
    //        ModelBindingContext bindingContext = new ModelBindingContext
    //        {
    //            ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(() => 42, typeof(int))
    //        };

    //        // Act & assert
    //        MemberHelper.TestPropertyWithDefaultInstance(bindingContext, "ValidationNode", new ModelValidationNode(bindingContext.ModelMetadata, "someName"));
    //    }

    //    [Fact]
    //    public void ValidationNodeProperty_DefaultValues()
    //    {
    //        // Act
    //        ModelBindingContext bindingContext = new ModelBindingContext
    //        {
    //            ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(() => 42, typeof(int)),
    //            ModelName = "theInt"
    //        };

    //        // Act
    //        ModelValidationNode validationNode = bindingContext.ValidationNode;

    //        // Assert
    //        Assert.NotNull(validationNode);
    //        Assert.Equal(bindingContext.ModelMetadata, validationNode.ModelMetadata);
    //        Assert.Equal(bindingContext.ModelName, validationNode.ModelStateKey);
    //    }
    //}
}
