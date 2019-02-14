// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Provides extension methods to simplify using <see cref="EditContext"/> with expressions.
    /// </summary>
    public static class EditContextExpressionExtensions
    {
        /// <summary>
        /// Gets the current validation messages for the specified field.
        ///
        /// This method does not perform validation itself. It only returns messages determined by previous validation actions.
        /// </summary>
        /// <param name="editContext">The <see cref="EditContext"/>.</param>
        /// <param name="accessor">Identifies the field whose current validation messages should be returned.</param>
        /// <returns>The current validation messages for the specified field.</returns>
        public static IEnumerable<string> GetValidationMessages(this EditContext editContext, Expression<Func<object>> accessor)
            => editContext.GetValidationMessages(FieldIdentifier.Create(accessor));

        /// <summary>
        /// Determines whether the specified fields in this <see cref="EditContext"/> has been modified.
        /// </summary>
        /// <param name="editContext">The <see cref="EditContext"/>.</param>
        /// <param name="accessor">Identifies the field whose current validation messages should be returned.</param>
        /// <returns>True if the field has been modified; otherwise false.</returns>
        public static bool IsModified(this EditContext editContext, Expression<Func<object>> accessor)
            => editContext.IsModified(FieldIdentifier.Create(accessor));
    }
}
