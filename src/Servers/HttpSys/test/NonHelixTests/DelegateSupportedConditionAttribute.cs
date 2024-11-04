// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.HttpSys.NonHelixTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DelegateSupportedConditionAttribute : Attribute, ITestCondition
{
    private readonly bool _isSupported;
    private readonly bool _httpApiSupportsDelegation;
    public DelegateSupportedConditionAttribute(bool isSupported)
    {
        _isSupported = isSupported;
        try { _httpApiSupportsDelegation = HttpApi.SupportsDelegation; } catch { }
    }

    public bool IsMet =>  _httpApiSupportsDelegation == _isSupported;

    public string SkipReason => $"Http.Sys does {(_isSupported ? "not" : "")} support delegating requests";
}
