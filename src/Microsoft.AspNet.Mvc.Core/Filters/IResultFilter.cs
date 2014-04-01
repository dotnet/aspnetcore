namespace Microsoft.AspNet.Mvc
{
    public interface IResultFilter : IFilter
    {
        void OnResultExecuting([NotNull] ResultExecutingContext context);

        void OnResultExecuted([NotNull] ResultExecutedContext context);
    }
}