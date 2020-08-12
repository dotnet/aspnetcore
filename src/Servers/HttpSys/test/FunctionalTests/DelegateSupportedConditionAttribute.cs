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
        private static readonly Lazy<bool> _isDelegateSupported = new Lazy<bool>(CanDelegate);
        public bool IsMet => _isDelegateSupported.Value;

        public string SkipReason => "Http.Sys does not support delegating requests";

        private static bool CanDelegate()
        {
            try
            {
                return HttpApi.HttpIsFeatureSupported(HTTP_FEATURE_ID.HttpFeatureDelegateEx);
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }
    }
}
