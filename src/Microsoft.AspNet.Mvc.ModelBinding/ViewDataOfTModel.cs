
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

        public new TModel Model
        {
            get { return (TModel)base.Model; }
            set { SetModel(value); }
        }

        protected override void SetModel(object value)
        {
            // TODO: Add checks for cast
            base.SetModel((TModel)value);
        }
    }
}
