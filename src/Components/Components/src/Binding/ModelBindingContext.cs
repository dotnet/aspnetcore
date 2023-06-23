// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// The binding context associated with a given model binding operation.
/// </summary>
public sealed class ModelBindingContext
{
    private Dictionary<string, BindingError>? _errors;
    private Dictionary<string, Dictionary<string, BindingError>>? _errorsByFormName;

    internal ModelBindingContext(string name, string bindingContextId)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(bindingContextId);
        // We are initializing the root context, that can be a "named" root context, or the default context.
        // A named root context only provides a name, and that acts as the BindingId
        // A "default" root context does not provide a name, and instead it provides an explicit Binding ID.
        // The explicit binding ID matches that of the default handler, which is the URL Path.
        if (string.IsNullOrEmpty(name) ^ string.IsNullOrEmpty(bindingContextId))
        {
            throw new InvalidOperationException("A root binding context needs to provide a name and explicit binding context id or none.");
        }

        Name = name;
        BindingContextId = bindingContextId ?? name;
    }

    /// <summary>
    /// The context name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The computed identifier used to determine what parts of the app can bind data.
    /// </summary>
    public string BindingContextId { get; }

    /// <summary>
    /// Retrieves the list of errors for a given model key.
    /// </summary>
    /// <param name="key">The key used to identify the specific part of the model.</param>
    /// <returns>The list of errors associated with that part of the model if any.</returns>
    public IReadOnlyList<FormattableString> GetErrors(string key) =>
        _errors?.TryGetValue(key, out var bindingError) == true ? bindingError.ErrorMessages : Array.Empty<FormattableString>();

    /// <summary>
    /// Retrieves all the errors for the model.
    /// </summary>
    /// <returns>The list of errors associated with the model if any.</returns>
    public IEnumerable<KeyValuePair<string, IReadOnlyList<FormattableString>>> GetAllErrors()
    {
        if (_errors == null)
        {
            yield break;
        }

        foreach (var (key, value) in _errors)
        {
            yield return new KeyValuePair<string, IReadOnlyList<FormattableString>>(key, value.ErrorMessages);
        }
    }

    /// <summary>
    /// Retrieves the attempted value that failed to bind for a given model key.
    /// </summary>
    /// <param name="key">The key used to identify the specific part of the model.</param>
    /// <returns>The attempted value associated with that part of the model if any.</returns>
    public string? GetAttemptedValue(string key) =>
        _errors?.TryGetValue(key, out var bindingError) == true ? bindingError.AttemptedValue : null;

    internal static string Combine(ModelBindingContext? parentContext, string name) =>
        string.IsNullOrEmpty(parentContext?.Name) ? name : $"{parentContext.Name}.{name}";

    internal void AddError(string key, FormattableString error, string? attemptedValue)
    {
        _errors ??= new Dictionary<string, BindingError>();
        AddErrorCore(_errors, key, error, attemptedValue);
    }

    private static void AddErrorCore(Dictionary<string, BindingError> errors, string key, FormattableString error, string? attemptedValue)
    {
        if (!errors.TryGetValue(key, out var bindingError))
        {
            bindingError = new BindingError(new List<FormattableString>() { error }, attemptedValue);
            errors.Add(key, bindingError);
        }
        else
        {
            bindingError.ErrorMessages.Add(error);
        }
    }

    internal void AddError(string formName, string key, FormattableString error, string? attemptedValue)
    {
        _errorsByFormName ??= new Dictionary<string, Dictionary<string, BindingError>>();
        if (!_errorsByFormName.TryGetValue(formName, out var formErrors))
        {
            formErrors = new Dictionary<string, BindingError>();
            _errorsByFormName.Add(formName, formErrors);
        }
        AddErrorCore(formErrors, key, error, attemptedValue);
    }

    internal void SetErrors(string formName, ModelBindingContext childContext)
    {
        if (_errorsByFormName == null || !_errorsByFormName.TryGetValue(formName, out var formErrors))
        {
            return;
        }

        childContext._errors = formErrors;
    }

    private record struct BindingError(List<FormattableString> ErrorMessages, string? AttemptedValue);
}
