// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.InternalTesting;

/// <summary>
/// Supplies a <see cref="ConditionalTheoryAttribute"/> data row that is quarantined on the specified operating systems.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class QuarantinedTestDataAttribute : DataAttribute
{
    private readonly object[] _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuarantinedTestDataAttribute"/> class for use with <see cref="ConditionalTheoryAttribute"/>.
    /// </summary>
    /// <param name="reason">A reason that this test data row is quarantined. Preferably a GitHub issue URL.</param>
    /// <param name="operatingSystems">The operating systems where the test data row is quarantined.</param>
    /// <param name="data">The data values to pass to the theory.</param>
    public QuarantinedTestDataAttribute(string reason, OperatingSystems operatingSystems, params object[] data)
    {
        Reason = reason;
        OperatingSystems = operatingSystems;
        _data = data;
    }

    /// <summary>
    /// Gets the reason for quarantining the test data row.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the operating systems where the test data row is quarantined.
    /// </summary>
    public OperatingSystems OperatingSystems { get; }

    /// <inheritdoc />
    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var data = new object[_data.Length + 1];
        data[0] = new QuarantinedTestData(Reason, OperatingSystems);
        Array.Copy(_data, 0, data, 1, _data.Length);

        yield return data;
    }
}

internal sealed class QuarantinedTestData : IXunitSerializable
{
    private string _reason;
    private OperatingSystems _operatingSystems;

    public QuarantinedTestData()
    {
    }

    public QuarantinedTestData(string reason, OperatingSystems operatingSystems)
    {
        _reason = reason;
        _operatingSystems = operatingSystems;
    }

    public QuarantinedTestAttribute Attribute => new(_reason, _operatingSystems);

    public void Deserialize(IXunitSerializationInfo info)
    {
        _reason = info.GetValue<string>(nameof(_reason));
        _operatingSystems = info.GetValue<OperatingSystems>(nameof(_operatingSystems));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(_reason), _reason);
        info.AddValue(nameof(_operatingSystems), _operatingSystems);
    }
}
