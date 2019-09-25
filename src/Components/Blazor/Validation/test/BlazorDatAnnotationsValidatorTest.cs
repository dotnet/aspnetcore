// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;
using Xunit;

namespace Microsoft.AspNetCore.Components
{
    public class BlazorDatAnnotationsValidatorTest
    {
        public class SimpleModel
        {
            [Required]
            public string Name { get; set; }

            [Range(1, 16)]
            public int Age { get; set; }
        }

        [Fact]
        public void ValidateObject_SimpleObject()
        {
            var model = new SimpleModel
            {
                Age = 23,
            };

            var editContext = Validate(model);
            var messages = editContext.GetValidationMessages(() => model.Name);
            Assert.Single(messages);

            messages = editContext.GetValidationMessages(() => model.Age);
            Assert.Single(messages);

            Assert.Equal(2, editContext.GetValidationMessages().Count());
        }

        [Fact]
        public void ValidateObject_SimpleObject_AllValid()
        {
            var model = new SimpleModel { Name = "Test", Age = 5 };

            var editContext = Validate(model);
            var messages = editContext.GetValidationMessages(() => model.Name);
            Assert.Empty(messages);

            messages = editContext.GetValidationMessages(() => model.Age);
            Assert.Empty(messages);

            Assert.Empty(editContext.GetValidationMessages());
        }

        public class ModelWithComplexProperty
        {
            [Required]
            public string Property1 { get; set; }

            [ValidateComplexType]
            public SimpleModel SimpleModel { get; set; }
        }

        [Fact]
        public void ValidateObject_NullComplexProperty()
        {
            var model = new ModelWithComplexProperty();

            var editContext = Validate(model);
            var messages = editContext.GetValidationMessages(() => model.Property1);
            Assert.Single(messages);

            Assert.Single(editContext.GetValidationMessages());
        }

        [Fact]
        public void ValidateObject_ModelWithComplexProperties()
        {
            var model = new ModelWithComplexProperty { SimpleModel = new SimpleModel() };

            var editContext = Validate(model);
            var messages = editContext.GetValidationMessages(() => model.Property1);
            Assert.Single(messages);

            messages = editContext.GetValidationMessages(() => model.SimpleModel);
            Assert.Empty(messages);

            messages = editContext.GetValidationMessages(() => model.SimpleModel.Age);
            Assert.Single(messages);

            messages = editContext.GetValidationMessages(() => model.SimpleModel.Name);
            Assert.Single(messages);

            Assert.Equal(3, editContext.GetValidationMessages().Count());
        }

        [Fact]
        public void ValidateObject_ModelWithComplexProperties_SomeValid()
        {
            var model = new ModelWithComplexProperty
            {
                Property1 = "Value",
                SimpleModel = new SimpleModel { Name = "Some Value" },
            };

            var editContext = Validate(model);
            var messages = editContext.GetValidationMessages(() => model.Property1);
            Assert.Empty(messages);

            messages = editContext.GetValidationMessages(() => model.SimpleModel);
            Assert.Empty(messages);

            messages = editContext.GetValidationMessages(() => model.SimpleModel.Age);
            Assert.Single(messages);

            messages = editContext.GetValidationMessages(() => model.SimpleModel.Name);
            Assert.Empty(messages);

            Assert.Single(editContext.GetValidationMessages());
        }

        public class TestValidatableObject : IValidatableObject
        {
            [Required]
            public string Name { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                yield return new ValidationResult("Custom validation error");
            }
        }

        public class ModelWithValidatableComplexProperty
        {
            [Required]
            public string Property1 { get; set; }

            [ValidateComplexType]
            public TestValidatableObject Property2 { get; set; } = new TestValidatableObject();
        }

