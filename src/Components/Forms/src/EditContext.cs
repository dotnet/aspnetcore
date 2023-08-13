// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Holds metadata related to a data editing process, such as flags to indicate which
/// fields have been modified and the current set of validation messages.
/// </summary>
public sealed class EditContext
{
    // Note that EditContext tracks state for any FieldIdentifier you give to it, plus
    // the underlying storage is sparse. As such, none of the APIs have a "field not found"
    // error state. If you give us an unrecognized FieldIdentifier, that just means we
    // didn't yet track any state for it, so we behave as if it's in the default state
    // (valid and unmodified).
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
        Properties = new EditContextProperties();
    }

    /// <summary>
    /// An event that is raised when a field value changes.
    /// </summary>
    public event EventHandler<FieldChangedEventArgs>? OnFieldChanged;

    /// <summary>
    /// An event that is raised when validation is requested.
    /// </summary>
    public event EventHandler<ValidationRequestedEventArgs>? OnValidationRequested;

    /// <summary>
    /// An event that is raised when validation state has changed.
    /// </summary>
    public event EventHandler<ValidationStateChangedEventArgs>? OnValidationStateChanged;

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
    /// Gets a collection of arbitrary properties associated with this instance.
    /// </summary>
    public EditContextProperties Properties { get; }

    /// <summary>
    /// Gets whether field identifiers should be generated for &lt;input&gt; elements.
    /// </summary>
    public bool ShouldUseFieldIdentifiers { get; set; } = !OperatingSystem.IsBrowser();

    /// <summary>
    /// Signals that the value for the specified field has changed.
    /// </summary>
    /// <param name="fieldIdentifier">Identifies the field whose value has been changed.</param>
    public void NotifyFieldChanged(in FieldIdentifier fieldIdentifier)
    {
        GetOrAddFieldState(fieldIdentifier).IsModified = true;
        OnFieldChanged?.Invoke(this, new FieldChangedEventArgs(fieldIdentifier));
    }

    /// <summary>
    /// Signals that some aspect of validation state has changed.
    /// </summary>
    public void NotifyValidationStateChanged()
    {
        OnValidationStateChanged?.Invoke(this, ValidationStateChangedEventArgs.Empty);
    }

    /// <summary>
    /// Clears any modification flag that may be tracked for the specified field.
    /// </summary>
    /// <param name="fieldIdentifier">Identifies the field whose modification flag (if any) should be cleared.</param>
    public void MarkAsUnmodified(in FieldIdentifier fieldIdentifier)
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
        foreach (var state in _fieldStates)
        {
            state.Value.IsModified = false;
        }
    }

    /// <summary>
    /// Determines whether any of the fields in this <see cref="EditContext"/> have been modified.
    /// </summary>
    /// <returns>True if any of the fields in this <see cref="EditContext"/> have been modified; otherwise false.</returns>
    public bool IsModified()
    {
        // If necessary, we could consider caching the overall "is modified" state and only recomputing
        // when there's a call to NotifyFieldModified/NotifyFieldUnmodified
        foreach (var state in _fieldStates)
        {
            if (state.Value.IsModified)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the current validation messages across all fields.
    ///
    /// This method does not perform validation itself. It only returns messages determined by previous validation actions.
    /// </summary>
    /// <returns>The current validation messages.</returns>
    public IEnumerable<string> GetValidationMessages()
    {
        // Since we're only enumerating the fields for which we have a non-null state, the cost of this grows
        // based on how many fields have been modified or have associated validation messages
        foreach (var state in _fieldStates)
        {
            foreach (var message in state.Value.GetValidationMessages())
            {
                yield return message;
            }
        }
    }

    /// <summary>
    /// Gets the current validation messages for the specified field.
    ///
    /// This method does not perform validation itself. It only returns messages determined by previous validation actions.
    /// </summary>
    /// <param name="fieldIdentifier">Identifies the field whose current validation messages should be returned.</param>
    /// <returns>The current validation messages for the specified field.</returns>
    public IEnumerable<string> GetValidationMessages(FieldIdentifier fieldIdentifier)
    {
        if (_fieldStates.TryGetValue(fieldIdentifier, out var state))
        {
            foreach (var message in state.GetValidationMessages())
            {
                yield return message;
            }
        }
    }

    /// <summary>
    /// Gets the current validation messages for the specified field.
    ///
    /// This method does not perform validation itself. It only returns messages determined by previous validation actions.
    /// </summary>
    /// <param name="accessor">Identifies the field whose current validation messages should be returned.</param>
    /// <returns>The current validation messages for the specified field.</returns>
    public IEnumerable<string> GetValidationMessages(Expression<Func<object>> accessor)
        => GetValidationMessages(FieldIdentifier.Create(accessor));

    /// <summary>
    /// Determines whether the specified fields in this <see cref="EditContext"/> has been modified.
    /// </summary>
    /// <returns>True if the field has been modified; otherwise false.</returns>
    public bool IsModified(in FieldIdentifier fieldIdentifier)
        => _fieldStates.TryGetValue(fieldIdentifier, out var state)
        ? state.IsModified
        : false;

    /// <summary>
    /// Determines whether the specified fields in this <see cref="EditContext"/> has been modified.
    /// </summary>
    /// <param name="accessor">Identifies the field whose current validation messages should be returned.</param>
    /// <returns>True if the field has been modified; otherwise false.</returns>
    public bool IsModified(Expression<Func<object>> accessor)
        => IsModified(FieldIdentifier.Create(accessor));

    /// <summary>
    /// Determines whether the specified fields in this <see cref="EditContext"/> has no associated validation messages.
    /// </summary>
    /// <returns>True if the field has no associated validation messages after validation; otherwise false.</returns>
    public bool IsValid(in FieldIdentifier fieldIdentifier)
        => !GetValidationMessages(fieldIdentifier).Any();

    /// <summary>
    /// Determines whether the specified fields in this <see cref="EditContext"/> has no associated validation messages.
    /// </summary>
    /// <param name="accessor">Identifies the field whose current validation messages should be returned.</param>
    /// <returns>True if the field has no associated validation messages after validation; otherwise false.</returns>
    public bool IsValid(Expression<Func<object>> accessor)
        => IsValid(FieldIdentifier.Create(accessor));

    /// <summary>
    /// Validates this <see cref="EditContext"/>.
    /// </summary>
    /// <returns>True if there are no validation messages after validation; otherwise false.</returns>
    public bool Validate()
    {
        OnValidationRequested?.Invoke(this, ValidationRequestedEventArgs.Empty);
        return !GetValidationMessages().Any();
    }

    internal FieldState? GetFieldState(in FieldIdentifier fieldIdentifier)
    {
        _fieldStates.TryGetValue(fieldIdentifier, out var state);
        return state;
    }

    internal FieldState GetOrAddFieldState(in FieldIdentifier fieldIdentifier)
    {
        if (!_fieldStates.TryGetValue(fieldIdentifier, out var state))
        {
            state = new FieldState(fieldIdentifier);
            _fieldStates.Add(fieldIdentifier, state);
        }

        return state;
    }
}
