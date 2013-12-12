
namespace Microsoft.AspNet.Mvc
{
    public class ControllerActionInvokerFactory : IActionInvokerFactory
    {
        private readonly IActionResultFactory _actionResultFactory;

        public ControllerActionInvokerFactory(IActionResultFactory actionResultFactory)
        {
            _actionResultFactory = actionResultFactory;
        }

        public IActionInvoker CreateInvoker(ControllerContext context)
        {
            return new ControllerActionInvoker(context, _actionResultFactory);
        }
    }
}
