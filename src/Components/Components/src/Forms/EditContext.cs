// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Forms
{
    /* Async validation plan
     * =====================
     * - Add method: editContext.AddValidationTask(FieldIdentifier f, Task t)
     *   It adds the task to a HashSet<Task> on both the FieldState and the EditContext,
     *   so we can easily get all the tasks for a given field and across the whole EditContext
     *   Also it awaits the task completion, and then regardless of outcome (success/fail/cancel),
     *   it removes the task from those hashsets.
     * - Add method: editContext.WhenAllValidationTasks()
     *   Add method: editContext.WhenAllValidationTasks(FieldIdentifier f)
     *   These return Task.WhenAll(hashSet.Values), or Task.Completed if there are none
     * - Optionally also add editContext.HasPendingValidationTasks()
     * - Add method: editContext.ValidateAsync() that awaits all the validation tasks then
     *   returns true if there are no validation messages, false otherwise
     * - Now a validation library can register tasks whenever it starts an async validation process,
     *   can cancel them if it wants, and can still issue ValidationResultsChanged notifications when
     *   each task completes. So a UI can determine whether to show "pending" state on a per-field
     *   and per-form basis, and will re-render as each field's results arrive.
     * - Note: it's unclear why we'd need WhenAllValidationTasks(FieldIdentifier) (i.e., per-field),
     *   since you wouldn't "await" this to get per-field updates (rather, you'd use ValidationResultsChanged).
     *   Maybe WhenAllValidationTasks can be private, and only called by ValidateAsync. We just expose
     *   public HasPendingValidationTasks (per-field and per-edit-context).
     * Will implement this shortly after getting more of the system in place, assuming it still
     * appears to be the correct design.
     */

    /// <summary>
    /// Holds metadata related to a data editing process, such as flags to indicate which
    /// fields have been modified and the current set of validation messages.
    /// </summary>
    public class EditContext
    {
        private readonly Dictionary<FieldIdentifier, FieldState> _fieldStates = new Dictionary<FieldIdentifier, FieldState>();

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
        /// An event that is raised when a field value changes.
        /// </summary>
        public event EventHandler<FieldIdentifier> OnFieldChanged;

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
        /// Signals that the value for the specified field has changed.
        /// </summary>
        /// <param name="fieldIdentifier">Identifies the field whose value has been changed.</param>
        public void NotifyFieldChanged(FieldIdentifier fieldIdentifier)
        {
            GetFieldState(fieldIdentifier, ensureExists: true).IsModified = true;
            OnFieldChanged?.Invoke(this, fieldIdentifier);
        }

        /// <summary>
        /// Clears any modification flag that may be tracked for the specified field.
        /// </summary>
        /// <param name="fieldIdentifier">Identifies the field whose modification flag (if any) should be cleared.</param>
        public void MarkAsUnmodified(FieldIdentifier fieldIdentifier)
        {
            if (_fieldStates.TryGetValue(fieldIdentifier, out var state))
            {
                state.IsModified = false;
            }
        }

        /// <summary>
        /// Clears all modification flags within this <see cref="EditContext"/>.
        /// </summary>
        public void MarkAsUnmodified()
        {
            foreach (var state in _fieldStates.Values)
            {
                state.IsModified = false;
            }
        }

        /// <summary>
        /// Determines whether any of the fields in this <see cref="EditContext"/> have been modified.
        /// </summary>
        /// <returns>True if any of the fields in this <see cref="EditContext"/> have been modified; otherwise false.</returns>
        public bool IsModified()
            // If necessary, we could consider caching the overall "is modified" state and only recomputing
            // when there's a call to NotifyFieldModified/NotifyFieldUnmodified
            => _fieldStates.Values.Any(state => state.IsModified);

        /// <summary>
        /// Gets the current validation messages across all fields.
        ///
        /// This method does not perform validation itself. It only returns messages determined by previous validation actions.
        /// </summary>
        /// <returns>The current validation messages.</returns>
        public IEnumerable<string> GetValidationMessages()
            // Since we're only enumerating the fields for which we have a non-null state, the cost of this grows
            // based on how many fields have been modified or have associated validation messages
            => _fieldStates.Values.SelectMany(state => state.GetValidationMessages());

        /// <summary>
        /// Gets the current validation messages for the specified field.
        ///
        /// This method does not perform validation itself. It only returns messages determined by previous validation actions.
        /// </summary>
        /// <param name="fieldIdentifier">Identifies the field whose current validation messages should be returned.</param>
        /// <returns>The current validation messages for the specified field.</returns>
        public IEnumerable<string> GetValidationMessages(FieldIdentifier fieldIdentifier)
            => _fieldStates.TryGetValue(fieldIdentifier, out var state) ? state.GetValidationMessages() : Enumerable.Empty<string>();

        /// <summary>
        /// Determines whether the specified fields in this <see cref="EditContext"/> has been modified.
        /// </summary>
        /// <returns>True if the field has been modified; otherwise false.</returns>
        public bool IsModified(FieldIdentifier fieldIdentifier)
            => _fieldStates.TryGetValue(fieldIdentifier, out var state)
            ? state.IsModified
            : false;

        internal FieldState GetFieldState(FieldIdentifier fieldIdentifier, bool ensureExists)
        {
            if (!_fieldStates.TryGetValue(fieldIdentifier, out var state) && ensureExists)
            {
                state = new FieldState(fieldIdentifier);
                _fieldStates.Add(fieldIdentifier, state);
            }

            return state;
        }
    }
}
