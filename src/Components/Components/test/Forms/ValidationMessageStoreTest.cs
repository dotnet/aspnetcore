// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Tests.Forms
{
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
        public void CanAddMessagesByRange()
        {
            // Arrange
            var messages = new ValidationMessageStore(new EditContext(new object()));
            var field1 = new FieldIdentifier(new object(), "field1");
            var entries = new[] { "A", "B", "C" };

            // Act
            messages.AddRange(field1, entries);

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

        [Fact]
        public void CanEnumerateMessagesAcrossAllStoresForSingleField()
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
        public void CanEnumerateMessagesAcrossAllStoresForAllFields()
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
    }
}
