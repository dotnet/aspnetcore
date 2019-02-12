// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Forms;
using System;
using Xunit;

namespace Microsoft.AspNetCore.Components.Tests.Forms
{
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
            editContext.OnFieldChanged += (sender, changedFieldIdentifier) =>
            {
                Assert.Same(editContext, sender);
                Assert.Equal(field1, changedFieldIdentifier);
                didReceiveNotification = true;
            };

            // Act
            editContext.NotifyFieldChanged(field1);

            // Assert
            Assert.True(didReceiveNotification);
        }
    }
}
