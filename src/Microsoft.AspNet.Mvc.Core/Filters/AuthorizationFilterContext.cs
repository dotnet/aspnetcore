namespace Microsoft.AspNet.Mvc
{
    public class AuthorizationFilterContext
    {
        private IActionResult _actionResult;
        private bool _fail;

        public AuthorizationFilterContext(ActionContext actionContext)
        {
            ActionContext = actionContext;
        }

        public bool HasFailed
        {
            get { return _fail; }
        }

        public ActionContext ActionContext { get; private set; }

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
            _fail = true;
        }
    }
}
