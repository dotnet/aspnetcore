// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using Moq;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

public class ValidationAttributeAdapterOfTAttributeTest
{
    [Fact]
    public void GetErrorMessage_DontLocalizeWhenErrorMessageResourceTypeGiven()
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        var modelMetadata = metadataProvider.GetMetadataForProperty(typeof(string), "Length");

        var stringLocalizer = new Mock<IStringLocalizer>(MockBehavior.Loose);

        var attribute = new TestValidationAttribute();
        var adapter = new TestValidationAttributeAdapter(attribute, stringLocalizer.Object);

        var actionContext = new ActionContext();
        var validationContext = new ModelValidationContext(
            actionContext,
            modelMetadata,
            metadataProvider,
            container: null,
            model: null);

        // Act
        adapter.GetErrorMessage(validationContext);

        // Assert
        Assert.True(attribute.Formatted);
    }

    public class TestValidationAttribute : ValidationAttribute
    {
        public bool Formatted = false;

        public override string FormatErrorMessage(string name)
        {
            Formatted = true;
            return base.FormatErrorMessage(name);
        }
    }

    public class TestValidationAttributeAdapter : ValidationAttributeAdapter<TestValidationAttribute>
    {
        public TestValidationAttributeAdapter(TestValidationAttribute attribute, IStringLocalizer stringLocalizer)
            : base(attribute, stringLocalizer)
        { }

        public override void AddValidation(ClientModelValidationContext context)
        {
            throw new NotImplementedException();
        }

        public string GetErrorMessage(ModelValidationContextBase validationContext)
        {
            var displayName = validationContext.ModelMetadata.GetDisplayName();
            return GetErrorMessage(validationContext.ModelMetadata, displayName);
        }
    }
}
