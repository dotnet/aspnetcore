// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Uniquely identifies a single field that can be edited. This may correspond to a property on a
    /// model object, or can be any other named value.
    /// </summary>
    public struct FieldIdentifier
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FieldIdentifier"/> structure.
        /// </summary>
        /// <param name="model">The object that owns the field.</param>
        /// <param name="fieldName">The name of the editable field.</param>
        public FieldIdentifier(object model, string fieldName)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (model.GetType().IsValueType)
            {
                throw new ArgumentException("The model must be a reference-typed object.", nameof(model));
            }

            Model = model;

            // Note that we do allow an empty string. This is used by some validation systems
            // as a place to store object-level (not per-property) messages.
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        }

        /// <summary>
        /// Gets the object that owns the editable field.
        /// </summary>
        public object Model { get; }

        /// <summary>
        /// Gets the name of the editable field.
        /// </summary>
        public string FieldName { get; }

        /// <inheritdoc />
        public override int GetHashCode()
            => (Model, FieldName).GetHashCode();

        /// <inheritdoc />
        public override bool Equals(object obj)
            => obj is FieldIdentifier otherIdentifier
            && otherIdentifier.Model == Model
            && string.Equals(otherIdentifier.FieldName, FieldName, StringComparison.Ordinal);
    }
}
