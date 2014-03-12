namespace Microsoft.AspNet.Mvc.Filters
{
    public interface IFilterContainer
    {
        IFilter FilterDefinition { get; set; }
    }
}