        [Fact]
        public void ValidateObject_ValidatableComplexProperty()
        {
            var model = new ModelWithValidatableComplexProperty();

            var editContext = Validate(model);
            var messages = editContext.GetValidationMessages(() => model.Property1);
            Assert.Single(messages);

            messages = editContext.GetValidationMessages(() => model.Property2);
            Assert.Empty(messages);

            messages = editContext.GetValidationMessages(() => model.Property2.Name);
            Assert.Single(messages);

            Assert.Equal(2, editContext.GetValidationMessages().Count());
        }

        [Fact]
        public void ValidateObject_ValidatableComplexProperty_ValidatesIValidatableProperty()
        {
            var model = new ModelWithValidatableComplexProperty
            {
                Property2 = new TestValidatableObject { Name = "test" },
            };

            var editContext = Validate(model);
            var messages = editContext.GetValidationMessages(() => model.Property1);
            Assert.Single(messages);

            messages = editContext.GetValidationMessages(() => model.Property2);
            Assert.Single(messages);

            messages = editContext.GetValidationMessages(() => model.Property2.Name);
            Assert.Empty(messages);

            Assert.Equal(2, editContext.GetValidationMessages().Count());
        }

        [Fact]
        public void ValidateObject_ModelIsIValidatable_PropertyHasError()
        {
            var model = new TestValidatableObject();

            var editContext = Validate(model);
            var messages = editContext.GetValidationMessages(new FieldIdentifier(model, string.Empty));
            Assert.Empty(messages);

            messages = editContext.GetValidationMessages(() => model.Name);
            Assert.Single(messages);

            Assert.Single(editContext.GetValidationMessages());
        }

        [Fact]
        public void ValidateObject_ModelIsIValidatable_ModelHasError()
        {
            var model = new TestValidatableObject { Name = "test" };

            var editContext = Validate(model);
            var messages = editContext.GetValidationMessages(new FieldIdentifier(model, string.Empty));
            Assert.Single(messages);

            messages = editContext.GetValidationMessages(() => model.Name);
            Assert.Empty(messages);

            Assert.Single(editContext.GetValidationMessages());
        }

        [Fact]
        public void ValidateObject_CollectionModel()
        {
            var model = new List<SimpleModel>
            {
                new SimpleModel(),
                new SimpleModel { Name = "test", },
            };

            var editContext = Validate(model);

            var item = model[0];
            var messages = editContext.GetValidationMessages(new FieldIdentifier(model, "0"));
            Assert.Empty(messages);

            messages = editContext.GetValidationMessages(() => item.Name);
            Assert.Single(messages);

            messages = editContext.GetValidationMessages(() => item.Age);
            Assert.Single(messages);

            item = model[1];
            messages = editContext.GetValidationMessages(new FieldIdentifier(model, "1"));
            Assert.Empty(messages);

            messages = editContext.GetValidationMessages(() => item.Name);
            Assert.Empty(messages);

            messages = editContext.GetValidationMessages(() => item.Age);
            Assert.Single(messages);

            Assert.Equal(3, editContext.GetValidationMessages().Count());
        }

        [Fact]
        public void ValidateObject_CollectionValidatableModel()
        {
            var model = new List<TestValidatableObject>
            {
                new TestValidatableObject(),
                new TestValidatableObject { Name = "test", },
            };

            var editContext = Validate(model);

            var item = model[0];
            var messages = editContext.GetValidationMessages(() => item.Name);
            Assert.Single(messages);

            item = model[1];
            Assert.Single(messages);

            messages = editContext.GetValidationMessages(() => item.Name);
            Assert.Empty(messages);

            Assert.Equal(2, editContext.GetValidationMessages().Count());
        }

        private class Level1Validation
        {
            [ValidateComplexType]
            public Level2Validation Level2 { get; set; }
        }

        public class Level2Validation
        {
            [ValidateComplexType]
            public SimpleModel Level3 { get; set; }
        }

