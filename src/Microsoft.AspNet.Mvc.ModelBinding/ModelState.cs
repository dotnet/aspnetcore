
namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelState
    {
        private readonly ModelErrorCollection _errors = new ModelErrorCollection();

        public ValueProviderResult Value { get; set; }

        public ModelErrorCollection Errors
        {
            get { return _errors; }
        }
    }
}
