// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Holds state related to a data editing process.
    /// </summary>
    public class EditContext
    {
        /// <summary>
        /// Constructs an instance of <see cref="EditContext"/>.
        /// </summary>
        /// <param name="model">The model object for the <see cref="EditContext"/>. This object should hold the data being edited, for example as a set of properties.</param>
        public EditContext(object model)
        {
            // The only reason we disallow null is because you'd almost always want one, and if you
            // really don't, you can pass an empty object then ignore it. Ensuring it's nonnull
            // simplifies things for all consumers of EditContext.
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <summary>
        /// Supplies a <see cref="FieldIdentifier"/> corresponding to a specified field name
        /// on this <see cref="EditContext"/>'s <see cref="Model"/>.
        /// </summary>
        /// <param name="fieldName">The name of the editable field.</param>
        /// <returns>A <see cref="FieldIdentifier"/> corresponding to a specified field name on this <see cref="EditContext"/>'s <see cref="Model"/>.</returns>
        public FieldIdentifier Field(string fieldName)
            => new FieldIdentifier(Model, fieldName);

        /// <summary>
        /// Gets the model object for this <see cref="EditContext"/>.
        /// </summary>
        public object Model { get; }

        /// <summary>
        /// Signals that the specified field within this <see cref="EditContext"/> has been changed.
        /// </summary>
        /// <param name="fieldIdentifier">Identifies the field whose value has been changed.</param>
        public void NotifyFieldChanged(FieldIdentifier fieldIdentifier)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines whether any of the fields in this <see cref="EditContext"/> have been modified.
        /// </summary>
        /// <returns>True if any of the fields in this <see cref="EditContext"/> have been modified; otherwise false.</returns>
        public bool IsModified()
        {
            return false;
        }

        /// <summary>
        /// Determines whether the specified fields in this <see cref="EditContext"/> has been modified.
        /// </summary>
        /// <returns>True if the field has been modified; otherwise false.</returns>
        public bool IsModified(FieldIdentifier fieldIdentifier)
        {
            return false;
        }
    }
}
