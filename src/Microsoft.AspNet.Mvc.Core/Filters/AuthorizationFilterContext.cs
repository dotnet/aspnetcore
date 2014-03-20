using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class AuthorizationFilterContext
    {
        private IActionResult _actionResult;

        public AuthorizationFilterContext([NotNull] ActionContext actionContext, [NotNull] IReadOnlyList<FilterItem> filterItems)
        {
            ActionContext = actionContext;
            FilterItems = filterItems;
        }

        public bool HasFailed { get; private set; }

        public ActionContext ActionContext { get; private set; }

        public IReadOnlyList<FilterItem> FilterItems { get; private set; }

        // Result
        public IActionResult ActionResult
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
