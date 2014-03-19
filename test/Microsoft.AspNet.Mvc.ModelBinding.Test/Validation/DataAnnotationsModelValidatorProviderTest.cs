using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
#if NET45
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DataAnnotationsModelValidatorProviderTest
    {
        private readonly DataAnnotationsModelMetadataProvider _metadataProvider = new DataAnnotationsModelMetadataProvider();

        [Fact]
        public void UnknownValidationAttributeGetsDefaultAdapter()
        {
            // Arrange
            var provider = new DataAnnotationsModelValidatorProvider();
            var metadata = _metadataProvider.GetMetadataForType(() => null, typeof(DummyClassWithDummyValidationAttribute));

            // Act
            IEnumerable<IModelValidator> validators = provider.GetValidators(metadata);

            // Assert
            var validator = validators.Single();
            Assert.IsType<DataAnnotationsModelValidator>(validator);
        }

        private class DummyValidationAttribute : ValidationAttribute
        {
        }

        [DummyValidation]
        private class DummyClassWithDummyValidationAttribute
        {
        }

        // Default IValidatableObject adapter factory

#if NET45
        [Fact]
        public void IValidatableObjectGetsAValidator()
        {
            // Arrange
            var provider = new DataAnnotationsModelValidatorProvider();
            var mockValidatable = new Mock<IValidatableObject>();
            var metadata = _metadataProvider.GetMetadataForType(() => null, mockValidatable.Object.GetType());

            // Act
            var validators = provider.GetValidators(metadata);

            // Assert
            Assert.Single(validators);
        }
#endif

        // Integration with metadata system

        [Fact]
        public void DoesNotReadPropertyValue()
        {
            // Arrange
            var provider = new DataAnnotationsModelValidatorProvider();
            var model = new ObservableModel();
            var metadata = _metadataProvider.GetMetadataForProperty(() => model.TheProperty, typeof(ObservableModel), "TheProperty");
            var context = new ModelValidationContext(metadata,  null, null, null);

            // Act
            var validators = provider.GetValidators(metadata).ToArray();
            var results = validators.SelectMany(o => o.Validate(context)).ToArray();

            // Assert
            Assert.Empty(validators);
            Assert.False(model.PropertyWasRead());
        }

        private class ObservableModel
        {
            private bool _propertyWasRead;

            public string TheProperty
            {
                get
                {
                    _propertyWasRead = true;
                    return "Hello";
                }
            }

            public bool PropertyWasRead()
            {
                return _propertyWasRead;
            }
        }

        private class BaseModel
        {
            public virtual string MyProperty { get; set; }
        }

        private class DerivedModel : BaseModel
        {
            [StringLength(10)]
            public override string MyProperty
            {
                get { return base.MyProperty; }
                set { base.MyProperty = value; }
            }
        }
    }
}
