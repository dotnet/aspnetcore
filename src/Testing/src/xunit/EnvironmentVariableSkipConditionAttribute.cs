// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.InternalTesting;

/// <summary>
/// Skips a test when the value of an environment variable matches any of the supplied values.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class EnvironmentVariableSkipConditionAttribute : Attribute, ITestCondition
{
    private readonly string _variableName;
    private readonly string[] _values;
    private string _currentValue;
    private readonly IEnvironmentVariable _environmentVariable;

    /// <summary>
    /// Creates a new instance of <see cref="EnvironmentVariableSkipConditionAttribute"/>.
    /// </summary>
    /// <param name="variableName">Name of the environment variable.</param>
    /// <param name="values">Value(s) of the environment variable to match for the test to be skipped</param>
    public EnvironmentVariableSkipConditionAttribute(string variableName, params string[] values)
        : this(new EnvironmentVariable(), variableName, values)
    {
    }

    // To enable unit testing
    internal EnvironmentVariableSkipConditionAttribute(
        IEnvironmentVariable environmentVariable,
        string variableName,
        params string[] values)
    {
        ArgumentNullThrowHelper.ThrowIfNull(environmentVariable);
        ArgumentNullThrowHelper.ThrowIfNull(variableName);
        ArgumentNullThrowHelper.ThrowIfNull(values);

        _variableName = variableName;
        _values = values;
        _environmentVariable = environmentVariable;
    }

    /// <summary>
    /// Runs the test only if the value of the variable matches any of the supplied values. Default is <c>True</c>.
    /// </summary>
    public bool RunOnMatch { get; set; } = true;

    public bool IsMet
    {
        get
        {
            _currentValue = _environmentVariable.Get(_variableName);
            var hasMatched = _values.Any(value => string.Equals(value, _currentValue, StringComparison.OrdinalIgnoreCase));

            if (RunOnMatch)
            {
                return hasMatched;
            }
            else
            {
                return !hasMatched;
            }
        }
    }

    public string SkipReason
    {
        get
        {
            var value = _currentValue ?? "(null)";
            return $"Test skipped on environment variable with name '{_variableName}' and value '{value}' " +
                $"for the '{nameof(RunOnMatch)}' value of '{RunOnMatch}'.";
        }
    }

    private struct EnvironmentVariable : IEnvironmentVariable
    {
        public string Get(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }
    }
}
