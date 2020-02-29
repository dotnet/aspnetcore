// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Testing
{
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
            if (environmentVariable == null)
            {
                throw new ArgumentNullException(nameof(environmentVariable));
            }
            if (variableName == null)
            {
                throw new ArgumentNullException(nameof(variableName));
            }
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

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
                var hasMatched = _values.Any(value => string.Compare(value, _currentValue, ignoreCase: true) == 0);

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
                var value = _currentValue == null ? "(null)" : _currentValue;
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
}
