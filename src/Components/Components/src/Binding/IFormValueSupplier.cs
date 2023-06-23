// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Binding;

/// <summary>
/// Binds form data values to a model.
/// </summary>
public interface IFormValueSupplier
{
    /// <summary>
    /// Determines whether the specified value type can be bound.
    /// </summary>
    /// <param name="formName">The form name to bind data from.</param>
    /// <param name="valueType">The <see cref="Type"/> for the value to bind.</param>
    /// <returns><c>true</c> if the value type can be bound; otherwise, <c>false</c>.</returns>
    bool CanBind(Type valueType, string? formName = null);

    /// <summary>
    /// Tries to bind the form with the specified name to a value of the specified type.
    /// </summary>
    /// <param name="formName">The form name to bind data from.</param>
    /// <param name="valueType">The <see cref="Type"/> for the value to bind.</param>
    /// <param name="boundValue">The bound value if succeeded.</param>
    /// <param name="onError">The callback to invoke if an error occurs during the binding process.</param>
    /// <returns><c>true</c> if the form was bound successfully; otherwise, <c>false</c>.</returns>
    void Bind(FormValueSupplierContext context);
}

/// <summary>
/// Context for binding a form value.
/// </summary>
public struct FormValueSupplierContext
{
    private bool _resultSet;

    public FormValueSupplierContext(
        string formName,
        Type valueType,
        string parameterName,
        Action<string, FormattableString, string?> onError)
    {
        FormName = formName;
        ParameterName = parameterName;
        ValueType = valueType;
        OnError = onError;
    }

    public string FormName { get; }
    public string ParameterName { get; }
    public Type ValueType { get; }
    public Action<string, FormattableString, string?> OnError { get; }

    public object? Result { get; private set; }

    public void SetResult(object? result)
    {
        if (_resultSet)
        {
            throw new InvalidOperationException($"The result has already been set to '{Result}'.");
        }
        _resultSet = true;
        Result = result;
    }
}
