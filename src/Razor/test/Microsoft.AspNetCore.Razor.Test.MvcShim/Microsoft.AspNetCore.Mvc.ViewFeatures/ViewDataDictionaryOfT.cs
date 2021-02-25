namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class ViewDataDictionary<TModel> : ViewDataDictionary
    {
        public TModel Model { get; set; }
    }
}
