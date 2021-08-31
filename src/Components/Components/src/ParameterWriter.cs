// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

#pragma warning disable RS0016 // Add public types and members to the declared API
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public interface IPropertySetter
{
    bool Cascading { get; }
    void SetValue(object target, object value);
}

public interface IUnmatchedValuesPropertySetter : IPropertySetter
{
    string UnmatchedValuesPropertyName { get; }
}

public class ParameterWriter<T> : IPropertySetter
{
    private readonly Action<T, object> _propertySetter;

    public bool Cascading { get; init; }

    public ParameterWriter(Action<T, object> propertySetter)
    {
        _propertySetter = propertySetter;
    }

    public void SetValue(object target, object value) => _propertySetter((T)target, value);
}

public sealed class UnmatchedValuesParameterWriter<T> : ParameterWriter<T>, IUnmatchedValuesPropertySetter
{
    public string UnmatchedValuesPropertyName { get; }

    public UnmatchedValuesParameterWriter(string unmatchedValuesPropertyName, Action<T, object> propertySetter)
        : base(propertySetter)
    {
        UnmatchedValuesPropertyName = unmatchedValuesPropertyName;
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore RS0016 // Add public types and members to the declared API
