// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public class ValidationMessageStoreTest
{
    [Fact]
    public void CannotUseNullEditContext()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new ValidationMessageStore(null));
        Assert.Equal("editContext", ex.ParamName);
    }

    [Fact]
    public void CanCreateForEditContext()
    {
        new ValidationMessageStore(new EditContext(new object()));
    }

    [Fact]
    public void CanAddMessages()
    {
        // Arrange
        var messages = new ValidationMessageStore(new EditContext(new object()));
        var field1 = new FieldIdentifier(new object(), "field1");
        var field2 = new FieldIdentifier(new object(), "field2");
        var field3 = new FieldIdentifier(new object(), "field3");

        // Act
        messages.Add(field1, "Field 1 message 1");
        messages.Add(field1, "Field 1 message 2");
        messages.Add(field2, "Field 2 message 1");

        // Assert
        Assert.Equal(new[] { "Field 1 message 1", "Field 1 message 2" }, messages[field1]);
        Assert.Equal(new[] { "Field 2 message 1" }, messages[field2]);
        Assert.Empty(messages[field3]);
    }

    [Fact]
    public void CanAddMessagesMultiple()
    {
        // Arrange
        var messages = new ValidationMessageStore(new EditContext(new object()));
        var field1 = new FieldIdentifier(new object(), "field1");
        var entries = new[] { "A", "B", "C" };

        // Act
        messages.Add(field1, entries);

        // Assert
        Assert.Equal(entries, messages[field1]);
    }

    [Fact]
    public void CanClearMessagesForSingleField()
    {
        // Arrange
        var messages = new ValidationMessageStore(new EditContext(new object()));
        var field1 = new FieldIdentifier(new object(), "field1");
        var field2 = new FieldIdentifier(new object(), "field2");
        messages.Add(field1, "Field 1 message 1");
        messages.Add(field1, "Field 1 message 2");
        messages.Add(field2, "Field 2 message 1");

        // Act
        messages.Clear(field1);

        // Assert
        Assert.Empty(messages[field1]);
        Assert.Equal(new[] { "Field 2 message 1" }, messages[field2]);
    }

    [Fact]
    public void CanClearMessagesForAllFields()
    {
        // Arrange
        var messages = new ValidationMessageStore(new EditContext(new object()));
        var field1 = new FieldIdentifier(new object(), "field1");
        var field2 = new FieldIdentifier(new object(), "field2");
        messages.Add(field1, "Field 1 message 1");
        messages.Add(field2, "Field 2 message 1");

        // Act
        messages.Clear();

        // Assert
        Assert.Empty(messages[field1]);
        Assert.Empty(messages[field2]);
    }
#nullable enable
    [Fact]
    public void CanAddMessagesUsingExpressionWithNullableProperty()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);

        messages.Add(() => model.Text, "This value is not valid");

        var fieldIdentifier = FieldIdentifier.Create(() => model.Text);
        Assert.Equal(new[] { "This value is not valid" }, messages[fieldIdentifier]);
    }

    [Fact]
    public void CanAddMultipleMessagesUsingExpressionWithNullableProperty()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        var validationMessages = new[] { "First error", "Second error" };

        messages.Add(() => model.Text, validationMessages);

        var fieldIdentifier = FieldIdentifier.Create(() => model.Text);
        Assert.Equal(validationMessages, messages[fieldIdentifier]);
    }

    private class TestModel
    {
        public string? Text { get; set; }
    }
#nullable disable
}
