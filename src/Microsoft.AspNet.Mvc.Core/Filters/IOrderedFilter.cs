namespace Microsoft.AspNet.Mvc
{
    public interface IOrderedFilter : IFilter
    {
        int Order { get; }
    }
}
