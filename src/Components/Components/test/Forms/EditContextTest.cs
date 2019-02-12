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
        public void TracksFieldsAsModifiedWhenChanged()
        {
            // Arrange
            var editContext = new EditContext(new object());
            var field1 = editContext.Field("field1");
            var field2 = editContext.Field("field2");

            // Act
            editContext.NotifyFieldChanged(field1);

            // Assert
            Assert.True(editContext.IsModified());
            Assert.True(editContext.IsModified(field1));
            Assert.False(editContext.IsModified(field2));
        }
    }
}
