// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DelegateSupportedConditionAttribute : Attribute, ITestCondition
{
    private readonly bool _isSupported;
    public DelegateSupportedConditionAttribute(bool isSupported) => _isSupported = isSupported;

    public bool IsMet => HttpApi.SupportsDelegation == _isSupported;

    public string SkipReason => $"Http.Sys does {(_isSupported ? "not" : "")} support delegating requests";
}
