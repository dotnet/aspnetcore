using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.DataAnnotations.Test
{
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
            Assert.True(attribute.Formated);
        }

        public class TestValidationAttribute : ValidationAttribute
        {
            public bool Formated = false;

            public override string FormatErrorMessage(string name)
            {
                Formated = true;
                return base.FormatErrorMessage(name);
            }
        }

        public class TestValidationAttributeAdapter : ValidationAttributeAdapter<TestValidationAttribute>
        {
            public TestValidationAttributeAdapter(TestValidationAttribute attribute, IStringLocalizer stringLocalizer)
                : base(attribute, stringLocalizer)
            { }

            public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(ClientModelValidationContext context)
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
}
