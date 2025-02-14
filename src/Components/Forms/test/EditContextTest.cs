// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public class EditContextTest
{
    [Fact]
    public void CannotUseNullModel()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new EditContext(null));
        Assert.Equal("model", ex.ParamName);
    }

    [Fact]
    public void CanGetModel()
    {
        var model = new object();
        var editContext = new EditContext(model);
        Assert.Same(model, editContext.Model);
    }

    [Fact]
    public void CanConstructFieldIdentifiersForRootModel()
    {
        // Arrange/Act
        var model = new object();
        var editContext = new EditContext(model);
        var fieldIdentifier = editContext.Field("testFieldName");

        // Assert
        Assert.Same(model, fieldIdentifier.Model);
        Assert.Equal("testFieldName", fieldIdentifier.FieldName);
    }

    [Fact]
    public void IsInitiallyUnmodified()
    {
        var editContext = new EditContext(new object());
        Assert.False(editContext.IsModified());
    }

    [Fact]
    public void TracksFieldsAsModifiedWhenValueChanged()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var fieldOnThisModel1 = editContext.Field("field1");
        var fieldOnThisModel2 = editContext.Field("field2");
        var fieldOnOtherModel = new FieldIdentifier(new object(), "field on other model");

        // Act
        editContext.NotifyFieldChanged(fieldOnThisModel1);
        editContext.NotifyFieldChanged(fieldOnOtherModel);

        // Assert
        Assert.True(editContext.IsModified());
        Assert.True(editContext.IsModified(fieldOnThisModel1));
        Assert.False(editContext.IsModified(fieldOnThisModel2));
        Assert.True(editContext.IsModified(fieldOnOtherModel));
    }

    [Fact]
    public void CanClearIndividualModifications()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var fieldThatWasModified = editContext.Field("field1");
        var fieldThatRemainsModified = editContext.Field("field2");
        var fieldThatWasNeverModified = editContext.Field("field that was never modified");
        editContext.NotifyFieldChanged(fieldThatWasModified);
        editContext.NotifyFieldChanged(fieldThatRemainsModified);

        // Act
        editContext.MarkAsUnmodified(fieldThatWasModified);
        editContext.MarkAsUnmodified(fieldThatWasNeverModified);

        // Assert
        Assert.True(editContext.IsModified());
        Assert.False(editContext.IsModified(fieldThatWasModified));
        Assert.True(editContext.IsModified(fieldThatRemainsModified));
        Assert.False(editContext.IsModified(fieldThatWasNeverModified));
    }

    [Fact]
    public void CanClearAllModifications()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var field1 = editContext.Field("field1");
        var field2 = editContext.Field("field2");
        editContext.NotifyFieldChanged(field1);
        editContext.NotifyFieldChanged(field2);

        // Act
        editContext.MarkAsUnmodified();

        // Assert
        Assert.False(editContext.IsModified());
        Assert.False(editContext.IsModified(field1));
        Assert.False(editContext.IsModified(field2));
    }

    [Fact]
    public void RaisesEventWhenFieldIsChanged()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var field1 = new FieldIdentifier(new object(), "fieldname"); // Shows it can be on a different model
        var didReceiveNotification = false;
        editContext.OnFieldChanged += (sender, eventArgs) =>
        {
            Assert.Same(editContext, sender);
            Assert.Equal(field1, eventArgs.FieldIdentifier);
            didReceiveNotification = true;
        };

        // Act
        editContext.NotifyFieldChanged(field1);

        // Assert
        Assert.True(didReceiveNotification);
    }

    [Fact]
    public void CanEnumerateValidationMessagesAcrossAllStoresForSingleField()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var store1 = new ValidationMessageStore(editContext);
        var store2 = new ValidationMessageStore(editContext);
        var field = new FieldIdentifier(new object(), "field");
        var fieldWithNoState = new FieldIdentifier(new object(), "field with no state");
        store1.Add(field, "Store 1 message 1");
        store1.Add(field, "Store 1 message 2");
        store1.Add(new FieldIdentifier(new object(), "otherfield"), "Message for other field that should not appear in results");
        store2.Add(field, "Store 2 message 1");

        // Act/Assert: Can pick out the messages for a field
        Assert.Equal(new[]
        {
                "Store 1 message 1",
                "Store 1 message 2",
                "Store 2 message 1",
            }, editContext.GetValidationMessages(field).OrderBy(x => x)); // Sort because the order isn't defined

        // Act/Assert: It's fine to ask for messages for a field with no associated state
        Assert.Empty(editContext.GetValidationMessages(fieldWithNoState));

        // Act/Assert: After clearing a single store, we only see the results from other stores
        store1.Clear(field);
        Assert.Equal(new[] { "Store 2 message 1", }, editContext.GetValidationMessages(field));
    }

    [Fact]
    public void CanEnumerateValidationMessagesAcrossAllStoresForAllFields()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var store1 = new ValidationMessageStore(editContext);
        var store2 = new ValidationMessageStore(editContext);
        var field1 = new FieldIdentifier(new object(), "field1");
        var field2 = new FieldIdentifier(new object(), "field2");
        store1.Add(field1, "Store 1 field 1 message 1");
        store1.Add(field1, "Store 1 field 1 message 2");
        store1.Add(field2, "Store 1 field 2 message 1");
        store2.Add(field1, "Store 2 field 1 message 1");

        // Act/Assert
        Assert.Equal(new[]
        {
                "Store 1 field 1 message 1",
                "Store 1 field 1 message 2",
                "Store 1 field 2 message 1",
                "Store 2 field 1 message 1",
            }, editContext.GetValidationMessages().OrderBy(x => x)); // Sort because the order isn't defined

        // Act/Assert: After clearing a single store, we only see the results from other stores
        store1.Clear();
        Assert.Equal(new[] { "Store 2 field 1 message 1", }, editContext.GetValidationMessages());
    }

    [Fact]
    public void IsValidWithNoValidationMessages()
    {
        // Arrange
        var editContext = new EditContext(new object());

        // Act
        var isValid = editContext.Validate();

        // assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsInvalidWithValidationMessages()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var messages = new ValidationMessageStore(editContext);
        messages.Add(
            new FieldIdentifier(new object(), "some field"),
            "Some message");

        // Act
        var isValid = editContext.Validate();

        // assert
        Assert.False(isValid);
    }

    [Fact]
    public void RequestsValidationWhenValidateIsCalled()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var messages = new ValidationMessageStore(editContext);
        editContext.OnValidationRequested += (sender, eventArgs) =>
        {
            Assert.Same(editContext, sender);
            Assert.NotNull(eventArgs);
            messages.Add(
                new FieldIdentifier(new object(), "some field"),
                "Some message");
        };

        // Act
        var isValid = editContext.Validate();

        // assert
        Assert.False(isValid);
        Assert.Equal(new[] { "Some message" }, editContext.GetValidationMessages());
    }

    [Fact]
    public void IsInvalidWithValidationMessagesForSpecifiedField()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var messages = new ValidationMessageStore(editContext);
        var fieldOnThisModel1 = editContext.Field("field1");
        var fieldOnThisModel2 = editContext.Field("field2");
        messages.Add(
            fieldOnThisModel1,
            "Some message");

        // Assert
        Assert.False(editContext.Validate());
        Assert.False(editContext.IsValid(fieldOnThisModel1));
        Assert.True(editContext.IsValid(fieldOnThisModel2));
    }

    [Fact]
    public void LookingUpModel_ThatOverridesGetHashCodeAndEquals_Works()
    {
        // Test for https://github.com/aspnet/AspNetCore/issues/18069
        // Arrange
        var model = new EquatableModel();
        var editContext = new EditContext(model);

        editContext.NotifyFieldChanged(editContext.Field(nameof(EquatableModel.Property)));

        model.Property = "new value";

        Assert.True(editContext.IsModified(editContext.Field(nameof(EquatableModel.Property))));
    }

    [Fact]
    public void Properties_CanRetrieveViaIndexer()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var key1 = new object();
        var key2 = new object();
        var key3 = new object();
        var value1 = new object();
        var value2 = new object();

        // Initially, the values are not present
        Assert.Throws<KeyNotFoundException>(() => editContext.Properties[key1]);

        // Can store and retrieve values
        editContext.Properties[key1] = value1;
        editContext.Properties[key2] = value2;
        Assert.Same(value1, editContext.Properties[key1]);
        Assert.Same(value2, editContext.Properties[key2]);

        // Unrelated keys are still not found
        Assert.Throws<KeyNotFoundException>(() => editContext.Properties[key3]);
    }

    [Fact]
    public void Properties_CanRetrieveViaTryGetValue()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var key1 = new object();
        var key2 = new object();
        var key3 = new object();
        var value1 = new object();
        var value2 = new object();

        // Initially, the values are not present
        Assert.False(editContext.Properties.TryGetValue(key1, out _));

        // Can store and retrieve values
        editContext.Properties[key1] = value1;
        editContext.Properties[key2] = value2;
        Assert.True(editContext.Properties.TryGetValue(key1, out var retrievedValue1));
        Assert.True(editContext.Properties.TryGetValue(key2, out var retrievedValue2));
        Assert.Same(value1, retrievedValue1);
        Assert.Same(value2, retrievedValue2);

        // Unrelated keys are still not found
        Assert.False(editContext.Properties.TryGetValue(key3, out _));
    }

    [Fact]
    public void Properties_CanRemove()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var key = new object();
        var value = new object();
        editContext.Properties[key] = value;

        // Act
        var resultForExistingKey = editContext.Properties.Remove(key);
        var resultForNonExistingKey = editContext.Properties.Remove(new object());

        // Assert
        Assert.True(resultForExistingKey);
        Assert.False(resultForNonExistingKey);
        Assert.False(editContext.Properties.TryGetValue(key, out _));
        Assert.Throws<KeyNotFoundException>(() => editContext.Properties[key]);
    }

    class EquatableModel : IEquatable<EquatableModel>
    {
        public string Property { get; set; } = "";

        public bool Equals(EquatableModel other)
        {
            return string.Equals(Property, other?.Property, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Property);
        }
    }
}
