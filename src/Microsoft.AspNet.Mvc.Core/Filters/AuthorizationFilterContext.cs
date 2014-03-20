using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    public class AuthorizationFilterContext : FilterContext
    {
        private IActionResult _actionResult;

        public AuthorizationFilterContext([NotNull] ActionContext actionContext, [NotNull] IReadOnlyList<FilterItem> filterItems)
            : base(actionContext, filterItems)
        {
        }

        public bool HasFailed { get; private set; }

        // Result
        public override IActionResult ActionResult
        {
            get { return _actionResult; }
            set
            {
                if (value != null)
                {
                    Fail();
                }

                _actionResult = value;
            }
        }

        public void Fail()
        {
            HasFailed = true;
        }
    }
}