        [Fact]
        public void ValidateObject_ManyLevels()
        {
            var model = new Level1Validation
            {
                Level2 = new Level2Validation
                {
                    Level3 = new SimpleModel
                    {
                        Age = 47,
                    }
                }
            };

            var editContext = Validate(model);
            var level3 = model.Level2.Level3;

            var messages = editContext.GetValidationMessages(() => level3.Name);
            Assert.Single(messages);

            messages = editContext.GetValidationMessages(() => level3.Age);
            Assert.Single(messages);

            Assert.Equal(2, editContext.GetValidationMessages().Count());
        }

        [Fact]
        public void ValidateField_PropertyValid()
        {
            var model = new SimpleModel { Age = 1 };
            var fieldIdentifier = FieldIdentifier.Create(() => model.Age);

            var editContext = ValidateField(model, fieldIdentifier);
            var messages = editContext.GetValidationMessages(fieldIdentifier);
            Assert.Empty(messages);

            Assert.Empty(editContext.GetValidationMessages());
        }

        [Fact]
        public void ValidateField_PropertyInvalid()
        {
            var model = new SimpleModel { Age = 42 };
            var fieldIdentifier = FieldIdentifier.Create(() => model.Age);

            var editContext = ValidateField(model, fieldIdentifier);
            var messages = editContext.GetValidationMessages(fieldIdentifier);
            Assert.Single(messages);

            Assert.Single(editContext.GetValidationMessages());
        }

        [Fact]
        public void ValidateField_AfterSubmitValidation()
        {
            var model = new SimpleModel { Age = 42 };
            var fieldIdentifier = FieldIdentifier.Create(() => model.Age);

            var editContext = Validate(model);
            var messages = editContext.GetValidationMessages(fieldIdentifier);
            Assert.Single(messages);

            Assert.Equal(2, editContext.GetValidationMessages().Count());

            model.Age = 4;

            editContext.NotifyFieldChanged(fieldIdentifier);
            messages = editContext.GetValidationMessages(fieldIdentifier);
            Assert.Empty(messages);

            Assert.Single(editContext.GetValidationMessages());
        }

        [Fact]
        public void ValidateField_ModelWithComplexProperty()
        {
            var model = new ModelWithComplexProperty
            {
                SimpleModel = new SimpleModel { Age = 1 },
            };
            var fieldIdentifier = FieldIdentifier.Create(() => model.SimpleModel.Name);

            var editContext = ValidateField(model, fieldIdentifier);
            var messages = editContext.GetValidationMessages(fieldIdentifier);
            Assert.Single(messages);

            Assert.Single(editContext.GetValidationMessages());
        }

        [Fact]
        public void ValidateField_ModelWithComplexProperty_AfterSubmitValidation()
        {
            var model = new ModelWithComplexProperty
            {
                Property1 = "test",
                SimpleModel = new SimpleModel { Age = 29, Name = "Test" },
            };
            var fieldIdentifier = FieldIdentifier.Create(() => model.SimpleModel.Age);

            var editContext = Validate(model);
            var messages = editContext.GetValidationMessages(fieldIdentifier);
            Assert.Single(messages);

            model.SimpleModel.Age = 9;
            editContext.NotifyFieldChanged(fieldIdentifier);

            messages = editContext.GetValidationMessages(fieldIdentifier);
            Assert.Empty(messages);
            Assert.Empty(editContext.GetValidationMessages());
        }

        private static EditContext Validate(object model)
        {
            var editContext = new EditContext(model);
            var validator = new TestBlazorDataAnnotationsValidator { EditContext = editContext, };
            validator.OnInitialized();

            editContext.Validate();

            return editContext;
        }

        private static EditContext ValidateField(object model, in FieldIdentifier field)
        {
            var editContext = new EditContext(model);
            var validator = new TestBlazorDataAnnotationsValidator { EditContext = editContext, };
            validator.OnInitialized();

            editContext.NotifyFieldChanged(field);

            return editContext;
        }

        private class TestBlazorDataAnnotationsValidator : BlazorDataAnnotationsValidator
        {
            public new void OnInitialized() => base.OnInitialized();
        }
    }
}
