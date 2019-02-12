// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Holds validation messages for an <see cref="EditContext"/>.
    /// </summary>
    public class ValidationMessageStore
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
        public void Add(FieldIdentifier fieldIdentifier, string message)
            => GetOrCreateMessagesListForField(fieldIdentifier).Add(message);

        /// <summary>
        /// Adds the messages from the specified collection for the specified field.
        /// </summary>
        /// <param name="fieldIdentifier">The identifier for the field.</param>
        /// <param name="messages">The validation messages to be added.</param>
        public void AddRange(FieldIdentifier fieldIdentifier, IEnumerable<string> messages)
            => GetOrCreateMessagesListForField(fieldIdentifier).AddRange(messages);

        /// <summary>
        /// Gets the validation messages within this <see cref="ValidationMessageStore"/> for the specified field.
        /// </summary>
        /// <param name="fieldIdentifier">The identifier for the field.</param>
        /// <returns>The validation messages for the specified field within this <see cref="ValidationMessageStore"/>.</returns>
        public IEnumerable<string> this[FieldIdentifier fieldIdentifier]
        {
            get => _messages.TryGetValue(fieldIdentifier, out var messages) ? messages : Enumerable.Empty<string>();
        }

        /// <summary>
        /// Removes all messages within this <see cref="ValidationMessageStore"/>.
        /// </summary>
        public void Clear()
            => _messages.Clear();

        /// <summary>
        /// Removes all messages within this <see cref="ValidationMessageStore"/> for the specified field.
        /// </summary>
        /// <param name="fieldIdentifier">The identifier for the field.</param>
        public void Clear(FieldIdentifier fieldIdentifier)
            => _messages.Remove(fieldIdentifier);

        private List<string> GetOrCreateMessagesListForField(FieldIdentifier fieldIdentifier)
        {
            if (!_messages.TryGetValue(fieldIdentifier, out var messagesForField))
            {
                messagesForField = new List<string>();
                _messages.Add(fieldIdentifier, messagesForField);
            }

            return messagesForField;
        }
    }
}
