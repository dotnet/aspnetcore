// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Binding;

/// <summary>
/// Context for binding a form value.
/// </summary>
public class FormValueSupplierContext
{
    private bool _resultSet;

    /// <summary>
    /// Initializes a new instance of <see cref="FormValueSupplierContext"/>.
    /// </summary>
    /// <param name="formName">The name of the form to bind data from.</param>
    /// <param name="valueType">The <see cref="Type"/> of the value to bind.</param>
    /// <param name="parameterName">The name of the parameter to bind data to.</param>
    public FormValueSupplierContext(
        string formName,
        Type valueType,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(formName, nameof(formName));
        ArgumentNullException.ThrowIfNull(valueType, nameof(valueType));
        ArgumentNullException.ThrowIfNull(parameterName, nameof(parameterName));
        FormName = formName;
        ParameterName = parameterName;
        ValueType = valueType;
    }

    /// <summary>
    /// Gets the name of the form to bind data from.
    /// </summary>
    public string FormName { get; }

    /// <summary>
    /// Gets the name of the parameter to bind data to.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Gets the <see cref="Type"/> of the value to bind.
    /// </summary>
    public Type ValueType { get; }

    /// <summary>
    /// Gets the callback to invoke when an error occurs.
    /// </summary>
    public Action<string, FormattableString, string?>? OnError { get; set; }

    /// <summary>
    /// Maps a set of errors to a concrete containing instance.
    /// </summary>
    /// <remarks>
    /// For example, maps errors for a given property in a class to the class instance.
    /// This is required so that validation can work without the need of the full identifier.
    /// </remarks>
    public Action<string, object>? MapErrorToContainer { get; set; }

    /// <summary>
    /// Gets the result of the binding operation.
    /// </summary>
    public object? Result { get; private set; }

    /// <summary>
    /// Sets the result of the binding operation.
    /// </summary>
    /// <param name="result">The result of the binding operation.</param>
    /// <exception cref="InvalidOperationException">Thrown if the result has already been set.</exception>
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
