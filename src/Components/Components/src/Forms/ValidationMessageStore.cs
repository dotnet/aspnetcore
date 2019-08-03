// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Holds validation messages for an <see cref="EditContext"/>.
    /// </summary>
    public sealed class ValidationMessageStore
    {
        private readonly EditContext _editContext;
        private readonly Dictionary<FieldIdentifier, List<string>> _messages = new Dictionary<FieldIdentifier, List<string>>();

        /// <summary>
        /// Creates an instance of <see cref="ValidationMessageStore"/>.
        /// </summary>
        /// <param name="editContext">The <see cref="EditContext"/> with which this store should be associated.</param>
        public ValidationMessageStore(EditContext editContext)
        {
            _editContext = editContext ?? throw new ArgumentNullException(nameof(editContext));
        }

        /// <summary>
        /// Adds a validation message for the specified field.
        /// </summary>
        /// <param name="fieldIdentifier">The identifier for the field.</param>
        /// <param name="message">The validation message.</param>
        public void Add(in FieldIdentifier fieldIdentifier, string message)
            => GetOrCreateMessagesListForField(fieldIdentifier).Add(message);

        /// <summary>
        /// Adds a validation message for the specified field.
        /// </summary>
        /// <param name="accessor">Identifies the field for which to add the message.</param>
        /// <param name="message">The validation message.</param>
        public void Add(Expression<Func<object>> accessor, string message)
            => Add(FieldIdentifier.Create(accessor), message);

        /// <summary>
        /// Adds the messages from the specified collection for the specified field.
        /// </summary>
        /// <param name="fieldIdentifier">The identifier for the field.</param>
        /// <param name="messages">The validation messages to be added.</param>
        public void Add(in FieldIdentifier fieldIdentifier, IEnumerable<string> messages)
            => GetOrCreateMessagesListForField(fieldIdentifier).AddRange(messages);

        /// <summary>
        /// Adds the messages from the specified collection for the specified field.
        /// </summary>
        /// <param name="accessor">Identifies the field for which to add the messages.</param>
        /// <param name="messages">The validation messages to be added.</param>
        public void Add(Expression<Func<object>> accessor, IEnumerable<string> messages)
            => Add(FieldIdentifier.Create(accessor), messages);

        /// <summary>
        /// Gets the validation messages within this <see cref="ValidationMessageStore"/> for the specified field.
        ///
        /// To get the validation messages across all validation message stores, use <see cref="EditContext.GetValidationMessages(FieldIdentifier)"/> instead
        /// </summary>
        /// <param name="fieldIdentifier">The identifier for the field.</param>
        /// <returns>The validation messages for the specified field within this <see cref="ValidationMessageStore"/>.</returns>
        public IEnumerable<string> this[FieldIdentifier fieldIdentifier]
            => _messages.TryGetValue(fieldIdentifier, out var messages) ? messages : Enumerable.Empty<string>();

        /// <summary>
        /// Gets the validation messages within this <see cref="ValidationMessageStore"/> for the specified field.
        ///
        /// To get the validation messages across all validation message stores, use <see cref="EditContext.GetValidationMessages(FieldIdentifier)"/> instead
        /// </summary>
        /// <param name="accessor">The identifier for the field.</param>
        /// <returns>The validation messages for the specified field within this <see cref="ValidationMessageStore"/>.</returns>
        public IEnumerable<string> this[Expression<Func<object>> accessor]
            => this[FieldIdentifier.Create(accessor)];

        /// <summary>
        /// Removes all messages within this <see cref="ValidationMessageStore"/>.
        /// </summary>
        public void Clear()
        {
            foreach (var fieldIdentifier in _messages.Keys)
            {
                DissociateFromField(fieldIdentifier);
            }

            _messages.Clear();
        }

        /// <summary>
        /// Removes all messages within this <see cref="ValidationMessageStore"/> for the specified field.
        /// </summary>
        /// <param name="accessor">Identifies the field for which to remove the messages.</param>
        public void Clear(Expression<Func<object>> accessor)
            => Clear(FieldIdentifier.Create(accessor));

        /// <summary>
        /// Removes all messages within this <see cref="ValidationMessageStore"/> for the specified field.
        /// </summary>
        /// <param name="fieldIdentifier">The identifier for the field.</param>
        public void Clear(in FieldIdentifier fieldIdentifier)
        {
            DissociateFromField(fieldIdentifier);
            _messages.Remove(fieldIdentifier);
        }

        private List<string> GetOrCreateMessagesListForField(in FieldIdentifier fieldIdentifier)
        {
            if (!_messages.TryGetValue(fieldIdentifier, out var messagesForField))
            {
                messagesForField = new List<string>();
                _messages.Add(fieldIdentifier, messagesForField);
                AssociateWithField(fieldIdentifier);
            }

            return messagesForField;
        }

        private void AssociateWithField(in FieldIdentifier fieldIdentifier)
            => _editContext.GetFieldState(fieldIdentifier, ensureExists: true).AssociateWithValidationMessageStore(this);

        private void DissociateFromField(in FieldIdentifier fieldIdentifier)
            => _editContext.GetFieldState(fieldIdentifier, ensureExists: false)?.DissociateFromValidationMessageStore(this);
    }
}
