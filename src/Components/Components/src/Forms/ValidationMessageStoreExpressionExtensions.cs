// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Provides extension methods to simplify using <see cref="ValidationMessageStore"/> with expressions.
    /// </summary>
    public static class ValidationMessageStoreExpressionExtensions
    {
        /// <summary>
        /// Adds a validation message for the specified field.
        /// </summary>
        /// <param name="store">The <see cref="ValidationMessageStore"/>.</param>
        /// <param name="accessor">Identifies the field for which to add the message.</param>
        /// <param name="message">The validation message.</param>
        public static void Add(this ValidationMessageStore store, Expression<Func<object>> accessor, string message)
            => store.Add(FieldIdentifier.Create(accessor), message);

        /// <summary>
        /// Adds the messages from the specified collection for the specified field.
        /// </summary>
        /// <param name="store">The <see cref="ValidationMessageStore"/>.</param>
        /// <param name="accessor">Identifies the field for which to add the messages.</param>
        /// <param name="messages">The validation messages to be added.</param>
        public static void AddRange(this ValidationMessageStore store, Expression<Func<object>> accessor, IEnumerable<string> messages)
            => store.AddRange(FieldIdentifier.Create(accessor), messages);

        /// <summary>
        /// Removes all messages within this <see cref="ValidationMessageStore"/> for the specified field.
        /// </summary>
        /// <param name="store">The <see cref="ValidationMessageStore"/>.</param>
        /// <param name="accessor">Identifies the field for which to remove the messages.</param>
        public static void Clear(this ValidationMessageStore store, Expression<Func<object>> accessor)
            => store.Clear(FieldIdentifier.Create(accessor));
    }
}
