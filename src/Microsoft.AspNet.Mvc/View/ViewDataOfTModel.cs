
namespace Microsoft.AspNet.Mvc
{
    public class ViewData<TModel> : ViewData
    {
        public ViewData()
            : base()
        {
        }

        public ViewData(ViewData source) :
            base(source)
        {
        }

        public ViewData(ViewData<TModel> source)
            : base(source)
        {
            Model = source.Model;
        }

        public TModel Model { get; set; }
    }
}
