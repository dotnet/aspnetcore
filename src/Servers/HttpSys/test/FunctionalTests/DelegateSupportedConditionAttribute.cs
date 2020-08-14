using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using static Microsoft.AspNetCore.HttpSys.Internal.HttpApiTypes;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests
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
            return HttpApi.IsFeatureSupported(HTTP_FEATURE_ID.HttpFeatureDelegateEx);
        }
    }
}
