// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.HttpSys.NonHelixTests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DelegateSupportedConditionAttribute : Attribute, ITestCondition
    {
        private readonly bool _isSupported;
        public DelegateSupportedConditionAttribute(bool isSupported) => _isSupported = isSupported;

        private readonly Lazy<bool> _isDelegateSupported = new Lazy<bool>(CanDelegate);
        public bool IsMet => (_isDelegateSupported.Value == _isSupported);

        public string SkipReason => $"Http.Sys does {(_isSupported ? "not" : "")} support delegating requests";

        private static bool CanDelegate()
        {
            return HttpApi.SupportsDelegation;
        }
    }
}
