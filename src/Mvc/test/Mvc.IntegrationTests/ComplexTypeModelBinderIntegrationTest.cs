// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{

    public class ComplexTypeModelBinderIntegrationTest : ComplexTypeIntegrationTestBase
    {
        [Fact]
        public async Task ComplexTypeModelBinderIntegrationTest_BindsComplexType_EmptyPrefix_ParameterNameEqualsPropertyName_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Id",
                ParameterType = typeof(Person)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?Id=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Person>(modelBindingResult.Model);

            Assert.Equal("10", model.Id.ToString());
        }

        private class Person
        {
            public int Id { get; set; }
        }
        
#pragma warning disable CS0618 // Type or member is obsolete
        protected override Type ExpectedModelBinderType => typeof(ComplexTypeModelBinder);

        protected override ModelBindingTestContext GetTestContext(
            Action<HttpRequest> updateRequest = null,
            Action<MvcOptions> updateOptions = null,
            IModelMetadataProvider metadataProvider = null)
        {
            return ModelBindingTestHelper.GetTestContext(
                updateRequest,
                updateOptions: options =>
                {
                    options.ModelBinderProviders.RemoveType<ComplexObjectModelBinderProvider>();
                    options.ModelBinderProviders.Add(new ComplexTypeModelBinderProvider());

                    updateOptions?.Invoke(options);
                },
                metadataProvider: metadataProvider);
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
