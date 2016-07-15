using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Rewrite.RuleAbstraction
{
    public enum RuleTerminiation
    {
        Continue,
        ResponseComplete,
        StopRules
    }
}
