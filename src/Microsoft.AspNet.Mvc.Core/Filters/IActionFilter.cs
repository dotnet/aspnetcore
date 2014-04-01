namespace Microsoft.AspNet.Mvc
{
    public interface IActionFilter : IFilter
    {
        void OnActionExecuting([NotNull] ActionExecutingContext context);

        void OnActionExecuted([NotNull] ActionExecutedContext context);
    }
}