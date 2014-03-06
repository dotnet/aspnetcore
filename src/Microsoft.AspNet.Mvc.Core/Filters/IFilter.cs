namespace Microsoft.AspNet.Mvc
{
    public interface IFilter
    {
        int Order { get; }
    }
}