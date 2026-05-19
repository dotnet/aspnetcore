// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms.Mapping;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// The context associated with a given form mapping operation.
/// </summary>
public sealed class FormMappingContext
{
    private Dictionary<string, FormMappingError>? _errors;
    private List<KeyValuePair<string, FormMappingError>>? _pendingErrors;
    private Dictionary<string, Dictionary<string, FormMappingError>>? _errorsByFormName;

    internal FormMappingContext(string mappingScopeName)
    {
        ArgumentNullException.ThrowIfNull(mappingScopeName);
        MappingScopeName = mappingScopeName;
    }

    /// <summary>
    /// The mapping scope name.
    /// </summary>
    public string MappingScopeName { get; }

    /// <summary>
    /// Retrieves the list of errors for a given model key.
    /// </summary>
    /// <param name="key">The key used to identify the specific part of the model.</param>
    /// <returns>The list of errors associated with that part of the model if any.</returns>
    public FormMappingError? GetErrors(string key) =>
        _errors?.TryGetValue(key, out var mappingError) == true ? mappingError : null;

    /// <summary>
    /// Retrieves the list of errors for a given model key.
    /// </summary>
    /// <param name="key">The key used to identify the specific part of the model.</param>
    /// <param name="formName">Form name for a form under this context.</param>
    /// <returns>The list of errors associated with that part of the model if any.</returns>
    public FormMappingError? GetErrors(string formName, string key) =>
        _errorsByFormName?.TryGetValue(formName, out var formErrors) == true &&
        formErrors.TryGetValue(key, out var mappingError) == true ? mappingError : null;

    /// <summary>
    /// Retrieves all the errors for the model.
    /// </summary>
    /// <returns>The list of errors associated with the model if any.</returns>
    public IEnumerable<FormMappingError> GetAllErrors()
    {
        return GetAllErrorsCore(_errors);
    }

    private static IEnumerable<FormMappingError> GetAllErrorsCore(Dictionary<string, FormMappingError>? errors)
    {
        if (errors == null)
        {
            return Array.Empty<FormMappingError>();
        }

        return errors.Values;
    }

    /// <summary>
    /// Retrieves all the errors for the model.
    /// </summary>
    /// <param name="formName">Form name for a form under this context.</param>
    /// <returns>The list of errors associated with the model if any.</returns>
    public IEnumerable<FormMappingError> GetAllErrors(string formName)
    {
        return _errorsByFormName?.TryGetValue(formName, out var formErrors) == true ?
            GetAllErrorsCore(formErrors) :
            Array.Empty<FormMappingError>();
    }

    /// <summary>
    /// Retrieves the attempted value that failed to map for a given model key.
    /// </summary>
    /// <param name="key">The key used to identify the specific part of the model.</param>
    /// <returns>The attempted value associated with that part of the model if any.</returns>
    public string? GetAttemptedValue(string key) =>
        _errors?.TryGetValue(key, out var mappingError) == true ? mappingError.AttemptedValue : null;

    /// <summary>
    /// Retrieves the attempted value that failed to map for a given model key.
    /// </summary>
    /// <param name="formName">Form name for a form under this context.</param>
    /// <param name="key">The key used to identify the specific part of the model.</param>
    /// <returns>The attempted value associated with that part of the model if any.</returns>
    public string? GetAttemptedValue(string formName, string key) =>
        _errorsByFormName?.TryGetValue(formName, out var formErrors) == true &&
            formErrors.TryGetValue(key, out var mappingError) ? mappingError.AttemptedValue : null;

    internal void AddError(string key, FormattableString error, string? attemptedValue)
    {
        _errors ??= new Dictionary<string, FormMappingError>();
        AddErrorCore(_errors, key, error, attemptedValue, ref _pendingErrors);
    }

    private static void AddErrorCore(Dictionary<string, FormMappingError> errors, string key, FormattableString error, string? attemptedValue, ref List<KeyValuePair<string, FormMappingError>>? pendingErrors)
    {
        if (!errors.TryGetValue(key, out var mappingError))
        {
            mappingError = new FormMappingError(key, new List<FormattableString>() { error }, attemptedValue);
            errors.Add(key, mappingError);
            pendingErrors ??= new();
            pendingErrors.Add(new KeyValuePair<string, FormMappingError>(key, mappingError));
        }
        else
        {
            mappingError.AddError(error);
        }
    }

    internal void AddError(string formName, string key, FormattableString error, string? attemptedValue)
    {
        _errorsByFormName ??= new Dictionary<string, Dictionary<string, FormMappingError>>();
        if (!_errorsByFormName.TryGetValue(formName, out var formErrors))
        {
            formErrors = new Dictionary<string, FormMappingError>();
            _errorsByFormName.Add(formName, formErrors);
        }
        AddErrorCore(formErrors, key, error, attemptedValue, ref _pendingErrors);
    }

    internal void AttachParentValue(string key, object value)
    {
        if (_pendingErrors == null)
        {
            return;
        }

        for (var i = 0; i < _pendingErrors.Count; i++)
        {
            var (errorKey, error) = _pendingErrors[i];
            if (!errorKey.StartsWith(key, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"'{errorKey}' does must start with '{key}'");
            }

            error.Container = value;
        }

        _pendingErrors.Clear();
    }

    internal void SetErrors(string formName, FormMappingContext childContext)
    {
        if (_errorsByFormName == null || !_errorsByFormName.TryGetValue(formName, out var formErrors))
        {
            return;
        }

        childContext._errors = formErrors;
    }
}
