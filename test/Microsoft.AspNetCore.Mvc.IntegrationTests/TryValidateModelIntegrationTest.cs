// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class TryValidateModelIntegrationTest
    {
        [Fact]
        public void ModelState_IsInvalid_ForInvalidData_OnDerivedModel()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();

            var modelState = operationContext.ActionContext.ModelState;
            var model = new SoftwareViewModel
            {
                Category = "Technology",
                CompanyName = "Microsoft",
                Contact = "4258393231",
                Country = "UK", // Here the validate country is USA only
                DatePurchased = new DateTime(2010, 10, 10),
                Price = 110,
                Version = "2"
            };
            var oldModel = model;

            // Act
            var result = TryValidateModel(model, "software", operationContext);

            // Assert
            Assert.False(result);
            Assert.Same(oldModel, model);
            Assert.False(modelState.IsValid);
            var modelStateErrors = GetModelStateErrors(modelState);
            Assert.Single(modelStateErrors);
            Assert.Equal("Product must be made in the USA if it is not named.", modelStateErrors["software"]);
        }

        [Fact]
        public void ModelState_IsValid_ForValidData_OnDerivedModel()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = operationContext.ActionContext.ModelState;
            var model = new SoftwareViewModel
            {
                Category = "Technology",
                CompanyName = "Microsoft",
                Contact = "4258393231",
                Country = "USA",
                DatePurchased = new DateTime(2010, 10, 10),
                Name = "MVC",
                Price = 110,
                Version = "2"
            };
            var oldModel = model;

            // Act
            var result = TryValidateModel(model, prefix: string.Empty, operationContext: operationContext);

            // Assert
            Assert.True(result);
            Assert.Same(oldModel, model);
            Assert.True(modelState.IsValid);
            var modelStateErrors = GetModelStateErrors(modelState);
            Assert.Empty(modelStateErrors);
        }

        [Fact]
        public void TryValidateModel_CollectionsModel_ReturnsErrorsForInvalidProperties()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var modelState = operationContext.ActionContext.ModelState;
            var model = new List<ProductViewModel>();
            model.Add(new ProductViewModel()
            {
                Price = 2,
                Contact = "acvrdzersaererererfdsfdsfdsfsdf",
                ProductDetails = new ProductDetails()
                {
                    Detail1 = "d1",
                    Detail2 = "d2",
                    Detail3 = "d3"
                }
            });
            model.Add(new ProductViewModel()
            {
                Price = 2,
                Contact = "acvrdzersaererererfdsfdsfdsfsdf",
                ProductDetails = new ProductDetails()
                {
                    Detail1 = "d1",
                    Detail2 = "d2",
                    Detail3 = "d3"
                }
            });
            var oldModel = model;

            // Act
            var result = TryValidateModel(model, prefix: string.Empty, operationContext: operationContext);

            // Assert
            Assert.False(result);
            Assert.False(modelState.IsValid);
            var modelStateErrors = GetModelStateErrors(modelState);
            Assert.Equal("CompanyName cannot be null or empty.", modelStateErrors["[0].CompanyName"]);
            Assert.Equal("The field Price must be between 20 and 100.", modelStateErrors["[0].Price"]);
            Assert.Equal(
                PlatformNormalizer.NormalizeContent("The Category field is required."),
                modelStateErrors["[0].Category"]);
            AssertErrorEquals(
                "The field Contact Us must be a string with a maximum length of 20." +
                "The field Contact Us must match the regular expression " +
                (TestPlatformHelper.IsMono ? "^[0-9]*$." : "'^[0-9]*$'."),
                modelStateErrors["[0].Contact"]);
            Assert.Equal("CompanyName cannot be null or empty.", modelStateErrors["[1].CompanyName"]);
            Assert.Equal("The field Price must be between 20 and 100.", modelStateErrors["[1].Price"]);
            Assert.Equal(
                PlatformNormalizer.NormalizeContent("The Category field is required."),
                modelStateErrors["[1].Category"]);
            AssertErrorEquals(
                "The field Contact Us must be a string with a maximum length of 20." +
                "The field Contact Us must match the regular expression " +
                (TestPlatformHelper.IsMono ? "^[0-9]*$." : "'^[0-9]*$'."),
                modelStateErrors["[1].Contact"]);
        }

        private bool TryValidateModel(
            object model,
            string prefix,
            OperationBindingContext operationContext)
        {
            var controller = new TestController();
            controller.ControllerContext = new ControllerContext(operationContext.ActionContext);
            controller.ObjectValidator = ModelBindingTestHelper.GetObjectValidator(operationContext.MetadataProvider);
            controller.MetadataProvider = operationContext.MetadataProvider;
            controller.ControllerContext.ValidatorProviders = new[] { operationContext.ValidatorProvider }.ToList();

            return controller.TryValidateModel(model, prefix);
        }

        private void AssertErrorEquals(string expected, string actual)
        {
            // OrderBy is used because the order of the results may very depending on the platform / client.
            Assert.Equal(
                expected.Split('.').OrderBy(item => item, StringComparer.Ordinal),
                actual.Split('.').OrderBy(item => item, StringComparer.Ordinal));
        }

        private Dictionary<string, string> GetModelStateErrors(ModelStateDictionary modelState)
        {
            var result = new Dictionary<string, string>();
            foreach (var item in modelState)
            {
                var errorMessage = string.Empty;
                foreach (var error in item.Value.Errors)
                {
                    if (error != null)
                    {
                        errorMessage = errorMessage + error.ErrorMessage;
                    }
                }
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    result.Add(item.Key, errorMessage);
                }
            }

            return result;
        }

        private class TestController : Controller
        {
        }
    }
}
